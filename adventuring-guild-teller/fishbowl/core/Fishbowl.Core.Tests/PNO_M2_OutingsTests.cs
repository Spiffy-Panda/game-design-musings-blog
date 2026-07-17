using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Model;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// PNO.M2 — outings. The gate: a townee takes a standing posting, leaves, is findable at the site every
/// slot, returns, cools down, re-enters daily life — and <b>Away's one-way trapdoor is gone</b>.
/// <para>Board tests already explained why these run on the LIVE town, not the posting-free fixture
/// (<see cref="TestSupport.LoadLiveTown"/>): the fixture has no adventurers taking paper. The one fixture
/// test here is the opposite — it pins that <c>departs_day</c> still works (PNO.D6).</para>
/// </summary>
public class PNO_M2_OutingsTests
{
    // --- the mechanism, controlled -------------------------------------------------------

    [Fact]
    public void Take_Leaves_Town_And_Is_Findable_At_The_Site_Every_Slot()
    {
        var world = World.Build(TestSupport.LoadLiveTown());
        var posting = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        var adv = world.TowneeById["corvo-lunt"];
        Assert.Equal(Phase.Daily, adv.Phase);

        var outing = Outings.Take(world, "corvo-lunt", posting.Id, day: 1);
        Assert.NotNull(outing);
        Assert.Equal(Phase.Outing, adv.Phase);
        Assert.True(adv.Away);                                   // Away is derived: Outing ⟹ away
        Assert.Equal(PostingState.Taken, posting.State);
        Assert.Equal("corvo-lunt", posting.TakerId);

        // The itinerary catches up at the next dawn (never mid-day — that would rewind the streams). From
        // then the party is co-present at the site every single slot, which is what lets a site storylet
        // fire off-screen with no new engine code.
        Clockwork.ResolveDay(world);
        for (int s = 0; s < world.SlotsPerDay; s++)
        {
            Assert.Equal("the-sedge-fen", world.PlaceOf(adv, s));
            Assert.Contains("corvo-lunt", world.OccupantsAt(s).GetValueOrDefault("the-sedge-fen") ?? new());
        }
    }

    [Fact]
    public void An_Outing_Runs_To_An_Outcome_Then_Cooldown_Then_Back_To_Daily()
    {
        var world = World.Build(TestSupport.LoadLiveTown());
        var posting = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        var adv = world.TowneeById["corvo-lunt"];
        Outings.Take(world, "corvo-lunt", posting.Id, day: 1);

        // Walk the whole track (a day of slots is more than the sedge-fen's 19).
        for (int i = 0; i < world.SlotsPerDay; i++) Outings.StepSlot(world, i);
        Assert.True(adv.Outing!.Complete);
        Assert.NotEqual(OutingOutcome.Pending, adv.Outing.Outcome);   // an outcome was decided
        Assert.Equal(PostingState.Resolved, posting.State);

        // Day boundary: complete → cooldown, and cooldown is IN TOWN (not away).
        Outings.ResolveDay(world, day: 2);
        Assert.Equal(Phase.Cooldown, adv.Phase);
        Assert.False(adv.Away);

        // Cooldown elapses → daily life, outing cleared. THE TRAPDOOR IS GONE: there is a return path.
        Outings.ResolveDay(world, day: adv.CooldownUntilDay);
        Assert.Equal(Phase.Daily, adv.Phase);
        Assert.Null(adv.Outing);
    }

    [Fact]
    public void Restlessness_Discharges_Across_An_Outing()
    {
        // The PNO.M2 ruling: BOTH bursts (take + resolve) AND a tick while out. This controlled path
        // exercises the two bursts (the tick lives in Pressures.DriftSlot, covered by the soak); together
        // they are why the lint ledger's restlessness ratchets legitimately shrink after M2.
        var world = World.Build(TestSupport.LoadLiveTown());
        var posting = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        var adv = world.TowneeById["corvo-lunt"];
        adv.Pressures["restlessness"] = 0.9;

        Outings.Take(world, "corvo-lunt", posting.Id, day: 1);        // −0.15 burst
        for (int i = 0; i < world.SlotsPerDay; i++) Outings.StepSlot(world, i);   // completes → −0.25 burst
        Assert.True(adv.Pressure("restlessness") < 0.6,
            $"expected the two bursts to discharge restlessness, got {adv.Pressure("restlessness"):0.00}");
    }

    // --- the guards ----------------------------------------------------------------------

    [Fact]
    public void An_Errand_Cannot_Be_Taken_As_An_Outing()
    {
        // reach:"errand" carries no site (a neighbour handles it in town), so it is not an outing. Take
        // must refuse it rather than start a trip to nowhere.
        var world = World.Build(TestSupport.LoadLiveTown());
        var errand = Board.File(world, "pellow-flour-run", "nan-pellow", day: 1, slot: 0)!;
        Assert.Null(errand.SiteId);
        Assert.Null(Outings.Take(world, "corvo-lunt", errand.Id, day: 1));
        Assert.Equal(Phase.Daily, world.TowneeById["corvo-lunt"].Phase);
    }

    [Fact]
    public void A_Townee_Already_Out_Cannot_Take_Another_Posting()
    {
        var world = World.Build(TestSupport.LoadLiveTown());
        var p1 = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        var p2 = Board.File(world, "pig-iron-short", "marrow-bray", day: 1, slot: 0)!;
        Assert.NotNull(Outings.Take(world, "corvo-lunt", p1.Id, day: 1));
        Assert.Null(Outings.Take(world, "corvo-lunt", p2.Id, day: 1));   // already Outing, not Daily
        Assert.Equal(PostingState.Standing, p2.State);
    }

    // --- the whole loop, emergent in the real sim ----------------------------------------

    [Fact]
    public void The_Live_Town_Runs_The_Loop_End_To_End()
    {
        // No hand-driving: the bank's posting-taken rule binds an adventurer to a standing posting, the
        // phase machine does the rest. Over a fortnight at least one outing must complete and at least one
        // adventurer must be home again — the loop closed itself, and nobody is stuck out (the trapdoor).
        var sim = new Simulation(TestSupport.LoadLiveTown());
        sim.RunDays(14);

        Assert.Contains(sim.World.Postings, p => p.State == PostingState.Resolved);
        Assert.Contains(sim.World.Townees, t => t.Adventurer && t.Phase == Phase.Daily);
    }

    [Fact]
    public void Same_Seed_Reproduces_The_Hash_Sequence()
    {
        // The M2 determinism gate — genuine reproducibility, run-to-run. The outings stream draws real RNG
        // now (per posting, via SubRngFor), so this is doing more work than the fixture's seed-independence.
        // (No seed-DIVERGENCE assertion here on purpose: the only RNG-dependent state is an outing's
        // outcome, and at the authored hazard most outings carry regardless of seed, so two seeds can
        // honestly produce the same hashes — a divergence assert would be flaky, not a real invariant.)
        var a = RunHashes(seed: 1123, days: 14);
        var b = RunHashes(seed: 1123, days: 14);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Snapshot_Round_Trips_An_Active_Outing()
    {
        // The lossy-snapshot tripwire the drift check named: a bool Away could not carry a mid-flight
        // outing, so the reload would diverge. Phase + leg progress must survive.
        var town = TestSupport.LoadLiveTown();
        var world = World.Build(town);
        var posting = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        Outings.Take(world, "corvo-lunt", posting.Id, day: 1);
        for (int i = 0; i < 5; i++) Outings.StepSlot(world, i);      // mid-first-leg
        var before = world.TowneeById["corvo-lunt"].Outing!;

        var reloaded = Snapshot.Load(town, Snapshot.Save(world));
        var after = reloaded.TowneeById["corvo-lunt"];
        Assert.Equal(Phase.Outing, after.Phase);
        Assert.NotNull(after.Outing);
        Assert.Equal(before.SiteId, after.Outing!.SiteId);
        Assert.Equal(before.LegIndex, after.Outing.LegIndex);
        Assert.Equal(before.SlotsIntoLeg, after.Outing.SlotsIntoLeg);
    }

    [Fact]
    public void Live_Town_Snapshot_Reproduces_The_Forward_Hash_Sequence()
    {
        // The determinism contract for the LIVE town — board and trips included. This is the test the
        // fixture-only M2_PressuresSnapshotTests structurally could not be: a posting-free town has no
        // board to lose. It reddens if postings are not snapshotted (the whole board vanishes on reload
        // and the future diverges), which is why they now are.
        var town = TestSupport.LoadLiveTown();
        var simA = new Simulation(town);
        simA.RunDays(4);                                    // board fills; outings start
        string snap = Snapshot.Save(simA.World);
        simA.RunDays(4);                                    // days 5-8, no reload

        var simB = new Simulation(Snapshot.Load(town, snap));
        simB.RunDays(4);
        foreach (int d in new[] { 5, 6, 7, 8 })
            Assert.Equal(simA.World.DayHashes[d], simB.World.DayHashes[d]);
    }

    // --- PNO.D6: departs_day still works in the fixture ----------------------------------

    [Fact]
    public void Departs_Day_Still_Sends_A_Fixture_Adventurer_Off_Screen()
    {
        // PNO.D6 insurance: the frozen fixture keeps Brindle's departs_day: 1, and it must still be a
        // bare, off-screen departure — Phase.Outing with NO outing record, routed to "away" and frozen.
        // This is the golden-day's own mechanism, preserved through the phase generalization.
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunToDawn();                                             // finish day 1 → dawn of day 2
        var brindle = sim.World.TowneeById["brindle-ashe"];
        Assert.Equal(Phase.Outing, brindle.Phase);
        Assert.True(brindle.Away);
        Assert.Null(brindle.Outing);                                // BARE — no site, no legs, no return
        for (int s = 0; s < sim.World.SlotsPerDay; s++)
            Assert.Equal("away", sim.World.PlaceOf(brindle, s));
    }

    private static List<string> RunHashes(long seed, int days)
    {
        var world = World.Build(TestSupport.LoadLiveTown());
        world.Seed = seed;
        world.ResetDayStreams();
        var sim = new Simulation(world);
        sim.RunDays(days);
        return Enumerable.Range(1, days).Select(d => sim.World.DayHashes[d]).ToList();
    }
}

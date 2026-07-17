using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>VFB.M1 — clockwork acceptance + VFB.Q4 determinism: every townee is findable at
/// every slot, and the day-hash sequence is identical across independent runs.</summary>
public class M1_ClockworkDeterminismTests
{
    [Fact]
    public void Every_Townee_Is_Findable_At_Every_Slot()
    {
        var world = World.Build(TestSupport.LoadGoldenTown());
        for (int slot = 0; slot < world.SlotsPerDay; slot++)
        {
            foreach (var t in world.Townees)
            {
                string place = world.PlaceOf(t, slot);
                Assert.False(string.IsNullOrEmpty(place));
                bool ok = place == "away" || world.Town.PlaceById.ContainsKey(place);
                Assert.True(ok, $"{t.Id} at slot {slot} resolved to unknown place '{place}'");
            }
        }
    }

    [Fact]
    public void Twelve_Townees_Three_Days_Identical_Hash_Sequence()
    {
        var a = RunHashes(3);
        var b = RunHashes(3);
        Assert.Equal(a, b);
        Assert.Equal(3, a.Count);
        Assert.All(a, h => Assert.Equal(16, h.Length));
    }

    [Fact]
    public void Twelve_Townees_Three_Days_Hash_Sequence_Is_Pinned()
    {
        // THE ABSOLUTE PIN behind FISHBOWL.md's "12x3-day deterministic hash sequence".
        //
        // The test above only proves run A == run B within a single build, so a change that moved
        // every hash *consistently* sails through it green — self-consistency is not stability, and
        // the determinism contract claims stability. These literals were captured from the frozen
        // golden fixture and are the value the contract refers to; the golden town cannot drift
        // (PNO.D2), so nothing but a real change to what enters the hash can move them.
        //
        // If you are here because this test went red: that is the test working. Do not re-baseline
        // it to make it green. Either the change was not supposed to touch the hash — fix the change
        // — or it was, and that needs a ruling in DEV-LOG.md before these strings move.
        //
        // THESE STRINGS HAVE MOVED EXACTLY ONCE. 2026-07-16, ruled by Panda (NTD.Q1 + FBT.Q1) — the
        // second branch above, and the DEV-LOG entry of that date is the ruling. They were
        // b8d15299d8817639 / e3478bc4ff7d4848 / 02bc86b987c547c3, and they moved because
        // Pressures.BaseDaily's `trade` arm stopped being a flat -0.11/day countdown and became a
        // restoring force. Two other fixes landed in the same change (signed pressure_rate_mods,
        // heart pressure_targets) and moved NOTHING here — verified by staging them alone and watching
        // the old literals stay green, which is what "hash-neutral" has to mean if it means anything.
        //
        // What the next reader most needs to know, because it is the part that generalises: the same
        // ruling deleted 2 of the 7 beats in ../../tests/towns/golden-town/golden/day1.json. Both
        // (`stock-runs-low`, `fetch-arranged`) only ever fired BECAUSE of the ratchet, so that
        // acceptance list had been pinning the defect rather than catching it — and it did that
        // underneath this pin, a 30-test suite, and a determinism contract, all of them green, because
        // a ratchet is perfectly deterministic. Determinism was never the missing property.
        // M2_TradeEquilibriumTests is the guard that was missing, and it asserts two-wayness directly.
        Assert.Equal(
            new[] { "2a6a8a3af0a1a81d", "d615d01daa2c8020", "619649026a9d8895" },
            RunHashes(3));
    }

    [Fact]
    public void At_Default_Config_Hash_Is_Seed_Independent()
    {
        // No RNG is consumed at storylet_rate 1.0, so the run is deterministic regardless of
        // seed — a useful invariant, and proof the sim never leaked wall-clock/process state.
        var world1 = World.Build(TestSupport.LoadGoldenTown());
        var sim1 = new Simulation(world1); sim1.RunToDawn();
        var world2 = World.Build(TestSupport.LoadGoldenTown());
        world2.Seed = 999_999; world2.ResetDayStreams();
        var sim2 = new Simulation(world2); sim2.RunToDawn();
        Assert.Equal(sim1.World.DayHashes[1], sim2.World.DayHashes[1]);
    }

    private static List<string> RunHashes(int days)
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunDays(days);
        return Enumerable.Range(1, days).Select(d => sim.World.DayHashes[d]).ToList();
    }
}

using System.Text.Json.Nodes;
using Fishbowl.Core.Api;
using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// VFB.M3/M4 — the Summarizer's dawn/read split and the <see cref="WorldView"/> projections over it.
/// <para>FISHBOWL.md justifies putting the projections in the engine-free core "so they are
/// unit-testable"; until this file, nothing tested them. These are the tests that would have caught
/// the four defects the split fixes: the register/lines time-base lie, the rendering knobs having no
/// live effect, a past day being re-gated against today's occupancy, and snapshots silently dropping
/// the summary behind a green hash-only test.</para>
/// </summary>
public class M3_SummaryRenderTests
{
    // --- rendering knobs are live (the tuning surface actually tunes) -----------------------

    [Fact]
    public void Rendering_Knobs_Re_Render_A_Finished_Day_Without_Re_Running_It()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunToDawn();                       // day 1 is done; we never step the sim again
        var w = sim.World;
        string hashBefore = w.DayHashes[1];

        // actionability — a different register, same day, no re-run.
        w.SetKnob("actionability", 0.05);
        var hearsay = Summarizer.Render(w, 1).Select(l => l.Text).ToList();
        w.SetKnob("actionability", 0.95);
        var report = Summarizer.Render(w, 1).Select(l => l.Text).ToList();
        Assert.NotEmpty(hearsay);
        Assert.NotEqual(hearsay, report);

        // summary_lines — truncation moves on read.
        w.SetKnob("summary_lines", 3);
        Assert.Equal(3, Summarizer.Render(w, 1).Count);
        w.SetKnob("summary_lines", 7);
        Assert.True(Summarizer.Render(w, 1).Count > 3);

        // hearsay_required — the filter re-evaluates on read, over the gate frozen at dawn.
        w.SetKnob("hearsay_required", 0);
        Assert.Equal(w.Chronicle.Count(e => e.Day == 1), Summarizer.Candidates(w, 1).Count);
        w.SetKnob("hearsay_required", 1);
        Assert.All(Summarizer.Candidates(w, 1), e => Assert.True(e.CarriedByGossip));

        // None of that was allowed to touch the determinism spine.
        Assert.Equal(hashBefore, w.DayHashes[1]);
    }

    [Fact]
    public void Simulation_Knobs_Do_Not_Retroactively_Change_An_Already_Simulated_Day()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunToDawn();
        var w = sim.World;

        var textBefore = Summarizer.Render(w, 1).Select(l => l.Text).ToList();
        string hashBefore = w.DayHashes[1];
        int marksBefore = w.Townees.Sum(t => t.Marks.Count);
        Assert.True(marksBefore > 0);   // guard: bio_marks_enabled defaults on, so this is non-vacuous

        w.SetKnob("storylet_rate", 0.0);
        w.SetKnob("pressure_rates.trade", 3.0);
        // The trap: bio_marks_enabled sits beside hearsay_required and reads like a display toggle,
        // but it writes hashed Marks at storylet-fire time. Turning it off must not rewrite a day
        // that already fired — that would move the day-hash and break the determinism contract.
        w.SetKnob("bio_marks_enabled", 0);

        Assert.Equal(textBefore, Summarizer.Render(w, 1).Select(l => l.Text));
        Assert.Equal(hashBefore, w.DayHashes[1]);
        Assert.Equal(marksBefore, w.Townees.Sum(t => t.Marks.Count));
    }

    // --- a past day is history: reads must not re-gate it, or rewrite it -------------------

    /// <summary>
    /// A town whose only hearsay-carrier is an adventurer.
    /// <para>The golden cast cannot exercise the occupancy bug, and it is worth saying why: its two
    /// carriers are an innkeep who lives at her own inn and a courier who roams, neither has an
    /// `away` block in their dayplan, and <c>Clockwork</c> only consults <c>plan.Away</c> when the
    /// plan defines one — so both stand in the same rooms every day, and re-gating a past day
    /// against the wrong day's occupancy happens to land on the same answer. The defect is in the
    /// code path either way; the fixture just has to let occupancy actually move. `adventurer-default`
    /// is the one dayplan with an `away` block, so the carrier trait goes to an adventurer.</para>
    /// </summary>
    private static Model.Town CarrierIsAnAdventurer()
    {
        var town = TestSupport.LoadGoldenTown();
        foreach (var id in new[] { "odile-vance", "sela-quick" })
        {
            var d = town.TowneeById[id];
            town = town.WithTownee(d with { Traits = d.Traits.Where(t => t != "gossip-carrier").ToList() });
        }
        var c = town.TowneeById["corvo-lunt"];
        return town.WithTownee(c with { Traits = c.Traits.Append("gossip-carrier").ToList() });
    }

    [Fact]
    public void Re_Reading_An_Old_Day_Renders_It_Against_Its_Own_Occupancy()
    {
        var sim = new Simulation(CarrierIsAnAdventurer());
        sim.RunToDawn();                       // day 1 sealed while the carrier was still in town
        var w = sim.World;

        var textBefore = Summarizer.Render(w, 1).Select(l => l.Text).ToList();
        int poolBefore = Summarizer.Candidates(w, 1).Count;
        var gateBefore = w.Chronicle.Where(e => e.Day == 1).Select(e => e.CarriedByGossip).ToList();
        Assert.True(poolBefore > 1, "fixture must gate several events through the carrier's presence");

        // Now empty the carrier out of the town and move on. World._occupants only ever holds the
        // CURRENT day, so a read of day 1 that consulted it would be gating day 1's events against
        // a town day 1 never saw — collapsing the pool to only the events the carrier was *in*
        // (the one branch of hearsay-lite that reads participants instead of rooms).
        w.SetAway("corvo-lunt", true);
        sim.RunToDawn();
        Assert.True(w.Day > 1);
        Assert.Equal("away", w.PlaceOf(w.TowneeById["corvo-lunt"], 20));   // guard: he really left

        Assert.Equal(textBefore, Summarizer.Render(w, 1).Select(l => l.Text));
        Assert.Equal(poolBefore, Summarizer.Candidates(w, 1).Count);

        // And a read is not allowed to *write*: the gate is the frozen record of who could have
        // carried the story that night, not a scratch field a projection may recompute.
        Assert.Equal(gateBefore, w.Chronicle.Where(e => e.Day == 1).Select(e => e.CarriedByGossip));
    }

    [Fact]
    public void Snapshot_Round_Trip_Preserves_The_Summary()
    {
        // The M2 acceptance is "snapshots round-trip to the same forward hash sequence" — and they
        // did, because summaries were never hashed. The summary itself was dropped on the floor and
        // the suite could not see it. Deriving the summary from the (already snapshotted) chronicle
        // + frozen gate is what makes this pass without Snapshot needing to know summaries exist.
        var town = TestSupport.LoadGoldenTown();
        var simA = new Simulation(town);
        simA.RunToDawn();
        simA.RunToDawn();                      // days 1-2, so day 1 is a past day on both sides

        var before = Summarizer.Render(simA.World, 1).Select(l => l.Text).ToList();
        Assert.NotEmpty(before);

        var simB = new Simulation(Snapshot.Load(town, Snapshot.Save(simA.World)));
        Assert.Equal(before, Summarizer.Render(simB.World, 1).Select(l => l.Text));

        // ...and it survives all the way out through the projection the observatory reads.
        var lines = JsonNode.Parse(WorldView.SummaryJson(simB.World, 1))!["lines"]!.AsArray();
        Assert.Equal(before.Count, lines.Count);
    }

    // --- WorldView projections --------------------------------------------------------------

    [Fact]
    public void SummaryJson_Register_And_Lines_Share_One_Time_Base()
    {
        // The lie, asserted away: `register` was derived live from Config while `lines` came from a
        // dawn-baked cache, so the label could read "report" over gossip prose. One payload, one
        // moment in time — whatever the label claims, the text below it must be that register.
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunToDawn();
        var w = sim.World;

        AssertRegisterMatchesLines(w, dial: 0.05, register: "hearsay", e => e.Hearsay);
        AssertRegisterMatchesLines(w, dial: 0.50, register: "gossip", e => e.Gossip);
        AssertRegisterMatchesLines(w, dial: 0.95, register: "report", e => e.Report);
    }

    private static void AssertRegisterMatchesLines(
        World w, double dial, string register, Func<ChronicleEntry, string> variant)
    {
        w.SetKnob("actionability", dial);
        var json = JsonNode.Parse(WorldView.SummaryJson(w, 1))!;

        Assert.Equal(register, (string)json["register"]!);
        var expected = w.Chronicle.Where(e => e.Day == 1).Select(variant).ToHashSet();
        var texts = json["lines"]!.AsArray().Select(n => (string)n!["text"]!).ToList();
        Assert.NotEmpty(texts);
        Assert.All(texts, t => Assert.Contains(t, expected));
    }

    [Fact]
    public void StatsJson_Tellable_Matches_The_Cli_Soak_Definition()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        var delivered = sim.RunToDawn();
        var w = sim.World;

        // Fishbowl.Cli's soak metric, verbatim: distinct rendered text of the DELIVERED summary.
        // The strip and the soak must answer VFB.Q1 with the same number or the instrument is two
        // instruments. (The strip used to count distinct candidate StoryletIds — a pre-truncation
        // pool bounded by the 12-rule bank, which read 12 on a night that told at most 5 lines.)
        int cliDistinct = delivered.Select(l => l.Text).Distinct().Count();

        var stats = JsonNode.Parse(WorldView.StatsJson(w, 1))!;
        Assert.Equal(cliDistinct, (int)stats["tellable"]!);

        // The property the old number could never have: it answers to summary_lines.
        Assert.True((int)stats["tellable"]! <= w.Config.SummaryLines);
        w.SetKnob("summary_lines", 3);
        Assert.True((int)JsonNode.Parse(WorldView.StatsJson(w, 1))!["tellable"]! <= 3);

        // The pool is still reported — it separates a generation problem from a truncation one —
        // but it is now named for what it is, and it is the looser of the two bounds.
        Assert.True((int)JsonNode.Parse(WorldView.StatsJson(w, 1))!["pool"]! >= 3);
    }

    [Fact]
    public void Starvation_Does_Not_Fire_On_An_Unstarted_Day()
    {
        // distinct < 4 could not tell "not yet simulated" from "simulated and barren" — opposite
        // states — so the lamp lit at boot on a day-0 sim with zero events. DayHashes is the exact
        // "this day finalized" witness.
        var world = World.Build(TestSupport.LoadGoldenTown());
        var boot = JsonNode.Parse(WorldView.StatsJson(world, 1))!;
        Assert.Equal(0, (int)boot["events"]!);
        Assert.False((bool)boot["starvation"]!);

        // A finished day still reports honestly against the delivered-line threshold.
        var sim = new Simulation(world);
        sim.RunToDawn();
        var done = JsonNode.Parse(WorldView.StatsJson(world, 1))!;
        Assert.True((int)done["events"]! > 0);
        Assert.Equal((int)done["tellable"]! < 4, (bool)done["starvation"]!);
    }
}

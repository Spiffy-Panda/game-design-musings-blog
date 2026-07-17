using System.Text.Json.Nodes;
using Fishbowl.Core.Api;
using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// The across-night variety measure — "how much does this town actually say?".
///
/// <para><b>These tests matter more than usual.</b> This instrument exists to judge a
/// novelty/fatigue change to <see cref="Summarizer"/>'s ordering that is being written by someone
/// else, and an instrument that can be moved without the town saying anything new is worth less
/// than no instrument. So the tests pin what the numbers <i>mean</i>, not what they currently
/// equal.</para>
/// </summary>
public class VarietyTests
{
    /// <summary>
    /// <b>The defect in VFB.Q1, stated as an executable fact.</b> The old metric counts distinct
    /// texts <i>within</i> a summary that has already been truncated to <c>summary_lines</c>, so it
    /// is <c>min(pool, summary_lines)</c> averaged and can never report more than the truncation
    /// allows — no matter how much or how little the town has to say. That is the whole reason it
    /// reads a flat 5.00 with two-thirds of the bank deleted.
    /// <para>This test does not assert 5.00. It asserts the ceiling, which is the property that
    /// makes 5.00 meaningless.</para>
    /// </summary>
    [Fact]
    public void Vfb_Q1_Can_Never_Exceed_SummaryLines_Which_Is_Why_It_Is_Saturated()
    {
        var sim = Run(7);
        var v = Variety.Measure(sim.World);

        Assert.True(v.Vfb_Q1_AvgDistinctPerNight <= sim.World.Config.SummaryLines,
            $"VFB.Q1 {v.Vfb_Q1_AvgDistinctPerNight} exceeded its own ceiling of {sim.World.Config.SummaryLines}");

        // ... and the across-night measure is not bounded by it: over several nights a town that
        // says anything at all says more distinct things than fit in one night's summary. This is
        // the gap the new number exists to show.
        Assert.True(v.DistinctTexts > sim.World.Config.SummaryLines,
            "the golden town says no more in 7 nights than fits in one summary — check the fixture");
    }

    /// <summary>The headline counts <b>sentences</b>, not rule ids. Two fires of one rule with a
    /// different cast or room are two different sentences; two fires that render identically are
    /// one. That distinction is the entire point — <c>_binding</c> pins the cast and <c>place</c>
    /// pins the room, so rules mostly re-render the same sentence, and a rule-id count would score
    /// that as variety.</summary>
    [Fact]
    public void Distinct_Texts_Counts_Sentences_Not_Rule_Ids()
    {
        var sim = Run(7);
        var v = Variety.Measure(sim.World);

        var nights = Enumerable.Range(1, 7).Select(d => Summarizer.Render(sim.World, d)).ToList();
        var texts = nights.SelectMany(n => n.Select(l => l.Text)).ToList();

        Assert.Equal(texts.Distinct(StringComparer.Ordinal).Count(), v.DistinctTexts);
        Assert.Equal(texts.Count, v.DeliveredLines);
        Assert.Equal(nights.SelectMany(n => n.Select(l => l.StoryletId)).Distinct(StringComparer.Ordinal).Count(),
                     v.RulesTold);

        // The two are genuinely different questions: a rule-id count cannot exceed the bank, a text
        // count can. If these were interchangeable the measure would be pointless.
        Assert.True(v.RulesTold <= v.RulesFired);
    }

    /// <summary>The repeat arithmetic holds together. A line that repeats the night before also
    /// repeats "some prior night", never the reverse; and night 1 is excluded from the denominator
    /// because it cannot repeat anything and would dilute the rate for free.</summary>
    [Fact]
    public void Repeat_Counts_Are_Internally_Consistent()
    {
        var sim = Run(7);
        var v = Variety.Measure(sim.World);

        Assert.True(v.RepeatsOfPreviousNight <= v.RepeatsOfAnyPriorNight);
        Assert.True(v.RepeatsOfAnyPriorNight <= v.LinesWithAPriorNight);
        Assert.Equal(v.DeliveredLines - Summarizer.Render(sim.World, 1).Count, v.LinesWithAPriorNight);
        Assert.InRange(v.RepeatRateAnyPriorNight, 0.0, 1.0);
        Assert.InRange(v.NoveltyRate, 0.0, 1.0);

        // Every delivered line is either the first time that sentence appeared or a repeat of a
        // prior night. Nothing falls between the two.
        Assert.Equal(v.DeliveredLines, v.DistinctTexts + v.RepeatsOfAnyPriorNight);
    }

    /// <summary>Told is a subset of fired, and the ratio is the share of what happened that anyone
    /// heard about.</summary>
    [Fact]
    public void Told_Never_Exceeds_Fired()
    {
        var v = Variety.Measure(Run(7).World);
        Assert.Equal(v.DeliveredLines, v.BeatsTold);
        Assert.True(v.BeatsTold <= v.BeatsFired);
        Assert.InRange(v.ToldPerFired, 0.0, 1.0);
        Assert.True(v.RulesFiredButNeverTold >= 0);
    }

    /// <summary>The measure is machine-readable through <c>--report</c>, which is the point: the
    /// change this instrument will judge should be judged from a file, not from a screenshot of a
    /// terminal.</summary>
    [Fact]
    public void Variety_Is_Carried_In_The_Report_Json()
    {
        var sim = Run(3);
        var root = JsonNode.Parse(RunReport.ToJson(sim.World, 3))!.AsObject();
        var v = Variety.Measure(sim.World);

        var node = root["variety"]!;
        Assert.Equal(v.DistinctTexts, node["distinct_texts"]!.GetValue<int>());
        Assert.Equal(v.DeliveredLines, node["delivered_lines"]!.GetValue<int>());
        Assert.Equal(v.BeatsFired, node["beats_fired"]!.GetValue<int>());
        // The old number keeps its place and its label.
        Assert.Equal(v.Vfb_Q1_AvgDistinctPerNight, node["vfb_q1_avg_distinct_per_night"]!.GetValue<double>(), 3);
        Assert.Contains("SATURATED", node["vfb_q1_note"]!.GetValue<string>());
    }

    private static Simulation Run(int days)
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunDays(days);
        return sim;
    }
}

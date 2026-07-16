using Fishbowl.Core.Engine;
using Fishbowl.Core.Text;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>VFB.M3 — storylets + summary acceptance: the golden day reproduces its scripted
/// beat types and participants; the actionability dial renders three distinct reads; and
/// hearsay-lite gates the summary.</summary>
public class M3_StoryletSummaryTests
{
    [Fact]
    public void Golden_Day_Reproduces_Its_Beats()
    {
        var town = TestSupport.LoadGoldenTown();
        Assert.NotNull(town.Golden);
        var sim = new Simulation(town);
        sim.RunToDawn(); // day 1

        var fired = sim.World.Chronicle
            .Where(e => e.Day == 1)
            .Select(e => (e.StoryletId, Who: e.Participants.OrderBy(x => x).ToArray()))
            .ToList();

        foreach (var beat in town.Golden!.ExpectedBeats)
        {
            var want = beat.Participants.OrderBy(x => x).ToArray();
            Assert.True(
                fired.Any(f => f.StoryletId == beat.Storylet && f.Who.SequenceEqual(want)),
                $"golden beat not reproduced: {beat.Storylet} [{string.Join(", ", beat.Participants)}]");
        }
    }

    [Fact]
    public void Actionability_Dial_Has_Three_Distinct_Reads()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunToDawn();
        var entry = sim.World.Chronicle.First(e => e.StoryletId == "rent-quarrel");

        string hearsay = Actionability.Pick(entry, 0.10);
        string gossip = Actionability.Pick(entry, 0.50);
        string report = Actionability.Pick(entry, 0.90);

        Assert.Equal(Register.Hearsay, Actionability.Of(0.10));
        Assert.Equal(Register.Gossip, Actionability.Of(0.50));
        Assert.Equal(Register.Report, Actionability.Of(0.90));
        Assert.False(string.IsNullOrWhiteSpace(hearsay));
        Assert.NotEqual(hearsay, gossip);
        Assert.NotEqual(gossip, report);
        Assert.NotEqual(hearsay, report);
    }

    [Fact]
    public void Hearsay_Lite_Gates_The_Summary()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunToDawn();
        var candidates = Summarizer.Candidates(sim.World, 1);
        Assert.NotEmpty(candidates);
        Assert.All(candidates, e => Assert.True(e.CarriedByGossip)); // required → all carried

        // Every chronicle entry carries a full because-list (AGR.2 explainability).
        Assert.All(sim.World.Chronicle.Where(e => e.Day == 1), e => Assert.NotEmpty(e.Because));
    }

    [Fact]
    public void Bio_Marks_Toggle_Controls_Appended_Marks()
    {
        // FB.8: with marks on, the departure leaves a dated line on Tam's bio; off, it doesn't.
        Assert.True(MarksAfterDay1(enabled: true) > 0);
        Assert.Equal(0, MarksAfterDay1(enabled: false));
    }

    private static int MarksAfterDay1(bool enabled)
    {
        var world = World.Build(TestSupport.LoadGoldenTown());
        world.SetKnob("bio_marks_enabled", enabled ? 1 : 0);
        var sim = new Simulation(world);
        sim.RunToDawn();
        return world.Townees.Sum(t => t.Marks.Count(m => m.Day == 1));
    }
}

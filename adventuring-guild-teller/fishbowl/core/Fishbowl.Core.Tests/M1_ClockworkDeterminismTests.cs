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
        Assert.Equal(
            new[] { "b8d15299d8817639", "e3478bc4ff7d4848", "02bc86b987c547c3" },
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

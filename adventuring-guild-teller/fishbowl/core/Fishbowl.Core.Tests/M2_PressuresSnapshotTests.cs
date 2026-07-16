using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>VFB.M2 — pressures acceptance: snapshots round-trip to the same forward hash
/// sequence, and the pressure-rate knob visibly bends a drive.</summary>
public class M2_PressuresSnapshotTests
{
    [Fact]
    public void Snapshot_Load_Run_Reproduces_The_No_Reload_Hash_Sequence()
    {
        var town = TestSupport.LoadGoldenTown();

        // Run A: straight through, no reload.
        var simA = new Simulation(town);
        simA.RunToDawn();                       // finish day 1 → at dawn of day 2
        string snap = Snapshot.Save(simA.World);
        simA.RunDays(3);                        // days 2,3,4

        // Run B: reload the day-2 dawn snapshot, then run the same span.
        var simB = new Simulation(Snapshot.Load(town, snap));
        simB.RunDays(3);

        foreach (int d in new[] { 2, 3, 4 })
            Assert.Equal(simA.World.DayHashes[d], simB.World.DayHashes[d]);
    }

    [Fact]
    public void Pressure_Rate_Knob_Bends_The_Trade_Drive()
    {
        // Compare after 6 slots — pure drift, before any storylet effect touches Petch's trade.
        double normal = PetchTradeAfterSlots(rate: 1.0, slots: 6);
        double faster = PetchTradeAfterSlots(rate: 2.5, slots: 6);
        Assert.True(faster < normal, $"expected faster depletion: faster={faster:0.000} normal={normal:0.000}");
    }

    private static double PetchTradeAfterSlots(double rate, int slots)
    {
        var world = World.Build(TestSupport.LoadGoldenTown());
        world.SetKnob("pressure_rates.trade", rate);
        var sim = new Simulation(world);
        for (int i = 0; i < slots; i++) sim.StepSlot();
        return world.TowneeById["petch"].Pressure("trade");
    }
}

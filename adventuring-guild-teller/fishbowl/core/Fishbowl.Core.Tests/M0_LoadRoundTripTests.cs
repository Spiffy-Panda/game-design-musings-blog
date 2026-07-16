using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>VFB.M0 — scaffold acceptance: data loads clean, the Godot-stringify round-trip
/// parses, and an empty town hashes stably across runs.</summary>
public class M0_LoadRoundTripTests
{
    [Fact]
    public void GoldenTown_Loads_And_Validates()
    {
        var town = TestSupport.LoadGoldenTown();
        Assert.Equal(12, town.Townees.Count);
        Assert.Equal(6, town.Places.Count(p => p.Board));
        Assert.Equal(2, town.Townees.Count(t => t.Adventurer));
        Assert.True(town.Storylets.Count >= 7);
        Assert.NotNull(town.Golden);
    }

    [Fact]
    public void EmptyTown_Hash_Is_Stable_Across_Runs()
    {
        string a = World.Build(TestSupport.MakeEmptyTown()).ComputeDayHash();
        string b = World.Build(TestSupport.MakeEmptyTown()).ComputeDayHash();
        Assert.Equal(a, b);
        Assert.Equal(16, a.Length); // 64-bit hex
    }

    [Fact]
    public void GodotStringify_RoundTrip_Of_Every_File_Loads_And_Matches()
    {
        // The whole data/ tree, float-ified as Godot's JSON.stringify would emit it
        // (4 -> 4.0), must still load, validate, and simulate to the identical day-1 hash.
        string reference = HashDay1(TestSupport.DataDir);

        string tempData = Path.Combine(TestSupport.TempDir(), "data");
        TestSupport.WriteFloatifiedData(tempData);
        string floatified = HashDay1(tempData);

        Assert.Equal(reference, floatified);
    }

    private static string HashDay1(string dataDir)
    {
        var sim = new Simulation(TownLoader.Load(dataDir));
        sim.RunToDawn();
        return sim.World.DayHashes[1];
    }
}

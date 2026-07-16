using System.Text.Json.Nodes;
using Fishbowl.Core.Determinism;
using Fishbowl.Core.Json;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>The determinism spine itself: FNV-1a known vectors, canonical JSON rules, and
/// SplitMix64 stream independence — the primitives the whole method rests on.</summary>
public class DeterminismPrimitivesTests
{
    [Theory]
    [InlineData("", "cbf29ce484222325")]          // FNV-1a 64 offset basis (empty input)
    [InlineData("a", "af63dc4c8601ec8c")]
    [InlineData("foobar", "85944171f73967e8")]
    public void Fnv1a64_Matches_Known_Vectors(string input, string expectedHex)
        => Assert.Equal(expectedHex, FnvHash.Hex(FnvHash.Hash64(input)));

    [Fact]
    public void Canonical_Sorts_Keys_And_Emits_Ints_As_Ints()
    {
        var node = new JsonObject { ["b"] = 2, ["a"] = 4.0, ["c"] = 0.5 };
        Assert.Equal("{\"a\":4,\"b\":2,\"c\":0.500000}", CanonicalJson.Canonicalize(node));
    }

    [Fact]
    public void Canonical_Treats_Godot_Floatified_Int_Identically()
    {
        // 4 and 4.0 must canonicalize the same, so a Godot-round-tripped snapshot hashes equal.
        var a = CanonicalJson.Canonicalize(JsonNode.Parse("{\"x\":4}"));
        var b = CanonicalJson.Canonicalize(JsonNode.Parse("{\"x\":4.0}"));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Rng_Same_Seed_Same_Sequence_Different_Seed_Differs()
    {
        var a = new Rng(1234);
        var b = new Rng(1234);
        var c = new Rng(5678);
        for (int i = 0; i < 20; i++) Assert.Equal(a.NextUInt64(), b.NextUInt64());
        Assert.NotEqual(new Rng(1234).NextUInt64(), c.NextUInt64());
    }

    [Fact]
    public void Named_Streams_Are_Independent()
    {
        var plans = Rng.Stream(1123, 1, "plans");
        var story = Rng.Stream(1123, 1, "storylets");
        // First draws of two same-day streams should not collide (independence, not proof).
        Assert.NotEqual(plans.NextUInt64(), story.NextUInt64());
    }

    [Fact]
    public void TolerantInt_Accepts_Whole_Float()
    {
        var opts = DataJson.Options;
        Assert.Equal(4, System.Text.Json.JsonSerializer.Deserialize<int>("4.0", opts));
        Assert.Equal(48, System.Text.Json.JsonSerializer.Deserialize<int>("48", opts));
    }
}

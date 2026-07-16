namespace Fishbowl.Core.Determinism;

/// <summary>
/// A named, seeded RNG stream. SplitMix64 generator (fully specified bit math, so the
/// sequence is identical on every platform and .NET version). Streams are derived from
/// <c>(world_seed, day, stream_name)</c> via <see cref="FnvHash"/> so that adding a draw
/// in one system never shifts the sequence another system sees (PLAN "Determinism").
///
/// The canonical stream names are <c>plans</c>, <c>storylets</c>, <c>drift</c>, <c>gen</c>.
/// </summary>
public sealed class Rng
{
    private ulong _state;

    public Rng(ulong seed) => _state = seed;

    /// <summary>Derive an independent stream for a given day + system name.</summary>
    public static Rng Stream(long worldSeed, int day, string streamName)
        => new(FnvHash.Hash64($"{worldSeed}|{day}|{streamName}"));

    /// <summary>Derive a sub-stream (e.g. per-townee) that stays independent of the parent's cursor.</summary>
    public static Rng SubStream(long worldSeed, int day, string streamName, string key)
        => new(FnvHash.Hash64($"{worldSeed}|{day}|{streamName}|{key}"));

    public ulong NextUInt64()
    {
        // SplitMix64.
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform double in [0, 1). 53 bits of mantissa precision.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Uniform int in [0, maxExclusive). Lemire-style bounded, bias-free.</summary>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        ulong m = (ulong)(uint)maxExclusive * (uint)(NextUInt64() >> 32);
        return (int)(m >> 32);
    }
}

namespace MorningQueue.Core;

/// <summary>
/// Deterministic PCG32 stream — the composer's only randomness source. Self-owned so the
/// generated weeks are stable across .NET versions and platforms (System.Random's seeded
/// algorithm is documented as "subject to change"; Godot's RandomNumberGenerator is gone
/// with the GDScript generator). Rebaseline ruled at MQT.D2a: the streams changed once at
/// the port; the golden-week fixtures pin them from now on.
/// </summary>
public sealed class Rng
{
    private const ulong Mult = 6364136223846793005UL;
    private const ulong Inc = 1442695040888963407UL;

    private ulong _state;

    public Rng(int seed)
    {
        // pcg32_srandom(initstate = seed, initseq fixed): state = 0; step; += seed; step.
        _state = 0UL;
        NextUInt();
        _state += (ulong)seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong old = _state;
        _state = old * Mult + Inc;
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    /// <summary>Uniform int in [min, max], both inclusive (mirrors rng.randi_range).</summary>
    public int RangeInt(int min, int max)
        => max <= min ? min : min + (int)(NextUInt() % (uint)(max - min + 1));

    /// <summary>Uniform double in [0, 1) (mirrors rng.randf).</summary>
    public double NextDouble() => NextUInt() / 4294967296.0;

    public T Pick<T>(IReadOnlyList<T> list) => list[RangeInt(0, list.Count - 1)];
}

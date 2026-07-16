using System.Text;

namespace Fishbowl.Core.Determinism;

/// <summary>
/// FNV-1a 64-bit hash. Cross-process stable by construction (unlike .NET
/// <c>HashCode.Combine</c>, which is per-process seeded — the exact trap the mined
/// Autonome project fell into; see PLAN appendix MUA.N1). Used for both the dawn
/// day-hash and for deriving named RNG stream seeds.
/// </summary>
public static class FnvHash
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Hash64(ReadOnlySpan<byte> bytes)
    {
        ulong h = Offset;
        foreach (byte b in bytes)
        {
            h ^= b;
            h *= Prime;
        }
        return h;
    }

    /// <summary>UTF-8 hash of a string. Encoding is fixed (UTF-8, no BOM) so the
    /// value never depends on platform default encodings.</summary>
    public static ulong Hash64(string s) => Hash64(Encoding.UTF8.GetBytes(s));

    /// <summary>16-char lowercase hex — the form shown in the observatory, logged by
    /// the CLI, and pinned in golden tests.</summary>
    public static string Hex(ulong h) => h.ToString("x16");
}

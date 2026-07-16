using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fishbowl.Core.Json;

/// <summary>
/// Canonical serialization of a <see cref="JsonNode"/> for hashing — the exact answer to
/// appendix MUA.Q5 (what enters the day-hash: float precision, dict ordering):
/// <list type="bullet">
///   <item>object keys sorted by ordinal</item>
///   <item>integral numbers emitted as integers ("ints-as-ints"), so Godot's 4.0 hashes identically to 4</item>
///   <item>non-integral numbers rounded to 6 decimals and formatted invariantly (platform-stable)</item>
///   <item>negative zero normalized to zero</item>
/// </list>
/// The output is a compact, whitespace-free string; the day-hash is FNV-1a 64 over its UTF-8 bytes.
/// </summary>
public static class CanonicalJson
{
    private const int Decimals = 6;

    public static string Canonicalize(JsonNode? node)
    {
        var sb = new StringBuilder(256);
        Write(sb, node);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, JsonNode? node)
    {
        switch (node)
        {
            case null:
                sb.Append("null");
                break;
            case JsonObject obj:
                sb.Append('{');
                bool firstProp = true;
                foreach (var kv in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    if (!firstProp) sb.Append(',');
                    firstProp = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    Write(sb, kv.Value);
                }
                sb.Append('}');
                break;
            case JsonArray arr:
                sb.Append('[');
                for (int i = 0; i < arr.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Write(sb, arr[i]);
                }
                sb.Append(']');
                break;
            case JsonValue val:
                WriteValue(sb, val);
                break;
            default:
                throw new InvalidOperationException($"Unhandled node type {node.GetType().Name}.");
        }
    }

    private static void WriteValue(StringBuilder sb, JsonValue val)
    {
        if (val.TryGetValue(out bool b)) { sb.Append(b ? "true" : "false"); return; }
        if (val.TryGetValue(out string? s)) { WriteString(sb, s!); return; }

        // Numbers. A JsonValue may be backed either by a CLR primitive (node built in code)
        // or by a JsonElement (parsed) — try both so canonicalization never depends on how
        // the node was constructed. Integral values emit as integers ("ints-as-ints").
        if (val.TryGetValue(out int i)) { sb.Append(i.ToString(CultureInfo.InvariantCulture)); return; }
        if (val.TryGetValue(out long l)) { sb.Append(l.ToString(CultureInfo.InvariantCulture)); return; }
        if (val.TryGetValue(out double d)) { WriteDouble(sb, d); return; }
        if (val.TryGetValue(out decimal m)) { WriteDouble(sb, (double)m); return; }
        if (val.TryGetValue(out JsonElement el) && el.ValueKind == JsonValueKind.Number) { WriteDouble(sb, el.GetDouble()); return; }

        throw new InvalidOperationException($"Unhandled JSON value for canonicalization: {val.ToJsonString()}");
    }

    private static void WriteDouble(StringBuilder sb, double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d))
            throw new InvalidOperationException($"Non-finite number {d} cannot be canonicalized.");

        if (d == Math.Floor(d) && Math.Abs(d) < 9.007199254740992e15) // exact-integer double range
        {
            sb.Append(((long)d).ToString(CultureInfo.InvariantCulture));
            return;
        }
        double rounded = Math.Round(d, Decimals, MidpointRounding.AwayFromZero);
        if (rounded == 0.0) rounded = 0.0; // normalize -0
        sb.Append(rounded.ToString("F" + Decimals, CultureInfo.InvariantCulture));
    }

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}

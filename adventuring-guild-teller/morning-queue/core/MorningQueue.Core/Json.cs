using System.Text.Json;
using System.Text.Json.Nodes;

namespace MorningQueue.Core;

/// <summary>
/// Shared System.Text.Json configuration and small parsing helpers. The banks use
/// snake_case keys, so the naming policy maps them onto PascalCase properties; unknown
/// members are tolerated (the default), comments and trailing commas are allowed so the
/// hand-authored JSON stays forgiving.
/// </summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Godot's JSON round-trip (parse -> stringify) turns every whole number into a "4.0"
        // float. These converters let every `int`/`int?` model field bind such values, so the
        // real boot payload from DeckLoader.gd deserializes exactly like the raw file text.
        Converters =
        {
            new TolerantIntConverter(),
            new TolerantNullableIntConverter(),
        },
    };

    /// <summary>
    /// Deserialize a keyed sub-table (id -&gt; record), skipping "_"-prefixed metadata rows
    /// (`_tab`, `_note`, …) and null values — the same filter the GDScript loader applied.
    /// </summary>
    public static Dictionary<string, T> ParseTable<T>(JsonNode? node)
    {
        var outp = new Dictionary<string, T>();
        if (node is JsonObject obj)
        {
            foreach (var kv in obj)
            {
                if (kv.Key.StartsWith('_') || kv.Value is null)
                    continue;
                var value = kv.Value.Deserialize<T>(Options);
                if (value is not null)
                    outp[kv.Key] = value;
            }
        }
        return outp;
    }

    /// <summary>Parse a keyed int table (e.g. rankup_thresholds), skipping "_" rows.</summary>
    public static Dictionary<string, int> ParseIntTable(JsonNode? node)
    {
        var outp = new Dictionary<string, int>();
        if (node is JsonObject obj)
        {
            foreach (var kv in obj)
            {
                if (kv.Key.StartsWith('_') || kv.Value is null)
                    continue;
                // Tolerate Godot's "4.0" float form (see TolerantIntConverter): read as double
                // and round, so a round-tripped threshold binds exactly like the raw integer.
                if (kv.Value is JsonValue v && v.TryGetValue<double>(out var n))
                    outp[kv.Key] = (int)Math.Round(n);
            }
        }
        return outp;
    }

    /// <summary>True if a JsonElement holds a JSON number.</summary>
    public static bool IsNumber(JsonElement? e) => e.HasValue && e.Value.ValueKind == JsonValueKind.Number;

    /// <summary>Read a JsonElement as a double, or null if it is not a number.</summary>
    public static double? AsNumber(JsonElement? e)
        => IsNumber(e) ? e!.Value.GetDouble() : null;
}

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fishbowl.Core.Data;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Tests;

internal static class TestSupport
{
    public static string DataDir => ProjectPaths.DataDir();

    public static Town LoadGoldenTown() => TownLoader.Load(DataDir);

    public static Town MakeEmptyTown() => new()
    {
        Config = new SimConfig(),
        Places = Array.Empty<PlaceDto>(),
        Townees = Array.Empty<TowneeDto>(),
        DayPlans = new Dictionary<string, DayPlanDto>(),
        Traits = Array.Empty<TraitDto>(),
        Storylets = Array.Empty<StoryletDto>(),
        Golden = null,
        PlaceById = new Dictionary<string, PlaceDto>(),
        TowneeById = new Dictionary<string, TowneeDto>(),
        TraitById = new Dictionary<string, TraitDto>(),
        StoryletById = new Dictionary<string, StoryletDto>(),
    };

    public static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "fishbowl-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Simulate Godot's <c>JSON.stringify</c>: every number is emitted in float form, so whole
    /// numbers become <c>N.0</c>. This is the exact payload shape that broke strict int binding
    /// in the sibling desk prototype (DEV-LOG 2026-07-15) — the reason the tolerant-int converter
    /// exists. Comments and trailing commas in the source are dropped (as a real parse would).
    /// </summary>
    public static string GodotStringify(string json)
    {
        var node = JsonNode.Parse(json, nodeOptions: null,
            documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var sb = new StringBuilder();
        WriteGodot(sb, node);
        return sb.ToString();
    }

    /// <summary>Copy the real data/ tree into <paramref name="destDataDir"/>, float-ifying every
    /// .json file through <see cref="GodotStringify"/>. Returns the destination path.</summary>
    public static string WriteFloatifiedData(string destDataDir)
    {
        foreach (var src in Directory.GetFiles(DataDir, "*.json", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(DataDir, src);
            string dest = Path.Combine(destDataDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllText(dest, GodotStringify(File.ReadAllText(src)));
        }
        return destDataDir;
    }

    private static void WriteGodot(StringBuilder sb, JsonNode? node)
    {
        switch (node)
        {
            case null: sb.Append("null"); break;
            case JsonObject obj:
                sb.Append('{');
                bool first = true;
                foreach (var kv in obj)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(JsonSerializer.Serialize(kv.Key)).Append(':');
                    WriteGodot(sb, kv.Value);
                }
                sb.Append('}');
                break;
            case JsonArray arr:
                sb.Append('[');
                for (int i = 0; i < arr.Count; i++) { if (i > 0) sb.Append(','); WriteGodot(sb, arr[i]); }
                sb.Append(']');
                break;
            case JsonValue val:
                var el = val.GetValue<JsonElement>();
                switch (el.ValueKind)
                {
                    case JsonValueKind.String: sb.Append(JsonSerializer.Serialize(el.GetString())); break;
                    case JsonValueKind.True: sb.Append("true"); break;
                    case JsonValueKind.False: sb.Append("false"); break;
                    case JsonValueKind.Number:
                        if (el.TryGetInt64(out long l) && l == el.GetDouble())
                            sb.Append(l.ToString(CultureInfo.InvariantCulture)).Append(".0");   // 4 -> 4.0
                        else
                            sb.Append(el.GetDouble().ToString("R", CultureInfo.InvariantCulture));
                        break;
                    default: sb.Append("null"); break;
                }
                break;
        }
    }
}

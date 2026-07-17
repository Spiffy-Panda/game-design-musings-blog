using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fishbowl.Core.Data;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Tests;

internal static class TestSupport
{
    /// <summary>The LIVE town (`data/`) — all features on, postings and sites included. Only the
    /// Godot-stringify round-trip sweeps this, on purpose: it must cover every authored file.</summary>
    public static string DataDir => ProjectPaths.DataDir();

    /// <summary>The FROZEN golden fixture — posting-free, and no longer `data/` (PNO.D2, ruled
    /// 2026-07-16). Every acceptance test loads through here, which is what keeps seed-independence
    /// and the golden day's 7 beats true while the live town grows features.</summary>
    public static Town LoadGoldenTown() => TownLoader.Load(ProjectPaths.GoldenTownDir());

    /// <summary>
    /// The LIVE town as a <see cref="Town"/>. <b>Board tests structurally cannot use the fixture</b> —
    /// it is posting-free forever by ruling (PNO.D2), so there is no board in it to test. That leaves
    /// this or a hand-built town, and the live one is where postings are authored.
    /// <para><b>Therefore: assert the machine, never this town's current numbers.</b> A test that pins
    /// "files on day 2, expires on day 6" against a directory whose whole purpose is to grow would be
    /// pinning today's authoring, and the moment someone re-authors a dayplan it reddens without a bug.
    /// That is exactly the trap `golden/day1.json` fell into (`FBT.Q1`): it pinned two beats that fired
    /// only because of a defect, and looked identical to a test encoding a requirement. Assert
    /// invariants that must hold for any posting town — paper files, stands, expires, carries a
    /// because-list, never shows a nonsense countdown.</para>
    /// </summary>
    public static Town LoadLiveTown() => TownLoader.Load(ProjectPaths.DataDir());

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

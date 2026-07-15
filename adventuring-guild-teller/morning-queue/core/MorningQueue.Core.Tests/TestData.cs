using System.IO;

namespace MorningQueue.Core.Tests;

/// <summary>
/// Locates the project's real data/ folder by walking up from the test assembly's base
/// directory until it finds the marker file data/references.json — no CWD assumptions, so
/// the tests run identically from `dotnet test`, an IDE, or CI.
/// </summary>
public static class TestData
{
    private static readonly string DataDir = Locate();

    private static string Locate()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "references.json");
            if (File.Exists(candidate))
                return Path.Combine(dir.FullName, "data");
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate data/references.json walking up from " + AppContext.BaseDirectory);
    }

    public static string Read(string fileName) => File.ReadAllText(Path.Combine(DataDir, fileName));

    public static string References => Read("references.json");
    public static string Townees => Read("townees.json");
    public static string Adventurers => Read("adventurers.json");
    public static string Generation => Read("generation.json");
    public static string Visitors => Read("visitors.json");
    public static string LocaleEn => Read(Path.Combine("locales", "en.json"));

    /// <summary>
    /// Build the boot banks payload the bridge assembles: references verbatim, the two
    /// directories reduced to their inner id-&gt;record maps, generation verbatim.
    /// </summary>
    public static string BanksPayload()
    {
        var townees = InnerMap(Townees, "townees");
        var advs = InnerMap(Adventurers, "adventurers");
        return $"{{\"references\":{References},\"townees\":{townees},\"adventurers\":{advs},\"generation\":{Generation}}}";
    }

    private static string InnerMap(string fileJson, string key)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(fileJson);
        return doc.RootElement.GetProperty(key).GetRawText();
    }

    /// <summary>
    /// Reproduce Godot's lossy JSON round trip: GDScript's JSON.parse_string reads every JSON
    /// number as a float and JSON.stringify re-emits whole numbers with a trailing ".0"
    /// (4 -> 4.0). This rewrites `json` so every integer-valued number carries an explicit ".0",
    /// producing the exact text DeckLoader.gd hands CoreBridge over the wire. Fractional numbers
    /// and non-numbers are left untouched. Used by the boot round-trip regression tests.
    /// </summary>
    public static string Godotify(string json)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json,
            documentOptions: new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        var buffer = new System.IO.MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(buffer))
            GodotifyWrite(node, writer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void GodotifyWrite(System.Text.Json.Nodes.JsonNode? node,
                                      System.Text.Json.Utf8JsonWriter writer)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject obj:
                writer.WriteStartObject();
                foreach (var kv in obj)
                {
                    writer.WritePropertyName(kv.Key);
                    GodotifyWrite(kv.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case System.Text.Json.Nodes.JsonArray arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                    GodotifyWrite(item, writer);
                writer.WriteEndArray();
                break;
            case System.Text.Json.Nodes.JsonValue val:
                var elem = val.GetValue<System.Text.Json.JsonElement>();
                if (elem.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var d = elem.GetDouble();
                    // Emit whole numbers as Godot does: with an explicit ".0" suffix.
                    writer.WriteRawValue(d == Math.Floor(d) && !double.IsInfinity(d)
                        ? d.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                        : elem.GetRawText());
                }
                else
                {
                    elem.WriteTo(writer);
                }
                break;
            case null:
                writer.WriteNullValue();
                break;
        }
    }
}

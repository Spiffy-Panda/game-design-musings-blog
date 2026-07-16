using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Fishbowl.Core.Json;

/// <summary>
/// Shared System.Text.Json configuration for all data/ loads and snapshot writes.
/// Conventions mined from Autonome's DataLoader (appendix MUA.M6): case-insensitive,
/// comments + trailing commas allowed, and writes are LF-normalized and BOM-free
/// (BOMs broke their STJ loads). Adds the tolerant-int converter (this project's
/// day-one guard) on top.
/// </summary>
public static class DataJson
{
    public static readonly JsonSerializerOptions Options = Build(indented: false);
    public static readonly JsonSerializerOptions Pretty = Build(indented: true);

    private static JsonSerializerOptions Build(bool indented) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = indented,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        // Relax escaping so authored prose (apostrophes, em dashes) round-trips readably.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new TolerantIntConverter(), new TolerantInt64Converter() },
    };

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new JsonException($"Deserialized null for {typeof(T).Name}.");

    /// <summary>Serialize to UTF-8 text with LF line endings and no BOM.</summary>
    public static string Serialize<T>(T value, bool pretty = true)
    {
        string s = JsonSerializer.Serialize(value, pretty ? Pretty : Options);
        return s.Replace("\r\n", "\n");
    }

    /// <summary>Read a file as UTF-8, tolerating (and stripping) a BOM if present.</summary>
    public static string ReadText(string path) =>
        File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    /// <summary>Write a file as UTF-8, LF, no BOM.</summary>
    public static void WriteText(string path, string text) =>
        File.WriteAllText(path, text.Replace("\r\n", "\n"),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

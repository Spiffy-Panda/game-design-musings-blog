using System.Text;
using System.Text.Json.Nodes;

namespace MorningQueue.Core;

/// <summary>
/// Slug -&gt; Title-Case, plus authored overrides from data/locales/en.json ("en" -&gt;
/// overrides). This is the compose-time twin of GDScript's Loc.humanize — the UI's display
/// layer stays Loc; the generator port (WP-E) uses this when composing prose in Core.
///
/// Matches Loc.humanize exactly: overrides win; otherwise "-&gt;" becomes " → ", and "-"/"_"
/// become spaces; each word is capitalised on its first character (the rest is left as-is,
/// so acronyms survive); the arrow token is preserved.
/// </summary>
public sealed class Humanizer
{
    private readonly IReadOnlyDictionary<string, string> _overrides;

    public Humanizer(IReadOnlyDictionary<string, string>? overrides = null)
        => _overrides = overrides ?? new Dictionary<string, string>();

    /// <summary>Build a Humanizer from the raw en.json locale text ("en" -&gt; "overrides").</summary>
    public static Humanizer FromLocaleJson(string localeJson, string locale = "en")
    {
        var overrides = new Dictionary<string, string>();
        if (JsonNode.Parse(localeJson) is JsonObject root
            && root[locale] is JsonObject loc
            && loc["overrides"] is JsonObject ov)
        {
            foreach (var kv in ov)
                if (kv.Value is not null)
                    overrides[kv.Key] = kv.Value.GetValue<string>();
        }
        return new Humanizer(overrides);
    }

    public string Humanize(string raw)
    {
        if (_overrides.TryGetValue(raw, out var over))
            return over;

        var spaced = raw.Replace("->", " → ").Replace('-', ' ').Replace('_', ' ');
        var words = spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            var w = words[i];
            if (w == "→")
                sb.Append(w);
            else
                sb.Append(char.ToUpperInvariant(w[0])).Append(w.AsSpan(1));
        }
        return sb.ToString();
    }
}

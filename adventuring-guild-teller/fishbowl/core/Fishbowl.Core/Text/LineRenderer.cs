using System.Globalization;
using System.Text.RegularExpressions;
using Fishbowl.Core.Engine;

namespace Fishbowl.Core.Text;

/// <summary>
/// Renders a storylet line template against a binding. Placeholders:
/// <c>{A}</c> → townee name, <c>{place}</c> → place name, <c>{slot}</c> → slot index,
/// <c>{A.purse}</c> → that townee's drive value (2 decimals). Unknown tokens pass through
/// unchanged so authoring mistakes are visible rather than silently blanked.
/// </summary>
public static partial class LineRenderer
{
    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex TokenRx();

    public static string Render(string template, World world, IReadOnlyDictionary<string, string> bind, string placeId, int slot)
    {
        string placeName = world.Town.PlaceById.TryGetValue(placeId, out var p) ? p.Name : placeId;
        return TokenRx().Replace(template, m =>
        {
            string tok = m.Groups[1].Value;
            if (tok == "place") return placeName;
            if (tok == "slot") return slot.ToString(CultureInfo.InvariantCulture);

            int dot = tok.IndexOf('.');
            if (dot < 0)
            {
                // bare role → name
                return bind.TryGetValue(tok, out var id) && world.TowneeById.TryGetValue(id, out var t)
                    ? t.Name : m.Value;
            }

            string role = tok[..dot], drive = tok[(dot + 1)..];
            if (bind.TryGetValue(role, out var rid) && world.TowneeById.TryGetValue(rid, out var tn))
                return tn.Pressure(drive).ToString("0.00", CultureInfo.InvariantCulture);
            return m.Value;
        });
    }
}

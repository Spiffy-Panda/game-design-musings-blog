using Godot;
using MorningQueue.Core;

namespace MorningQueue;

/// <summary>
/// The single GDScript &lt;-&gt; .NET seam for The Morning Queue. Godot types live ONLY in this
/// file; everything it delegates to (MorningQueue.Core) is Godot-free and unit-tested.
///
/// The boundary is deliberately coarse — JSON text in, JSON text / string[] out, once per
/// boot (Validate) and once per loaded day (PrepareShift). Never per-visitor, never
/// per-string. File I/O stays on the GDScript side (FileAccess can read res://; System.IO
/// cannot see a PCK), so the loader reads the JSON and hands the text across.
///
/// Registered as a [GlobalClass] so GDScript can `CoreBridge.new()` and call these
/// instance methods. NOTE: adding a C# global class means the global-class cache must be
/// regenerated once (open the editor, or `godot --headless --path . --import`) before the
/// GDScript autoload that references `CoreBridge` will resolve it.
/// </summary>
[GlobalClass]
public partial class CoreBridge : RefCounted
{
    /// <summary>
    /// Boot-time bank validation. `banksJson` is an object with keys `references`,
    /// `townees`, `adventurers`, `generation` (townees/adventurers being the inner
    /// id-&gt;record maps). Returns the human-readable problems, empty when the banks are sane.
    /// </summary>
    public string[] Validate(string banksJson)
    {
        try
        {
            var data = MorningQueueData.ParseBanks(banksJson);
            return Validator.ValidateBanks(data).ToArray();
        }
        catch (System.Exception e)
        {
            return new[] { "core Validate failed: " + e.Message };
        }
    }

    /// <summary>
    /// Per-day shift preparation: validates the shift and annotates each visitor's
    /// inspections.scale.verdict against its claimed order. `referencesJson` is the full
    /// references bank; `visitorsPayloadJson` is `{ "visitors": [...] }`. Returns a JSON
    /// string `{ "visitors": [...annotated...], "errors": [...] }`.
    /// </summary>
    public string PrepareShift(string referencesJson, string visitorsPayloadJson)
    {
        try
        {
            return Shift.PrepareJson(referencesJson, visitorsPayloadJson);
        }
        catch (System.Exception e)
        {
            return "{\"visitors\":[],\"errors\":[\"core PrepareShift failed: "
                   + e.Message.Replace("\"", "'") + "\"]}";
        }
    }

    /// <summary>
    /// Day &gt; 0 shift generation: composes the shift in Core (Composer, seed = day), then
    /// runs the same validate + derive pass as PrepareShift. `banksJson` is the SAME payload
    /// shape Validate takes — built from the Deck's LIVE banks, so the pay-dues floor beat's
    /// runtime mutations are what the next day's generation sees. `localeJson` is the raw
    /// data/locales/en.json text (compose-time humanizing). One call per generated day.
    /// Returns `{ "visitors": [...], "errors": [...] }`.
    /// </summary>
    public string GenerateShift(int day, string banksJson, string localeJson)
    {
        try
        {
            return Composer.GenerateJson(day, banksJson, localeJson);
        }
        catch (System.Exception e)
        {
            return "{\"visitors\":[],\"errors\":[\"core GenerateShift failed: "
                   + e.Message.Replace("\"", "'") + "\"]}";
        }
    }

    /// <summary>Smoke-test hook — proves the bridge is reachable from GDScript.</summary>
    public static string Ping() => "pong";
}

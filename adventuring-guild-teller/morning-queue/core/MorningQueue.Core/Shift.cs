using System.Text.Json;
using System.Text.Json.Nodes;

namespace MorningQueue.Core;

/// <summary>
/// Prepares one loaded shift for the desk: validates every visitor (required fields +
/// inspection readings) and runs the derive pass that annotates inspections.scale.verdict
/// against the visitor's claimed standing order.
///
/// The annotation is done on a mutable JSON DOM so every authored field — checks,
/// player_story, truth optionals, portrait, and any field the banks grow later — survives
/// untouched; only `verdict` is added under inspections.scale.
/// </summary>
public static class Shift
{
    public sealed record Result(JsonArray Visitors, List<string> Errors);

    /// <summary>
    /// The bridge entry point: references JSON + a `{ "visitors": [...] }` payload in,
    /// a `{ "visitors": [...annotated...], "errors": [...] }` JSON string out.
    /// </summary>
    public static string PrepareJson(string referencesJson, string visitorsPayloadJson)
    {
        var refs = References.Parse(referencesJson);
        return ResultToJson(Prepare(refs, visitorsPayloadJson));
    }

    /// <summary>Serialize a Result to the bridge's `{ "visitors": [...], "errors": [...] }` shape.</summary>
    public static string ResultToJson(Result result)
    {
        var errorsArray = new JsonArray();
        foreach (var e in result.Errors)
            errorsArray.Add(e);
        var outp = new JsonObject
        {
            ["visitors"] = result.Visitors,
            ["errors"] = errorsArray,
        };
        return outp.ToJsonString(Json.Options);
    }

    /// <summary>
    /// Validate and derive-annotate the shift. `visitorsPayloadJson` is the object
    /// `{ "visitors": [...] }` (the shape the GDScript loader hands across).
    /// </summary>
    public static Result Prepare(References refs, string visitorsPayloadJson)
    {
        // Typed pass for validation (nulls read as "missing" like the GDScript has-checks).
        var wrapper = JsonSerializer.Deserialize<VisitorsWrapper>(visitorsPayloadJson, Json.Options)
                      ?? new VisitorsWrapper();
        var errors = Validator.ValidateShift(wrapper.Visitors);

        // DOM pass for the derive annotation (fidelity-preserving).
        var root = JsonNode.Parse(visitorsPayloadJson) as JsonObject;
        var arr = root?["visitors"] as JsonArray ?? new JsonArray();

        // Detach children so they can be re-parented into the fresh result array.
        var visitors = new JsonArray();
        var detached = new List<JsonNode?>();
        foreach (var node in arr) detached.Add(node);
        arr.Clear();

        foreach (var node in detached)
        {
            if (node is JsonObject visit)
                AnnotateScaleVerdict(visit, refs);
            visitors.Add(node);
        }

        return new Result(visitors, errors);
    }

    private static void AnnotateScaleVerdict(JsonObject visit, References refs)
    {
        if (visit["inspections"] is not JsonObject insp || insp["scale"] is not JsonObject scale)
            return;

        var againstId = AsString(visit["claim"]?["asserts"]?["against"]);
        Posting? order = null;
        if (againstId is not null && refs.Postings.TryGetValue(againstId, out var p))
            order = p;

        double? amount = scale["amount"] is JsonValue av && av.TryGetValue<double>(out var d) ? d : null;
        var unit = AsString(scale["unit"]);

        scale["verdict"] = Deriver.DeriveScaleVerdict(order, amount, unit);
    }

    private static string? AsString(JsonNode? node)
        => node is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private sealed class VisitorsWrapper
    {
        public List<Visit> Visitors { get; set; } = new();
    }
}

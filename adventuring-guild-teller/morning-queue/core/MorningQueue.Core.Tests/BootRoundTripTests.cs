using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MorningQueue.Core.Tests;

/// <summary>
/// Regression tests for the Godot JSON round-trip that broke the real in-engine boot while all
/// unit tests (which feed the raw file text) passed.
///
/// DeckLoader.gd builds its Validate / PrepareShift payloads with GDScript's JSON.stringify.
/// GDScript reads every JSON number as a float and stringifies whole numbers with a trailing
/// ".0" (4 -> 4.0). Stock System.Text.Json then refuses to bind "4.0" to an `int` property and
/// throws "The JSON value could not be converted to System.Int32. Path: $.accept.max". These
/// tests reproduce that exact text via TestData.Godotify over the REAL data files, driving the
/// same Core entry points CoreBridge.Validate / CoreBridge.PrepareShift call.
/// </summary>
public class BootRoundTripTests
{
    // ---- the exact failure the coordinator saw, reproduced in isolation ----------

    [Fact]
    public void Godotified_StandingOrder_BindsFloatMaxToInt()
    {
        // {"accept":{"min":2.0,"max":4.0,"unit":"dram"}} — the shape that threw at $.accept.max.
        var posting = JsonSerializer.Deserialize<Posting>(
            "{\"type\":\"standing_order\",\"item\":\"moonwort\"," +
            "\"accept\":{\"min\":2.0,\"max\":4.0,\"unit\":\"dram\"}}", Json.Options)!;
        Assert.NotNull(posting.Accept);
        Assert.Equal(4, posting.Accept!.Max);
        Assert.Equal(2, posting.Accept.Min);
    }

    // ---- CoreBridge.Validate boot path over Godot-ified REAL banks ---------------

    [Fact]
    public void Validate_GodotifiedRealBanks_NoInt32ConversionError()
    {
        // Mirror DeckLoader._run_banks_validation: one object with references/townees/
        // adventurers/generation, then stringified by Godot (whole numbers -> "N.0").
        var payload = TestData.Godotify(TestData.BanksPayload());

        var data = MorningQueueData.ParseBanks(payload); // must not throw
        var errors = Validator.ValidateBanks(data);
        Assert.DoesNotContain(errors, e => e.Contains("Int32") || e.Contains("Validate failed"));
        Assert.True(errors.Count == 0, "banks errors: " + string.Join(" | ", errors));
    }

    // ---- CoreBridge.PrepareShift boot path over Godot-ified REAL shift -----------

    [Fact]
    public void PrepareShift_GodotifiedRealDay0_NoInt32ConversionError()
    {
        // Mirror DeckLoader._prepare_shift_core: references + { "visitors": [...] }, both
        // stringified by Godot.
        var refsJson = TestData.Godotify(TestData.References);
        using var vdoc = JsonDocument.Parse(TestData.Visitors);
        var visitorsArray = vdoc.RootElement.GetProperty("visitors").GetRawText();
        var payload = TestData.Godotify("{ \"visitors\": " + visitorsArray + " }");

        var outText = Shift.PrepareJson(refsJson, payload);
        var root = JsonNode.Parse(outText)!.AsObject();

        var errors = root["errors"]!.AsArray();
        Assert.DoesNotContain(errors, e => e!.GetValue<string>().Contains("Int32")
                                        || e.GetValue<string>().Contains("PrepareShift failed"));
        Assert.Empty(errors);
        Assert.NotEmpty(root["visitors"]!.AsArray());
    }

    // ---- rankup_thresholds: manual int table must survive the same round trip ----

    [Fact]
    public void ParseIntTable_GodotifiedThresholds_Preserved()
    {
        var refsJson = TestData.Godotify(TestData.References);
        var refs = References.Parse(refsJson);
        // The raw file's threshold count must survive; the old TryGetValue<int> silently
        // dropped "N.0" rows.
        var rawRefs = References.Parse(TestData.References);
        Assert.Equal(rawRefs.RankupThresholds.Count, refs.RankupThresholds.Count);
        foreach (var kv in rawRefs.RankupThresholds)
            Assert.Equal(kv.Value, refs.RankupThresholds[kv.Key]);
    }
}

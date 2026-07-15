using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MorningQueue.Core.Tests;

public class ShiftTests
{
    // ---- shift validation red fixtures (one per inspection family) ---------------

    [Fact]
    public void Visitor_MissingRequiredField_Fires()
    {
        var visits = Parse("[ { \"id\": \"v-x\", \"affiliation\": \"townee\", \"profession\": \"Courier\", " +
                           "\"task_type\": \"item_check\", \"claim\": {}, \"truth\": {}, " +
                           "\"inspections\": { \"glass\": { \"reading\": \"g\" }, \"scale\": { \"reading\": \"s\" } } } ]");
        Assert.Contains("visitor 'v-x' missing field 'name'", Validator.ValidateShift(visits));
    }

    [Fact]
    public void Visitor_MissingInspections_Fires()
    {
        var visits = Parse("[ " + MinimalVisitFields("v-x") + " } ]");
        Assert.Contains("visitor 'v-x' missing 'inspections' object", Validator.ValidateShift(visits));
    }

    [Fact]
    public void Visitor_MissingScaleTool_Fires()
    {
        var visits = Parse("[ " + MinimalVisitFields("v-x") +
                           ", \"inspections\": { \"glass\": { \"reading\": \"g\" } } } ]");
        Assert.Contains("visitor 'v-x' inspections missing 'scale'", Validator.ValidateShift(visits));
    }

    [Fact]
    public void Visitor_EmptyGlassReading_Fires()
    {
        var visits = Parse("[ " + MinimalVisitFields("v-x") +
                           ", \"inspections\": { \"glass\": { \"reading\": \"\" }, \"scale\": { \"reading\": \"s\" } } } ]");
        Assert.Contains("visitor 'v-x' glass reading is empty", Validator.ValidateShift(visits));
    }

    // ---- derive pass -------------------------------------------------------------

    [Theory]
    [InlineData(3, "dram", ScaleVerdict.Within)]   // apothecary accept 2-4 dram
    [InlineData(5, "dram", ScaleVerdict.Over)]
    [InlineData(1, "dram", ScaleVerdict.Under)]
    [InlineData(3, "sprig", ScaleVerdict.NoOrder)] // unit mismatch
    public void Derive_AcceptWindow(double amount, string unit, string expected)
    {
        var order = new Posting { Type = "standing_order", Accept = new Accept { Min = 2, Max = 4, Unit = "dram" } };
        Assert.Equal(expected, Deriver.DeriveScaleVerdict(order, amount, unit));
    }

    [Theory]
    [InlineData(2, "sprig", ScaleVerdict.Meets)]   // temple total 2 sprig
    [InlineData(1, "sprig", ScaleVerdict.Under)]
    [InlineData(3, "sprig", ScaleVerdict.Meets)]   // at-or-above needed
    public void Derive_TotalOrder(double amount, string unit, string expected)
    {
        var order = new Posting { Type = "standing_order", Total = new Total { Needed = 2, Unit = "sprig" } };
        Assert.Equal(expected, Deriver.DeriveScaleVerdict(order, amount, unit));
    }

    [Fact]
    public void Derive_NoOrder_WhenNullOrNoLimit()
    {
        Assert.Equal(ScaleVerdict.NoOrder, Deriver.DeriveScaleVerdict(null, 3, "dram"));
        var gate = new Posting { Type = "bounty", RankMin = "bronze" };
        Assert.Equal(ScaleVerdict.NoOrder, Deriver.DeriveScaleVerdict(gate, 3, "dram"));
        var order = new Posting { Accept = new Accept { Min = 2, Max = 4, Unit = "dram" } };
        Assert.Equal(ScaleVerdict.NoOrder, Deriver.DeriveScaleVerdict(order, null, "dram"));
    }

    // ---- PrepareJson over the REAL day-0 shift -----------------------------------

    [Fact]
    public void PrepareJson_RealDay0_AnnotatesVerdictsAndReportsNoErrors()
    {
        var payload = "{ \"visitors\": " + VisitorsArray(TestData.Visitors) + " }";
        var outText = Shift.PrepareJson(TestData.References, payload);

        var root = JsonNode.Parse(outText)!.AsObject();
        Assert.Empty(root["errors"]!.AsArray());

        var visitors = root["visitors"]!.AsArray();
        Assert.NotEmpty(visitors);
        foreach (var v in visitors)
        {
            var scale = v!["inspections"]?["scale"];
            Assert.NotNull(scale);
            var verdict = scale!["verdict"]!.GetValue<string>();
            Assert.Contains(verdict, new[] {
                ScaleVerdict.Within, ScaleVerdict.Over, ScaleVerdict.Under,
                ScaleVerdict.Meets, ScaleVerdict.NoOrder });
        }
    }

    [Fact]
    public void PrepareJson_ItemCheckVerdict_MatchesClaimedOrder()
    {
        // wren-sixpence delivers 3 drams of moonwort against apothecary-standing-order (2-4).
        var payload = "{ \"visitors\": " + VisitorsArray(TestData.Visitors) + " }";
        var root = JsonNode.Parse(Shift.PrepareJson(TestData.References, payload))!.AsObject();
        var wren = root["visitors"]!.AsArray()
            .First(v => v!["id"]!.GetValue<string>() == "wren-sixpence")!;
        Assert.Equal(ScaleVerdict.Within, wren["inspections"]!["scale"]!["verdict"]!.GetValue<string>());
    }

    [Fact]
    public void PrepareJson_PreservesAuthoredFields()
    {
        var payload = "{ \"visitors\": " + VisitorsArray(TestData.Visitors) + " }";
        var root = JsonNode.Parse(Shift.PrepareJson(TestData.References, payload))!.AsObject();
        var wren = root["visitors"]!.AsArray()
            .First(v => v!["id"]!.GetValue<string>() == "wren-sixpence")!;
        // player_story / checks / notes survive the derive round-trip untouched.
        Assert.False(string.IsNullOrEmpty(wren["player_story"]?.GetValue<string>()));
        Assert.NotEmpty(wren["checks"]!.AsArray());
    }

    // ---- round trip on visitors.json ---------------------------------------------

    [Fact]
    public void Visitors_RoundTrip_IsStable()
    {
        var first = JsonSerializer.Deserialize<Wrapper>(TestData.Visitors, Json.Options)!;
        var reserialized = JsonSerializer.Serialize(first, Json.Options);
        var second = JsonSerializer.Deserialize<Wrapper>(reserialized, Json.Options)!;

        Assert.Equal(first.Visitors.Count, second.Visitors.Count);
        for (int i = 0; i < first.Visitors.Count; i++)
        {
            Assert.Equal(first.Visitors[i].Id, second.Visitors[i].Id);
            Assert.Equal(first.Visitors[i].TaskType, second.Visitors[i].TaskType);
            Assert.Equal(first.Visitors[i].Truth?.Stamp, second.Visitors[i].Truth?.Stamp);
            Assert.Equal(first.Visitors[i].Truth?.Binary, second.Visitors[i].Truth?.Binary);
        }
    }

    // ---- helpers -----------------------------------------------------------------

    private static List<Visit> Parse(string arrayJson)
        => JsonSerializer.Deserialize<List<Visit>>(arrayJson, Json.Options)!;

    private static string MinimalVisitFields(string id)
        => $"{{ \"id\": \"{id}\", \"name\": \"N\", \"affiliation\": \"townee\", " +
           "\"profession\": \"Courier\", \"task_type\": \"item_check\", \"claim\": {}, \"truth\": {}";

    private static string VisitorsArray(string visitorsJson)
    {
        using var doc = JsonDocument.Parse(visitorsJson);
        return doc.RootElement.GetProperty("visitors").GetRawText();
    }

    private sealed class Wrapper
    {
        public List<Visit> Visitors { get; set; } = new();
    }
}

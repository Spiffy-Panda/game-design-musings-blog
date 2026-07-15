using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MorningQueue.Core.Tests;

/// <summary>
/// Tests for the Core shift composer (the port of the retired GDScript ShiftGenerator).
///
/// GOLDEN WEEK: days 1-7 over the REAL banks are pinned as fixtures in Fixtures/. Any diff
/// is a real regression — the rebaseline was ruled once at MQT.D2a and spent by this port.
/// To intentionally rebaseline (a deliberate design change ONLY), run once with the env var
/// MQ_REBASELINE=1, review the fixture diffs, and commit them.
///
/// The generation entry is exercised the same two ways the boot round-trip tests exercise
/// Validate/PrepareShift: raw file text AND the Godot-stringified form DeckLoader actually
/// sends (whole numbers as "4.0"), asserting both produce byte-identical output.
/// </summary>
public class GeneratorTests
{
    private static readonly string FixtureDir = LocateFixtureDir();

    private static string LocateFixtureDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MorningQueue.Core.Tests.csproj")))
                return Path.Combine(dir.FullName, "Fixtures");
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate MorningQueue.Core.Tests.csproj above " + AppContext.BaseDirectory);
    }

    private static string GenerateDay(int day, string banksPayload)
        => Canonical(Composer.GenerateJson(day, banksPayload, TestData.LocaleEn));

    /// <summary>Pretty-print for reviewable fixtures; normalizes line endings.</summary>
    private static string Canonical(string json)
    {
        var node = JsonNode.Parse(json)!;
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
            .Replace("\r\n", "\n");
    }

    private static JsonArray VisitorsOf(string generatedJson)
        => JsonNode.Parse(generatedJson)!.AsObject()["visitors"]!.AsArray();

    private static JsonArray ErrorsOf(string generatedJson)
        => JsonNode.Parse(generatedJson)!.AsObject()["errors"]!.AsArray();

    // ---- golden week --------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void GoldenWeek_DayMatchesPinnedFixture(int day)
    {
        var actual = GenerateDay(day, TestData.BanksPayload());
        var path = Path.Combine(FixtureDir, $"golden_day{day}.json");

        if (Environment.GetEnvironmentVariable("MQ_REBASELINE") == "1")
        {
            Directory.CreateDirectory(FixtureDir);
            File.WriteAllText(path, actual);
            return;
        }

        Assert.True(File.Exists(path),
            $"Missing golden fixture {path} — run once with MQ_REBASELINE=1 to create it.");
        var expected = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.Equal(expected, actual);
    }

    // ---- determinism across the Godot JSON round trip -------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void Generate_GodotifiedBanks_ProducesIdenticalShift(int day)
    {
        var raw = GenerateDay(day, TestData.BanksPayload());
        var godot = GenerateDay(day, TestData.Godotify(TestData.BanksPayload()));
        Assert.Equal(raw, godot);
    }

    [Fact]
    public void Generate_SameDayTwice_IsDeterministic()
    {
        Assert.Equal(GenerateDay(3, TestData.BanksPayload()), GenerateDay(3, TestData.BanksPayload()));
    }

    // ---- selfcheck equivalent: the whole week is schema-valid, zero problems ---------

    [Fact]
    public void Week_AllSevenDays_ValidateWithZeroProblems()
    {
        int totalVisits = 0;
        for (int day = 1; day <= 7; day++)
        {
            var outp = GenerateDay(day, TestData.BanksPayload());
            var errors = ErrorsOf(outp);
            Assert.True(errors.Count == 0,
                $"day {day} errors: " + string.Join(" | ", errors.Select(e => e!.GetValue<string>())));
            var visits = VisitorsOf(outp);
            Assert.InRange(visits.Count, 12, 16);
            totalVisits += visits.Count;
        }
        Assert.True(totalVisits > 0);
    }

    // ---- distribution sanity across the week -----------------------------------------

    private static List<JsonObject> WeekVisits()
    {
        var all = new List<JsonObject>();
        for (int day = 1; day <= 7; day++)
            foreach (var v in VisitorsOf(GenerateDay(day, TestData.BanksPayload())))
                all.Add(v!.AsObject());
        return all;
    }

    [Fact]
    public void Week_EveryTaskTypeAppears()
    {
        var seen = WeekVisits().Select(v => v["task_type"]!.GetValue<string>()).ToHashSet();
        foreach (var t in Validator.TaskTypes)
            Assert.Contains(t, seen);
    }

    [Fact]
    public void Week_EveryReachableFailureAxisAppears()
    {
        // The axes the banks' material makes reachable (all twelve: every task admits its
        // recipe axes and the triggering material — owing actors, confusables, requires,
        // dup/short logbooks, out-of-season and too-deep drops — exists in the banks).
        var reachable = new[]
        {
            "identity", "amount", "paperwork", "rank", "unverifiable", "dues",
            "fieldability", "claimant", "authenticity", "duplicate", "season", "reach",
        };
        var seen = WeekVisits()
            .Select(v => v["truth"]!.AsObject()["failure"])
            .Where(f => f is JsonObject)
            .Select(f => f!.AsObject()["axis"]!.GetValue<string>())
            .ToHashSet();
        foreach (var axis in reachable)
            Assert.Contains(axis, seen);
    }

    [Fact]
    public void Week_ZeroEmptyFallbackVisits()
    {
        var banks = MorningQueueData.ParseBanks(TestData.BanksPayload());
        var loc = Humanizer.FromLocaleJson(TestData.LocaleEn);
        for (int day = 1; day <= 7; day++)
        {
            var result = Composer.Compose(day, banks, loc);
            Assert.True(result.FallbackCount == 0,
                $"day {day} used the clean-item_check fallback {result.FallbackCount} time(s)");
        }
    }

    [Fact]
    public void Week_NoNegativeScaleAmounts()
    {
        foreach (var v in WeekVisits())
        {
            var amount = v["inspections"]!.AsObject()["scale"]!.AsObject()["amount"];
            if (amount is JsonValue val && val.TryGetValue<double>(out var a))
                Assert.True(a >= 0, $"visit {v["id"]} emitted a negative amount {a}");
        }
    }

    [Fact]
    public void Week_EveryVisitCarriesScaleVerdict()
    {
        foreach (var v in WeekVisits())
        {
            var verdict = v["inspections"]!.AsObject()["scale"]!.AsObject()["verdict"];
            Assert.NotNull(verdict);
            Assert.Contains(verdict!.GetValue<string>(),
                new[] { "within", "over", "under", "meets", "no_order" });
        }
    }

    [Fact]
    public void Week_DuesFails_DeepLinkADirectoryRow()
    {
        // ReferencePanel deep-links dues fails via a consult:"…_directory" check entry.
        var duesFails = WeekVisits().Where(v =>
            v["truth"]!.AsObject()["failure"] is JsonObject f
            && f["axis"]!.GetValue<string>() == "dues").ToList();
        Assert.NotEmpty(duesFails);
        foreach (var v in duesFails)
        {
            var consults = v["checks"]!.AsArray()
                .Select(c => c!.AsObject()["consult"]!.GetValue<string>());
            Assert.Contains(consults, c => c.EndsWith("_directory"));
        }
    }

    // ---- the live-dues trap: generation must read the dues state it is HANDED ---------

    [Fact]
    public void Generate_AllDuesPaid_EmitsNoDuesFails()
    {
        // Simulate the pay-dues floor beat: flip every townee and adventurer to current in
        // the payload (the Deck passes its LIVE mutated dicts, not the bank files).
        var root = JsonNode.Parse(TestData.BanksPayload())!.AsObject();
        foreach (var key in new[] { "townees", "adventurers" })
            foreach (var kv in root[key]!.AsObject())
                if (kv.Value is JsonObject rec && rec.ContainsKey("dues"))
                    rec["dues"] = "current";

        for (int day = 1; day <= 7; day++)
        {
            var outp = Composer.GenerateJson(day, root.ToJsonString(), TestData.LocaleEn);
            foreach (var v in VisitorsOf(outp))
            {
                var failure = v!.AsObject()["truth"]!.AsObject()["failure"];
                if (failure is JsonObject f)
                    Assert.NotEqual("dues", f["axis"]!.GetValue<string>());
            }
        }
    }
}

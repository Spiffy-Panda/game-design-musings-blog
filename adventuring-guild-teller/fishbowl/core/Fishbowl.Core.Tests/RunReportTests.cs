using System.Text.Json.Nodes;
using Fishbowl.Core.Api;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Json;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// <c>--report</c>, the tool's only machine-readable output.
///
/// <para><b>Why these exist.</b> The flag threw an unhandled exception and wrote no file for its
/// entire life. Nothing caught it because the report writer was a local function inside
/// <c>Program.cs</c>'s top-level statements — no test could reach it, and the only way to find the
/// bug was to type the flag. Meanwhile four verification agents each hand-rolled a regex scraper
/// over the human-readable <c>--chronicle</c> output and disagreed with each other about basic
/// counts; one decoded the UTF-8 bullet as cp1252, counted zero bullets, and reported "0" rather
/// than erroring. The report now lives in <see cref="RunReport"/> in the core so that a test can
/// hold it, which is the only reason this file can exist at all.</para>
/// </summary>
public class RunReportTests
{
    /// <summary>
    /// <b>The regression pin — and it is deliberately not a serialization test.</b>
    ///
    /// <para>The bug: <see cref="DataJson.Pretty"/> carried no <c>TypeInfoResolver</c>, which
    /// <c>JsonNode.ToJsonString(options)</c> requires (it calls the parameterless
    /// <c>MakeReadOnly()</c>, the overload that refuses to attach a default) and
    /// <c>JsonSerializer</c> does not (it attaches one and mutates the object). So <c>--report</c>
    /// threw and wrote nothing, for the whole life of the flag.</para>
    ///
    /// <para><b>Why this asserts a field instead of serializing something.</b> These options are
    /// static singletons and <c>JsonSerializer</c> repairs them in place on first use. In this test
    /// process <c>M2_PressuresSnapshotTests</c> reaches <c>Snapshot.Save</c> →
    /// <c>DataJson.Serialize(pretty: true)</c> → <c>JsonSerializer.Serialize(value, Pretty)</c>,
    /// which heals <c>Pretty</c> for every test that runs afterwards. A test that round-trips a node
    /// through <c>Pretty</c> therefore <b>passes against the broken code</b> whenever a sibling got
    /// there first — verified on 2026-07-16, in both directions: the old report tree throws when run
    /// under <c>--filter</c> and passes in the full suite. That is precisely how a bug this total
    /// survived a 34-test suite.</para>
    ///
    /// <para><b>So the assertion is against a FRESH options object</b>
    /// (<see cref="DataJson.NewOptions"/>), which no other test can have touched. Asserting against
    /// the singleton does not work either — and not just for the round-trip: the healing literally
    /// assigns <c>TypeInfoResolver</c>, so even <c>Assert.NotNull(DataJson.Pretty.TypeInfoResolver)</c>
    /// passes against the broken code once a sibling has run. Confirmed by deleting the fix and
    /// watching all 42 tests stay green. Nothing about the shared statics is pinnable in-process;
    /// only what <c>Build()</c> produces is.</para>
    /// </summary>
    [Fact]
    public void Freshly_Built_Options_Carry_A_TypeInfoResolver_And_Can_Write_A_Bare_Clr_Value()
    {
        // Fresh, therefore un-healable: this is what Build() really produces.
        Assert.NotNull(DataJson.NewOptions(indented: true).TypeInfoResolver);
        Assert.NotNull(DataJson.NewOptions().TypeInfoResolver);

        var summary = new JsonArray();
        summary.Add("a rendered line");     // Add<string>: builds a JsonValueCustomized, which must
                                            // consult the resolver — unlike every implicit (JsonNode)
                                            // conversion, which carries its own converter. This one
                                            // line, as summary.Add(line.Text), is what killed --report.
        var root = new JsonObject { ["day"] = 1, ["summary"] = summary };

        // Threw InvalidOperationException before the fix. Against a fresh object, every run.
        string json = root.ToJsonString(DataJson.NewOptions(indented: true));

        Assert.Equal("a rendered line", JsonNode.Parse(json)!["summary"]![0]!.GetValue<string>());
    }

    /// <summary>
    /// The report is ASCII-escaped so a scraper cannot silently mojibake it. This box's Python
    /// defaults <c>open()</c> to cp1252; a UTF-8 <c>•</c> read that way corrupts rather than raises,
    /// and one agent's scraper reported "0 bullets" instead of erroring.
    /// <para>Fresh options, per this file's whole thesis — the singleton is healed in place, so an
    /// assertion against it is an assertion about test ordering.</para>
    /// </summary>
    [Fact]
    public void Report_Options_Ascii_Escape_While_Authored_Data_Keeps_Its_Prose()
    {
        const string prose = "Dob Millet weighed it again — twice • as slow";

        string report = new JsonObject { ["text"] = prose }.ToJsonString(DataJson.NewReportOptions());
        Assert.DoesNotContain("—", report);            // nothing outside ASCII survives in the bytes
        Assert.DoesNotContain("•", report);
        Assert.Contains("\\u2014", report);
        Assert.Equal(prose, JsonNode.Parse(report)!["text"]!.GetValue<string>());   // ...yet round-trips

        // The authored-data convention is deliberately NOT changed: data/ is prose a human edits.
        string authored = new JsonObject { ["text"] = prose }.ToJsonString(DataJson.NewOptions(indented: true));
        Assert.Contains("—", authored);
    }

    [Fact]
    public void Report_Is_Written_Parseable_And_Declares_Its_Schema()
    {
        var root = Report(days: 3);
        Assert.Equal(RunReport.Schema, root["schema"]!.GetValue<string>());
        Assert.Equal(3, root["days_requested"]!.GetValue<int>());
        Assert.Equal(3, root["days_run"]!.GetValue<int>());
        Assert.Equal(3, root["days"]!.AsArray().Count);
    }

    /// <summary>The report's day-hash is the simulation's day-hash, not a second opinion about it.
    /// This is the field a verifier trusts to say "the run I am reading is the run you claim".</summary>
    [Fact]
    public void Report_Day_Hashes_Are_The_Simulations_Own()
    {
        var sim = Run(3);
        var root = Parse(RunReport.ToJson(sim.World, 3));
        foreach (var day in root["days"]!.AsArray())
            Assert.Equal(sim.World.DayHashes[day!["day"]!.GetValue<int>()], day["hash"]!.GetValue<string>());
    }

    /// <summary>The rendered summary text arrives whole, as a field. This is the thing the scrapers
    /// were prying out from behind a "    • " prefix, and the reason one of them shipped a false
    /// zero — so the field is worth pinning rather than trusting.</summary>
    [Fact]
    public void Report_Summary_Carries_The_Rendered_Text_Verbatim()
    {
        var sim = Run(2);
        var root = Parse(RunReport.ToJson(sim.World, 2));

        foreach (var day in root["days"]!.AsArray())
        {
            var expected = Summarizer.Render(sim.World, day!["day"]!.GetValue<int>()).Select(l => l.Text).ToList();
            var actual = day["summary"]!.AsArray().Select(l => l!["text"]!.GetValue<string>()).ToList();
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Count, day["summary_count"]!.GetValue<int>());
        }
        Assert.All(root["days"]!.AsArray(), d => Assert.NotEmpty(d!["summary"]!.AsArray()));
    }

    /// <summary>Every count in the report agrees with every other count in the report. The whole
    /// point of the file is that four readers get the same number from it; internal disagreement
    /// would just relocate the original defect.</summary>
    [Fact]
    public void Report_Totals_And_Per_Rule_Counts_Reconcile()
    {
        var sim = Run(3);
        var root = Parse(RunReport.ToJson(sim.World, 3));

        var days = root["days"]!.AsArray();
        int eventsFromDays = days.Sum(d => d!["events"]!.AsArray().Count);
        int eventsFromRules = root["rules"]!.AsArray().Sum(r => r!["events"]!.GetValue<int>());

        Assert.Equal(eventsFromDays, root["totals"]!["events"]!.GetValue<int>());
        Assert.Equal(eventsFromDays, eventsFromRules);
        Assert.All(days, d => Assert.Equal(d!["events"]!.AsArray().Count, d["event_count"]!.GetValue<int>()));
        Assert.Equal(days.Sum(d => d!["summary"]!.AsArray().Count), root["totals"]!["summary_lines"]!.GetValue<int>());
    }

    /// <summary>Every rule in the bank gets a row, including the ones that never fired — the zero
    /// rows are the finding. A report that listed only what fired makes "did X fire?" unanswerable
    /// without already knowing the bank by heart, which is exactly the question a verifier has.
    /// <para>One day, on purpose: the golden bank is 12 rules and all 12 fire within three days, so
    /// a 3-day run would assert the zero rows vacuously and pass whether or not they were emitted.</para></summary>
    [Fact]
    public void Report_Lists_Every_Rule_In_The_Bank_Including_The_Zeroes()
    {
        var sim = Run(1);
        var root = Parse(RunReport.ToJson(sim.World, 1));
        var rows = root["rules"]!.AsArray().ToDictionary(r => r!["storylet"]!.GetValue<string>(), r => r!);
        var days = root["days"]!.AsArray();

        foreach (var s in sim.World.Town.Storylets)
        {
            Assert.True(rows.ContainsKey(s.Id), $"bank rule '{s.Id}' has no row in the report");
            // The row's count is exactly what the days array says — 0 included, not omitted.
            int fromDays = days.Sum(d => d!["events"]!.AsArray()
                .Count(e => e!["storylet"]!.GetValue<string>() == s.Id));
            Assert.Equal(fromDays, rows[s.Id]["events"]!.GetValue<int>());
        }
        Assert.Equal(sim.World.Town.Storylets.Count, root["totals"]!["bank"]!.GetValue<int>());
        Assert.Contains(rows.Values, r => r["events"]!.GetValue<int>() == 0);   // non-vacuous at 1 day
    }

    /// <summary>Same seed, same bytes. A report you cannot diff is a report you have to read.</summary>
    [Fact]
    public void Report_Is_Byte_Identical_Across_Runs()
        => Assert.Equal(RunReport.ToJson(Run(3).World, 3), RunReport.ToJson(Run(3).World, 3));

    // --- helpers ---

    private static Simulation Run(int days)
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        sim.RunDays(days);
        return sim;
    }

    private static JsonObject Report(int days) => Parse(RunReport.ToJson(Run(days).World, days));

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}

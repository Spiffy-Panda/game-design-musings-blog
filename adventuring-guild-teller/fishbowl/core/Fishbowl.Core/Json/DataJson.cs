using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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

    /// <summary>
    /// Options for <b>machine output</b> (<c>--report</c>) — this project's conventions, but
    /// <b>ASCII-escaped</b>: every non-ASCII character is written as <c>\uXXXX</c>.
    ///
    /// <para><b>Why the report escapes and authored data does not.</b> They have different jobs and
    /// different readers. `data/` is prose a human edits, so relaxed escaping is right there — an
    /// em dash should look like an em dash in the file (ruled, and unchanged). The report is read by
    /// scrapers, and on this box Python's <c>open()</c> defaults to cp1252: a UTF-8 em dash or `•`
    /// silently mojibakes rather than raising, and one verification agent's scraper reported "0
    /// bullets" instead of failing. <c>—</c> is pure ASCII, so it survives being read under the
    /// wrong codec and parses back to the identical string under any of them.</para>
    ///
    /// <para>Escaping is a transport detail, not a content change: <c>JsonNode.Parse</c> returns the
    /// same string either way. The report's bytes get smaller-alphabet, not smaller.</para>
    /// </summary>
    public static readonly JsonSerializerOptions ReportPretty = BuildReport();

    /// <summary>A <b>fresh</b> <see cref="ReportPretty"/>. Same reason as <see cref="NewOptions"/>:
    /// the singleton is healed in place by the first caller, so it can only be pinned fresh.</summary>
    public static JsonSerializerOptions NewReportOptions() => BuildReport();

    private static JsonSerializerOptions BuildReport()
    {
        var o = Build(indented: true);
        // Default (not Unsafe*): escapes non-ASCII. Set after Build so the report inherits every
        // other convention — and so this override is the single visible difference between the two.
        o.Encoder = JavaScriptEncoder.Default;
        return o;
    }

    /// <summary>
    /// A <b>fresh</b> options object carrying this project's conventions.
    ///
    /// <para>It exists because the two singletons above are mutable until first use and are then
    /// frozen in whatever state the first caller left them — which makes them untestable in-process:
    /// any assertion about <see cref="Pretty"/> is really an assertion about whichever test ran
    /// first. A fresh object is the only way to state what <see cref="Build"/> actually produces,
    /// and it is what <c>RunReportTests</c> pins.</para>
    ///
    /// <para>Also the right thing to call if you ever need these conventions with one setting
    /// varied: mutate a fresh copy, never the shared statics.</para>
    /// </summary>
    public static JsonSerializerOptions NewOptions(bool indented = false) => Build(indented);

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
        Converters = { new TolerantIntConverter(), new TolerantInt64Converter(), new RateModConverter() },

        // REQUIRED — and the reason is worth the paragraph, because the defect it caused was
        // invisible to the test suite BY CONSTRUCTION.
        //
        // Without an explicit resolver these options are half-built, and STJ repairs them lazily —
        // but *which* API touches them first decides whether they work forever or throw forever:
        //   · `JsonSerializer.Serialize`/`Deserialize` call `MakeReadOnly(populateMissingResolver:
        //     true)`, which attaches the default reflection resolver and MUTATES this object.
        //   · `JsonNode.ToJsonString(options)` / `WriteTo(writer, options)` call the *parameterless*
        //     `MakeReadOnly()`, which refuses to attach one and throws instead.
        // These are static singletons, so the first caller in a process fixes the outcome for every
        // caller after it. That is the whole bug:
        //   · In the CLI process nothing ever hands `Pretty` to JsonSerializer, so it stayed
        //     resolver-less and `--report` threw at `root.ToJsonString(Pretty)` and wrote no file —
        //     for the entire life of the flag.
        //   · In the TEST process `M2_PressuresSnapshotTests` reaches `Snapshot.Save` ->
        //     `DataJson.Serialize(pretty: true)` -> `JsonSerializer.Serialize(value, Pretty)`, which
        //     HEALS `Pretty` for every test that runs after it. So a regression test for this bug
        //     passes against the broken code whenever a sibling test got there first.
        // Verified both directions 2026-07-16: the old report tree throws under `--filter` (run
        // alone) and passes in the full suite. Do NOT try to prove this with a serialization test;
        // assert the resolver is present (RunReportTests does) — that is a fact about construction
        // and no neighbour can heal it.
        //
        // Which nodes need a resolver at all is the last piece: an implicit `(JsonNode)"s"` or
        // `obj["k"] = "s"` builds a JsonValuePrimitive carrying its own converter and never asks,
        // while `JsonArray.Add<T>(T)` builds a JsonValueCustomized that must consult one. A single
        // `summary.Add(line.Text)` was the difference between `--report` (dead) and `--lint --report`
        // (fine) — the two write the same shape, and only one of them went through Add<T>.
        //
        // Behaviour-neutral: it names the resolver STJ was already attaching lazily. It cannot reach
        // the day-hash — CanonicalJson does its own walk and never touches JsonSerializerOptions.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
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

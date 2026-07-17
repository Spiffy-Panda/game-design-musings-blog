using System.Text.Json.Nodes;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Json;
using Fishbowl.Core.Text;

namespace Fishbowl.Core.Api;

/// <summary>
/// <c>--report</c> — the machine-readable projection of a finished run. Sibling to
/// <see cref="WorldView"/> (which projects the <i>live</i> world for the observatory); this one
/// projects a <i>run</i>, for a verifier.
///
/// <para><b>Why it lives in Core and not in the CLI.</b> So it can be tested. The flag threw an
/// unhandled <c>InvalidOperationException</c> and wrote no file for its entire life, and nothing
/// noticed, because the report writer was a local function inside <c>Program.cs</c>'s top-level
/// statements — unreachable from the test project, and therefore exercised by nobody but a human
/// who happened to pass <c>--report</c>. The bug was never subtle; it was just unreachable.</para>
///
/// <para><b>What it is for, stated plainly, because the cost of not having it was real.</b> This is
/// the only machine-readable output the tool has. While it was dead, four verification agents each
/// hand-rolled a regex scraper over <c>--chronicle</c>'s human output and then disagreed with each
/// other about basic counts; one decoded the UTF-8 <c>•</c> bullet as cp1252, found no bullets, and
/// reported "0" rather than erroring. Every number those scrapers were reaching for is a field here.
/// If you find yourself about to regex this tool's stdout, add the field instead.</para>
///
/// <para><b>Shape contract.</b> <c>schema</c> is the version tag — bump it when a field changes
/// meaning, not when one is added. Everything is ordered deterministically (days ascending, events
/// by slot then storylet id, rule counts by storylet id ordinal), so two runs of the same seed
/// produce byte-identical files and a diff means something.</para>
/// </summary>
public static class RunReport
{
    /// <summary>Version tag emitted as <c>schema</c>. Additive fields do not bump it; a field that
    /// changes meaning does.</summary>
    public const string Schema = "fishbowl-report/1";

    /// <summary>The report as JSON text. Serialized here rather than at the call site so the
    /// serializer options are part of what the tests pin — the defect was in the options, not in
    /// the tree.
    /// <para>Uses <see cref="DataJson.ReportPretty"/>, not <see cref="DataJson.Pretty"/>: the report
    /// is machine output and is ASCII-escaped so a scraper reading it under the wrong codec cannot
    /// silently mojibake the prose. `data/` keeps relaxed escaping — see DataJson's remarks.</para>
    /// </summary>
    public static string ToJson(World w, int daysRequested) =>
        Build(w, daysRequested).ToJsonString(DataJson.ReportPretty);

    /// <summary>
    /// The report tree. Covers only <i>finalized</i> days: <see cref="World.DayHashes"/> is written by
    /// <c>Simulation.FinalizeDay</c>, so its keys are exactly the days that ran to dawn, and every day
    /// here is therefore in the past — which is what makes <see cref="Summarizer.Render"/> below a
    /// pure read of a frozen gate rather than a re-seal of live occupancy.
    /// </summary>
    public static JsonObject Build(World w, int daysRequested)
    {
        var days = w.DayHashes.Keys.OrderBy(d => d).ToList();

        // Per-rule tallies, accumulated from the very same lists the days array is built from. Not
        // recomputed independently: two places counting the same thing is how they come to disagree,
        // which is the entire reason this file exists.
        var eventsPerRule = new Dictionary<string, int>(StringComparer.Ordinal);
        var summarizedPerRule = new Dictionary<string, int>(StringComparer.Ordinal);
        var daysPerRule = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        var distinctText = new HashSet<string>(StringComparer.Ordinal);
        int totalEvents = 0, totalSummaryLines = 0;
        var daysNode = new JsonArray();

        foreach (int day in days)
        {
            var dayEvents = w.Chronicle.Where(e => e.Day == day)
                             .OrderBy(e => e.Slot)
                             .ThenBy(e => e.StoryletId, StringComparer.Ordinal).ToList();
            var summary = Summarizer.Render(w, day);

            // (slot, storylet) is SummaryLine's own documented "link back to its chronicle entry".
            var delivered = summary.Select(l => (l.Slot, l.StoryletId)).ToHashSet();

            var eventsNode = new JsonArray();
            foreach (var e in dayEvents)
            {
                eventsNode.Add(new JsonObject
                {
                    ["slot"] = e.Slot,
                    ["storylet"] = e.StoryletId,
                    ["kind"] = e.Kind,
                    ["place"] = e.PlaceId,
                    ["place_name"] = e.PlaceName,
                    ["participants"] = Strings(e.Participants),
                    ["tellability"] = Math.Round(e.Tellability, 4),
                    // The two reasons a beat that happened is not in `summary` below, both stated
                    // rather than left to be re-derived: the hearsay gate dropped it, or it was
                    // outranked. A reader asking "why is this not in the summary?" reads these.
                    ["carried_by_gossip"] = e.CarriedByGossip,
                    ["in_summary"] = delivered.Contains((e.Slot, e.StoryletId)),
                    ["posting_ids"] = Strings(e.PostingIds),
                });

                eventsPerRule[e.StoryletId] = eventsPerRule.GetValueOrDefault(e.StoryletId) + 1;
                if (!daysPerRule.TryGetValue(e.StoryletId, out var dl)) daysPerRule[e.StoryletId] = dl = new List<int>();
                if (dl.Count == 0 || dl[^1] != day) dl.Add(day);
            }

            var summaryNode = new JsonArray();
            foreach (var l in summary)
            {
                summaryNode.Add(new JsonObject
                {
                    ["slot"] = l.Slot,
                    ["storylet"] = l.StoryletId,
                    ["place_name"] = l.PlaceName,
                    ["participants"] = Strings(l.Participants),
                    ["tellability"] = Math.Round(l.Tellability, 4),
                    // The rendered text, verbatim — the thing the scrapers were pulling out from
                    // behind a "    • " prefix.
                    ["text"] = l.Text,
                });
                summarizedPerRule[l.StoryletId] = summarizedPerRule.GetValueOrDefault(l.StoryletId) + 1;
                distinctText.Add(l.Text);
            }

            totalEvents += dayEvents.Count;
            totalSummaryLines += summary.Count;

            daysNode.Add(new JsonObject
            {
                ["day"] = day,
                ["hash"] = w.DayHashes[day],
                ["event_count"] = dayEvents.Count,
                ["summary_count"] = summary.Count,
                ["events"] = eventsNode,
                ["summary"] = summaryNode,
            });
        }

        return new JsonObject
        {
            ["schema"] = Schema,
            ["seed"] = w.Seed,
            ["slots_per_day"] = w.SlotsPerDay,
            ["days_requested"] = daysRequested,
            // Not the same number as days_requested if the run stopped early. Both are emitted so a
            // short run is visible instead of being inferred from an array length.
            ["days_run"] = days.Count,
            ["config"] = ConfigNode(w),
            ["totals"] = new JsonObject
            {
                ["events"] = totalEvents,
                ["summary_lines"] = totalSummaryLines,
                ["distinct_summary_lines"] = distinctText.Count,
                ["bank"] = w.Town.Storylets.Count,
                // No `rules_fired` here on purpose: `variety.rules_fired` is that number, and this
                // one would have counted bank rules only while that one counts every id that
                // produced a beat (the board's synthetic `posting-expired` included). Two fields of
                // the same name disagreeing by one, in one document, is the exact defect this file
                // was written to end. One name, one place — `variety` below, and `rules[]` for the
                // per-rule detail.
            },
            // "How much does this town actually say?" — the across-night measure. VFB.Q1 lives in
            // here too, labelled, because it is saturated at its ceiling and the comparison is the
            // point. See Variety's remarks.
            ["variety"] = Variety.Measure(w).ToJson(),
            ["rules"] = RulesNode(w, eventsPerRule, summarizedPerRule, daysPerRule),
            ["days"] = daysNode,
        };
    }

    private static JsonObject ConfigNode(World w)
    {
        var c = w.Config;
        var rates = new JsonObject();
        foreach (var d in Model.Town.Drives) rates[d] = c.PressureRates.TryGetValue(d, out var r) ? r : 1.0;
        return new JsonObject
        {
            ["storylet_rate"] = c.StoryletRate,
            ["storylet_cooldown_scale"] = c.StoryletCooldownScale,
            ["hearsay_required"] = c.HearsayRequired,
            ["actionability"] = c.Actionability,
            // Which of the three registers `summary[].text` was picked from. The dial alone does not
            // say — Actionability.Of snaps it to a register, and a reader should not have to know the
            // thresholds to know what they are looking at.
            ["register"] = Actionability.Of(c.Actionability).ToString().ToLowerInvariant(),
            ["summary_lines"] = c.SummaryLines,
            ["novelty_decay"] = c.NoveltyDecay,
            // Emitted beside the knob because the knob is meaningless without it: novelty_decay is
            // the per-telling multiplier, this is how many nights a telling stays in the ledger.
            ["novelty_window"] = Summarizer.NoveltyWindow,
            ["bio_marks_enabled"] = c.BioMarksEnabled,
            ["posting_rate"] = c.PostingRate,
            ["posting_expiry_scale"] = c.PostingExpiryScale,
            ["pressure_rates"] = rates,
        };
    }

    /// <summary>
    /// Per-rule fire counts — <b>one row for every rule in the bank, including the zeroes</b>. The
    /// zero rows are the point: a rule that never fired is the finding, and a report that lists only
    /// what fired makes "did X fire?" unanswerable without knowing the bank by heart. Rows whose id
    /// is not in the bank (<c>posting-expired</c>, which is a board mechanism rather than a rule) are
    /// appended and marked <c>in_bank: false</c> rather than dropped.
    /// </summary>
    private static JsonArray RulesNode(World w, Dictionary<string, int> events,
        Dictionary<string, int> summarized, Dictionary<string, List<int>> days)
    {
        var bank = w.Town.Storylets.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var ids = bank.Concat(events.Keys.Where(k => !bank.Contains(k)))
                      .OrderBy(x => x, StringComparer.Ordinal);

        var arr = new JsonArray();
        foreach (var id in ids)
            arr.Add(new JsonObject
            {
                ["storylet"] = id,
                ["in_bank"] = bank.Contains(id),
                ["events"] = events.GetValueOrDefault(id),
                ["summarized"] = summarized.GetValueOrDefault(id),
                ["days_fired"] = new JsonArray(days.GetValueOrDefault(id, new List<int>())
                                                   .Select(d => (JsonNode)d).ToArray()),
            });
        return arr;
    }

    private static JsonArray Strings(IEnumerable<string> values) =>
        new(values.Select(v => (JsonNode)v).ToArray());
}

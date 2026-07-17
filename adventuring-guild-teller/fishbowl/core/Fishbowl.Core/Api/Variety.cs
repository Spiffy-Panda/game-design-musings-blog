using System.Text.Json.Nodes;
using Fishbowl.Core.Engine;

namespace Fishbowl.Core.Api;

/// <summary>
/// How much does this town actually <i>say</i>? — measured across nights, over the lines a player
/// really reads.
///
/// <para><b>Why this exists: VFB.Q1 is pinned to its own ceiling.</b> VFB.Q1 is specified as "avg
/// distinct tellable lines/night" and the soak computes it as the distinct texts <i>within</i> one
/// night's summary. But the summary has already been truncated to <c>summary_lines</c> (5) by the
/// time it is counted, so the metric is <c>min(pool, 5)</c> averaged — it asks "is the pool ≥ 5?"
/// of a pool measured at ≥ 20 every night. It reads a flat <b>5.00</b> and cannot move. Ablation
/// settled it: delete 30 of the 46 rules and it still reports 5.00 with 0/42 nights below 4. It
/// cannot see two-thirds of the bank.</para>
///
/// <para><b>What it misses is the thing that matters.</b> VFB.Q1 never compares night N to night
/// N−1. Because <c>_binding</c> pins each rule's cast and <c>place</c> pins its room, a told rule
/// renders the <i>identical sentence</i> every time it fires — <c>road-stories</c> runs byte-identical
/// on 11 nights of 14. The town fires ~27.9 beats/night, ~70 of 391 beats over a fortnight reach the
/// player, and the player sees <b>16 distinct sentences</b> in that fortnight. VFB.Q1 reads 5.00
/// through all of it.</para>
///
/// <para><b>The headline is <see cref="DistinctTexts"/></b> — distinct rendered sentences over the
/// window. Keyed on the final text, never on the rule id: two fires of one rule with a different
/// cast or room are two different sentences, and that distinction is the entire point of the
/// measure.</para>
///
/// <para><b>Deliberately un-scored.</b> There is no PASS/FAIL here, and that is not an oversight.
/// This instrument exists to judge a novelty/fatigue change to <see cref="Summarizer"/>'s ordering
/// that is being written by someone else, and a threshold is a judgement that invites tuning
/// toward it. It reports counts and ratios and lets a human read them. The old VFB.Q1 figure keeps
/// its PASS/FAIL because that verdict is what is written down in the plan, and the comparison
/// between the two numbers is itself the finding.</para>
/// </summary>
public sealed record Variety
{
    /// <summary>Finalized days in the window.</summary>
    public required int Nights { get; init; }

    /// <summary>Lines the player actually read, summed over nights (post filter/order/take/pick).</summary>
    public required int DeliveredLines { get; init; }

    /// <summary><b>The headline.</b> Distinct rendered sentences over the whole window. "How many
    /// different things did this town say to me?"</summary>
    public required int DistinctTexts { get; init; }

    /// <summary><see cref="DistinctTexts"/> over <see cref="DeliveredLines"/> — the share of what the
    /// player read that they had not already read. 1.0 = never repeats itself; 0.1 = says the same
    /// ten sentences forever.
    /// <para><b>Window-dependent by nature</b> (a longer window has more chances to repeat), so it is
    /// comparable only between runs of the same length. Compare like for like.</para></summary>
    public double NoveltyRate => DeliveredLines == 0 ? 0 : (double)DistinctTexts / DeliveredLines;

    /// <summary>Delivered lines on night N whose text was also delivered on night N−1.</summary>
    public required int RepeatsOfPreviousNight { get; init; }

    /// <summary>Delivered lines on night N whose text was delivered on <i>any</i> earlier night.</summary>
    public required int RepeatsOfAnyPriorNight { get; init; }

    /// <summary>Lines delivered on nights that had a predecessor — the denominator for both repeat
    /// rates. Night 1 cannot repeat anything, so counting it would dilute the number for free.</summary>
    public required int LinesWithAPriorNight { get; init; }

    public double RepeatRatePreviousNight =>
        LinesWithAPriorNight == 0 ? 0 : (double)RepeatsOfPreviousNight / LinesWithAPriorNight;

    public double RepeatRateAnyPriorNight =>
        LinesWithAPriorNight == 0 ? 0 : (double)RepeatsOfAnyPriorNight / LinesWithAPriorNight;

    /// <summary>Chronicle entries in the window — everything the engine made happen.</summary>
    public required int BeatsFired { get; init; }

    /// <summary>Beats that reached a player (== <see cref="DeliveredLines"/>). Named separately
    /// because the ratio below is the question, and a ratio whose halves share a name reads as a
    /// tautology.</summary>
    public int BeatsTold => DeliveredLines;

    /// <summary>Share of what happened that anyone heard about. The rest is yield the town paid for
    /// and threw away.</summary>
    public double ToldPerFired => BeatsFired == 0 ? 0 : (double)BeatsTold / BeatsFired;

    /// <summary><b>Bank rules</b> that produced at least one beat — i.e. distinct ids in the chronicle
    /// that are actually in <c>Town.Storylets</c>.
    ///
    /// <para><b>It used to count every id in the chronicle, and that made it lie about a bigger
    /// number.</b> The board files its expiries under the synthetic id <c>posting-expired</c>
    /// (<c>Board.ExpiredStoryletId</c>), which is a board mechanism and has no entry in the bank —
    /// <c>--report</c>'s per-rule table already says so, in a field named <c>in_bank: false</c>. So this
    /// reported <b>51 against a 50-rule bank</b>, which is impossible on its face and is the kind of
    /// number a reader corrects for silently instead of filing. Worse, <see cref="RulesFiredButNeverTold"/>
    /// subtracts these two, so the error propagated into a headline: "5 rules the player can never know
    /// exist" was 51−46, an arithmetic over two different universes.</para>
    ///
    /// <para><b>Why <see cref="RulesTold"/> moved with it, and why that was not optional.</b> The
    /// subtraction is only meaningful if both sides range over the same set, and <c>posting-expired</c>
    /// <i>is told</i> — it reached a summary once in the live town's fortnight. Rescoping only the
    /// minuend would have produced 50−46=4 and looked like a fix, while actually comparing 50 bank rules
    /// against 45 bank rules plus one board mechanism. One universe, three counts: the bank.</para>
    ///
    /// <para><b>The board's beats are not being hidden</b> — they are just not rules.
    /// <see cref="BeatsFired"/>, <see cref="BeatsTold"/>, <see cref="ToldPerFired"/> and
    /// <see cref="DistinctTexts"/> all still count every beat and every sentence the town produces,
    /// expiries included, because those measures ask what the town <i>says</i>. These three ask what the
    /// <i>bank</i> does, and they are named <c>rules_*</c> for that reason.</para></summary>
    public required int RulesFired { get; init; }

    /// <summary>Bank rules that reached the player at least once. Scoped to the bank for the same
    /// reason as <see cref="RulesFired"/> — see its remarks.</summary>
    public required int RulesTold { get; init; }

    /// <summary>Bank rules that fired and were <b>never once told</b>. Authored content the player has
    /// no way to know exists.</summary>
    public int RulesFiredButNeverTold => RulesFired - RulesTold;

    /// <summary><b>The old VFB.Q1 number, saturated, kept on purpose.</b> Mean over nights of the
    /// distinct texts <i>within</i> that night — i.e. <c>min(pool, summary_lines)</c> averaged. It is
    /// emitted because it is the figure written down in <c>plans/PLAN-village-fishbowl.md</c> and
    /// <c>FISHBOWL.md</c>, and because the gap between it and <see cref="DistinctTexts"/> is the
    /// finding. Do not read it as a health measure; it is a ceiling detector that has been touching
    /// the ceiling since it was written.</summary>
    public required double Vfb_Q1_AvgDistinctPerNight { get; init; }

    /// <summary>Nights whose <i>within-night</i> distinct count was under 4 — the old starvation
    /// test, kept alongside its metric for the same reason.</summary>
    public required int NightsBelowFourWithinNight { get; init; }

    /// <summary>
    /// Measure a finished run. Reads only finalized days (<see cref="World.DayHashes"/>), so every
    /// <see cref="Summarizer.Render"/> below is a pure read of a frozen gate.
    /// </summary>
    public static Variety Measure(World w)
    {
        var nights = w.DayHashes.Keys.OrderBy(d => d).ToList();

        // The bank. `rules_*` mean rules, and the chronicle carries ids that are not ones — the board
        // files expiries under Board.ExpiredStoryletId, which has no entry here on purpose. This is the
        // same set RunReport.RulesNode tests to emit `in_bank`, and it is asked of the town rather than
        // matched against a literal id: a second synthetic source added later needs no edit here.
        var bank = w.Town.Storylets.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);

        var allTexts = new HashSet<string>(StringComparer.Ordinal);
        var seenBefore = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string>? previousNight = null;

        int delivered = 0, repeatsPrev = 0, repeatsAny = 0, linesWithPrior = 0;
        int withinNightDistinctSum = 0, nightsBelowFour = 0;
        var rulesTold = new HashSet<string>(StringComparer.Ordinal);

        foreach (int night in nights)
        {
            var lines = Summarizer.Render(w, night);
            var textsTonight = new HashSet<string>(StringComparer.Ordinal);

            foreach (var l in lines)
            {
                delivered++;
                allTexts.Add(l.Text);
                textsTonight.Add(l.Text);
                if (bank.Contains(l.StoryletId)) rulesTold.Add(l.StoryletId);

                if (previousNight is not null)
                {
                    linesWithPrior++;
                    if (previousNight.Contains(l.Text)) repeatsPrev++;
                    if (seenBefore.Contains(l.Text)) repeatsAny++;
                }
            }

            withinNightDistinctSum += textsTonight.Count;
            if (textsTonight.Count < 4) nightsBelowFour++;

            foreach (var t in textsTonight) seenBefore.Add(t);
            previousNight = textsTonight;
        }

        var beats = w.Chronicle.Where(e => nights.Contains(e.Day)).ToList();

        return new Variety
        {
            Nights = nights.Count,
            DeliveredLines = delivered,
            DistinctTexts = allTexts.Count,
            RepeatsOfPreviousNight = repeatsPrev,
            RepeatsOfAnyPriorNight = repeatsAny,
            LinesWithAPriorNight = linesWithPrior,
            BeatsFired = beats.Count,
            RulesFired = beats.Select(e => e.StoryletId).Where(bank.Contains)
                              .Distinct(StringComparer.Ordinal).Count(),
            RulesTold = rulesTold.Count,
            Vfb_Q1_AvgDistinctPerNight = nights.Count == 0 ? 0 : (double)withinNightDistinctSum / nights.Count,
            NightsBelowFourWithinNight = nightsBelowFour,
        };
    }

    /// <summary>The machine-readable form, embedded in <c>--report</c> and printed by <c>--soak</c>.</summary>
    public JsonObject ToJson() => new()
    {
        ["nights"] = Nights,
        ["delivered_lines"] = DeliveredLines,
        ["distinct_texts"] = DistinctTexts,
        ["novelty_rate"] = Math.Round(NoveltyRate, 4),
        ["repeats_of_previous_night"] = RepeatsOfPreviousNight,
        ["repeats_of_any_prior_night"] = RepeatsOfAnyPriorNight,
        ["lines_with_a_prior_night"] = LinesWithAPriorNight,
        ["repeat_rate_previous_night"] = Math.Round(RepeatRatePreviousNight, 4),
        ["repeat_rate_any_prior_night"] = Math.Round(RepeatRateAnyPriorNight, 4),
        ["beats_fired"] = BeatsFired,
        ["beats_told"] = BeatsTold,
        ["told_per_fired"] = Math.Round(ToldPerFired, 4),
        ["rules_fired"] = RulesFired,
        ["rules_told"] = RulesTold,
        ["rules_fired_but_never_told"] = RulesFiredButNeverTold,
        // Kept, and labelled, rather than deleted: it is the number the plan cites.
        ["vfb_q1_avg_distinct_per_night"] = Math.Round(Vfb_Q1_AvgDistinctPerNight, 4),
        ["vfb_q1_nights_below_four"] = NightsBelowFourWithinNight,
        ["vfb_q1_note"] = "SATURATED: distinct texts *within* a summary already truncated to "
                        + "summary_lines, i.e. min(pool, summary_lines) averaged. Reads 5.00 with 30 of "
                        + "46 rules deleted. Use distinct_texts / novelty_rate instead.",
    };
}

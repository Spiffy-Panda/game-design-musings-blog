using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// The novelty/fatigue term in <see cref="Summarizer.Deliver"/>'s ordering (ruled 2026-07-16, after
/// measurement showed the town said 29 distinct sentences in a fortnight and never once told 23 of
/// the rules that fired).
///
/// <para><b>Built on a synthetic bank, on purpose.</b> Every test here constructs its own chronicle
/// with hand-picked tellabilities instead of running the golden town, because the property being
/// pinned is an <i>inequality</i> — "two tellings sink 0.90 below 0.25" — and asserting it against
/// whatever the fixture happens to score would pin the fixture, not the term. A synthetic bank makes
/// the arithmetic checkable by hand in the assertion itself, and keeps these tests alive when
/// somebody retunes a storylet.</para>
///
/// <para><b>Nothing here asserts against a shared singleton.</b> Each test builds a fresh
/// <see cref="World"/> and sets the knobs it depends on explicitly, including the ones it wants at
/// their defaults. Today's hard-won lesson was a test that pinned a mutable static which a sibling
/// test healed in place, so it passed against broken code; a test that inherits ambient config has
/// the same shape of hole.</para>
/// </summary>
public class M3_NoveltyFatigueTests
{
    // --- the headline: reach ----------------------------------------------------------------

    /// <summary>
    /// <b>The property the whole change exists for.</b> Tellability is authored-static per rule, so
    /// without fatigue <see cref="Summarizer"/> is a fixed leaderboard and a rule below the fifth
    /// place is unreachable <i>forever</i>. Reordering the winners is not a fix; the term has to be
    /// able to push the best beat in the bank under the dullest one.
    /// <para>The arithmetic is the assertion. At decay 0.5: night 1 shiny scores 0.90 and wins.
    /// Night 2 it is 0.90 × 0.5 = 0.45, still over dull's 0.25, and wins again. Night 3 it is
    /// 0.90 × 0.25 = 0.225, <i>under</i> 0.25 — and dull, which has fired every night and been told
    /// nothing, finally speaks.</para>
    /// </summary>
    [Fact]
    public void Two_Tellings_Sink_The_Best_Beat_In_The_Bank_Below_The_Dullest_Fresh_One()
    {
        var w = Bank(nights: 3, decay: 0.5, ("shiny", 0.90), ("dull", 0.25));

        Assert.Equal("shiny", Told(w, 1));
        Assert.Equal("shiny", Told(w, 2));
        Assert.Equal("dull", Told(w, 3));   // the waterline is crossed, not nudged
    }

    /// <summary>
    /// <b>The trap, pinned.</b> <c>a-fair-hand</c>, <c>market-cheer</c>, <c>the-daily-grind</c> and
    /// <c>carefuller-math</c> fire every single night and were told zero times in a fortnight. A term
    /// keyed on how often a rule <i>fires</i> would fatigue exactly those rules — the ones most in
    /// need of surfacing — and leave the town quieter than it found it.
    /// <para>So: dull fires three times a night, every night, and is never told. If the ledger counted
    /// firings it would sit at 0.25 × 0.5⁶ ≈ 0.004 by night 3 and shiny (0.225) would keep the slot
    /// forever. It is told, because telling is what tires.</para>
    /// </summary>
    [Fact]
    public void Fatigue_Counts_Tellings_Not_Firings()
    {
        var w = Bank(nights: 3, decay: 0.5, ("shiny", 0.90), ("dull", 0.25), ("dull", 0.25), ("dull", 0.25));

        Assert.Equal(3, w.Chronicle.Count(e => e.Day == 1 && e.StoryletId == "dull"));   // guard: it really fires 3×
        Assert.Equal("dull", Told(w, 3));
    }

    /// <summary>A rule told twice in one night is <b>one</b> night in the ledger: that is one thing
    /// the player heard about, and counting it per line would fatigue a rule for the engine's
    /// verbosity rather than for the player's boredom.</summary>
    [Fact]
    public void A_Rule_Told_Twice_In_One_Night_Is_Fatigued_Once()
    {
        // The only rule in the bank, firing twice a night, with room for both — so night 1 really
        // does deliver "dull" on two lines, which is the case the ledger has to collapse.
        var w = Bank(nights: 2, decay: 0.5, ("dull", 0.25), ("dull", 0.25));
        w.SetKnob("summary_lines", 2);

        Assert.Equal(2, Summarizer.Render(w, 1).Count(l => l.StoryletId == "dull"));   // guard: two lines
        Assert.Equal(1, Summarizer.TellingsBefore(w, 2)["dull"]);                      // one night
    }

    // --- the ablation is exact --------------------------------------------------------------

    /// <summary>
    /// <c>novelty_decay = 1.0</c> must be the pre-novelty Summarizer <i>exactly</i>, not
    /// approximately — it is the off position of the knob and the control arm of every A/B run
    /// against this change. <c>1.0^n = 1</c> for all n, and the ledger pass is skipped outright.
    /// </summary>
    [Fact]
    public void Decay_Of_One_Is_Exactly_The_Fixed_Leaderboard()
    {
        var w = Bank(nights: 5, decay: 1.0, ("shiny", 0.90), ("dull", 0.25));

        // The defect, reproduced on demand: shiny wins every night forever and dull is unreachable.
        Assert.All(Enumerable.Range(1, 5), night => Assert.Equal("shiny", Told(w, night)));
        Assert.Empty(Summarizer.TellingsBefore(w, 5));   // the pass is skipped, not merely neutral
    }

    // --- it is a RENDERING knob: live, retroactive, and hash-invisible -----------------------

    /// <summary>
    /// The knob's family contract: it re-presents days that already happened, on read, without
    /// re-simulating them — and cannot reach the determinism spine. If this ever needs a cached
    /// ledger to pass, the cache is the bug.
    /// </summary>
    [Fact]
    public void Novelty_Decay_Re_Orders_A_Finished_Day_On_Read_Without_Touching_The_Hash()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        for (int i = 0; i < 6; i++) sim.RunToDawn();
        var w = sim.World;
        var hashesBefore = w.DayHashes.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();

        w.SetKnob("novelty_decay", 1.0);
        var leaderboard = Summarizer.Render(w, 5).Select(l => l.StoryletId).ToList();
        w.SetKnob("novelty_decay", 0.2);
        var fatigued = Summarizer.Render(w, 5).Select(l => l.StoryletId).ToList();

        Assert.NotEmpty(leaderboard);
        Assert.NotEqual(leaderboard, fatigued);          // a finished night re-ordered, retroactively
        Assert.Equal(hashesBefore, w.DayHashes.OrderBy(kv => kv.Key).Select(kv => kv.Value));

        // Same knob, same answer: the term is deterministic, not merely different.
        w.SetKnob("novelty_decay", 1.0);
        Assert.Equal(leaderboard, Summarizer.Render(w, 5).Select(l => l.StoryletId));
    }

    /// <summary>
    /// The ledger is keyed on rule id, never on rendered text — so <c>actionability</c> cannot move
    /// the ordering. Text is a function of the dial (<see cref="Text.Actionability.Pick"/>), so a
    /// text-keyed ledger would make two rendering knobs move each other, and the tuning instrument
    /// would stop isolating one variable. It would also score <i>better</i> on the variety metric,
    /// which is exactly why it is worth pinning that it does not happen.
    /// </summary>
    [Fact]
    public void The_Ledger_Is_Keyed_On_Rule_Id_So_Actionability_Cannot_Reorder()
    {
        var sim = new Simulation(TestSupport.LoadGoldenTown());
        for (int i = 0; i < 6; i++) sim.RunToDawn();
        var w = sim.World;
        w.SetKnob("novelty_decay", 0.5);

        w.SetKnob("actionability", 0.05);
        var quiet = Summarizer.Render(w, 5);
        w.SetKnob("actionability", 0.95);
        var loud = Summarizer.Render(w, 5);

        Assert.NotEqual(quiet.Select(l => l.Text), loud.Select(l => l.Text));            // register moved
        Assert.Equal(quiet.Select(l => l.StoryletId), loud.Select(l => l.StoryletId));   // order did not
    }

    // --- helpers ----------------------------------------------------------------------------

    /// <summary>The single rule delivered on <paramref name="night"/> (these banks run at
    /// <c>summary_lines = 1</c>, so "who got the slot?" is the whole question).</summary>
    private static string Told(World w, int night) => Summarizer.Render(w, night).Single().StoryletId;

    /// <summary>
    /// A world whose chronicle is exactly the given rules, fired once each per night for
    /// <paramref name="nights"/> nights. Repeat a rule in <paramref name="rules"/> to make it fire
    /// more than once a night.
    /// <para>Every knob the ordering reads is set here rather than inherited from the fixture's
    /// <c>simconfig.json</c>, so these tests keep testing the term after somebody retunes the town.</para>
    /// </summary>
    private static World Bank(int nights, double decay, params (string Id, double Tellability)[] rules)
    {
        var w = World.Build(TestSupport.LoadGoldenTown());
        w.Chronicle.Clear();

        w.SetKnob("novelty_decay", decay);
        w.SetKnob("summary_lines", 1);
        // Off, so Candidates is the whole day and the gate cannot quietly drop a synthetic beat whose
        // cast is empty. The hearsay gate has its own tests; this file is about the ordering.
        w.SetKnob("hearsay_required", 0);

        for (int day = 1; day <= nights; day++)
            for (int i = 0; i < rules.Length; i++)
                w.Chronicle.Add(new ChronicleEntry
                {
                    Day = day, Slot = i, StoryletId = rules[i].Id, Kind = "test",
                    PlaceId = "market-row", PlaceName = "Market Row",
                    Participants = new List<string>(),   // no cast ⇒ no carrier bump to reason around
                    Tellability = rules[i].Tellability,
                    Hearsay = $"{rules[i].Id} (hearsay)",
                    Gossip = $"{rules[i].Id} (gossip)",
                    Report = $"{rules[i].Id} (report)",
                });

        // Past the last night, so Candidates reads frozen gates and never re-seals against occupancy.
        w.Day = nights + 1;
        return w;
    }
}

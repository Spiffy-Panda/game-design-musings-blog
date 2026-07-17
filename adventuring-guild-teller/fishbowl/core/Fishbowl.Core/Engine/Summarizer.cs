using Fishbowl.Core.Text;

namespace Fishbowl.Core.Engine;

/// <summary>One rendered dawn-summary line, with a link back to its chronicle entry.</summary>
public sealed class SummaryLine
{
    public required int Day { get; init; }
    public required int Slot { get; init; }
    public required string StoryletId { get; init; }
    public required string Text { get; init; }        // rendered at the current dial
    public required double Tellability { get; init; }
    public required string PlaceName { get; init; }
    public required List<string> Participants { get; init; }
}

/// <summary>
/// The Summarizer (research-page register + FBS.6 selection lens, thinned): picks 5±2 chronicle
/// entries by tellability, filtered through a hearsay-lite layer — an event reaches the summary
/// only if a gossip-carrier witnessed it or later shared a room with a witness. It quotes the
/// town's telephone game, not the engine log. Each line renders through the actionability dial.
///
/// <para><b>The dawn/read split — the shape that makes the rendering knobs live.</b> Of the phases
/// below exactly one depends on the day's occupancy, and occupancy is a <i>current-day-only</i>
/// array (<see cref="World.OccupantsAt"/>, rebuilt by <c>Clockwork.ResolveDay</c> on every day
/// advance). That one phase stays at dawn; the rest derive on read:</para>
/// <list type="bullet">
///   <item><b>Gate</b> — <see cref="SealDay"/>, hearsay-lite. Needs the day's occupancy ⇒ <b>dawn
///   only</b>. It is deliberately <i>unconditional</i>: the flag is frozen whatever
///   <c>hearsay_required</c> currently says, so switching that knob on later still finds a truthful
///   gate to filter against.</item>
///   <item><b>Filter</b> (<c>hearsay_required</c>) · <b>Order</b> (<see cref="Score"/> — tellability,
///   a carrier bump, and fatigue per recent telling, <c>novelty_decay</c>) · <b>Take</b>
///   (<c>summary_lines</c>) · <b>Pick</b> (<c>actionability</c>) — all pure over the frozen gate ⇒
///   <b>live on read</b>.</item>
/// </list>
/// <para>Because every phase after the gate is a pure function of frozen state and the current
/// knobs, re-rendering a day at the knobs dawn held is byte-identical to what dawn produced. The
/// four rendering knobs therefore re-present the current day without re-simulating it, which is
/// the whole point of a tuning instrument: one variable moves, not two.</para>
/// <para><b>Ordering reads history, and that is load-bearing.</b> <see cref="Score"/> fatigues a rule
/// by how often it was <i>told</i> in the last <see cref="NoveltyWindow"/> nights, so night N's
/// ranking depends on night N−1's delivery. Nothing caches that: it is re-derived by a forward fold
/// over the chronicle (<see cref="TellingsBefore"/>) on every read, which is what keeps
/// <c>novelty_decay</c> retroactive — move it and every finished night re-orders. The cost is a
/// render that is linear in nights instead of constant; see <see cref="TellingsBefore"/>.</para>
/// <para><b>Determinism.</b> The day-hash is sealed in <c>Simulation.FinalizeDay</c> <i>before</i>
/// any of this runs, and <see cref="World.ToHashNode"/> contains no summary, no config and no
/// register — so nothing here can reach the determinism spine. The one mutation on the path
/// (<see cref="ChronicleEntry.CarriedByGossip"/>) is hash-invisible: the chronicle digest emits
/// only slot/id/who.</para>
/// <para><b>There is no stored summary, on purpose.</b> The gate lives on the chronicle entry, which
/// <c>Snapshot</c> already persists, so a summary survives a save/load round-trip for free — where
/// a cached one silently did not, behind a green hash-only test.</para>
/// </summary>
public static class Summarizer
{
    /// <summary>
    /// <b>DAWN.</b> Freeze hearsay-lite onto the day's chronicle entries, while that day's occupancy
    /// is still the loaded one. This is the only occupancy-dependent phase, and the reason the rest
    /// can be derived on read: a past day must never be re-gated against today's co-presence.
    /// </summary>
    public static void SealDay(World world, int day)
    {
        var carriers = CarriersOf(world);

        foreach (var e in world.Chronicle.Where(e => e.Day == day))
            e.CarriedByGossip = IsCarried(world, e, carriers);
    }

    /// <summary>The town's gossip-carriers: everyone holding a trait flagged
    /// <c>hearsay_carrier</c>. Public because <c>--lint</c> needs the same set this gate uses;
    /// it kept its own copy, and a second definition of "who can carry news" is a second thing
    /// to keep in sync.</summary>
    public static HashSet<string> CarriersOf(World world) =>
        world.Townees
            .Where(t => t.Traits.Any(tr => world.Town.TraitById.TryGetValue(tr, out var td) && td.HearsayCarrier))
            .Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// <b>Hearsay-lite, in one place.</b> Would a beat with this cast, at this place and slot, be
    /// carried? <see cref="SealDay"/> gates real chronicle entries with it and <c>--lint</c> predicts
    /// hypothetical firings with it, so the gate and the check that audits the gate cannot drift.
    /// <para>They did drift: <c>--lint</c>'s <c>stranded-beats</c> modelled only clause (b) — "a carrier
    /// is in the room" — and so called a beat provably untellable while clause (c) was carrying it to
    /// the summary in the actual sim. An error-class check that cries wolf gets an author to delete
    /// good content. The clauses now have exactly one definition, and this is it.</para>
    /// </summary>
    public static bool WouldBeCarried(World world, int slot, string placeId,
        IReadOnlyList<string> participants, IReadOnlySet<string> carriers)
    {
        // (a) a carrier is a participant, or (b) a carrier was co-present at the event, or
        // (c) a carrier later shares a room with a participant that same day.
        if (participants.Any(carriers.Contains)) return true;

        var atEvent = world.OccupantsAt(slot).TryGetValue(placeId, out var occ) ? occ : new List<string>();
        if (atEvent.Any(carriers.Contains)) return true;

        for (int s = slot + 1; s < world.SlotsPerDay; s++)
        {
            foreach (var (_, here) in world.OccupantsAt(s))
            {
                bool carrierHere = here.Any(carriers.Contains);
                bool participantHere = here.Any(participants.Contains);
                if (carrierHere && participantHere) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// <b>READ — filter.</b> Summary-eligible entries for the day: hearsay-lite applied over the gate
    /// frozen at dawn. Also the pool instrument — a caller can count it.
    /// <para>The in-progress day has not been sealed yet, and its occupancy <i>is</i> the loaded one
    /// (the clockwork resolves a whole day up front), so gate it live — that keeps the mid-day stats
    /// strip reading true. Any earlier day reads its own frozen flag and never touches occupancy,
    /// which is what stops a read from quietly rewriting the historical record.</para>
    /// </summary>
    public static List<ChronicleEntry> Candidates(World world, int day)
    {
        if (day >= world.Day) SealDay(world, day);

        var todays = world.Chronicle.Where(e => e.Day == day).ToList();
        return world.Config.HearsayRequired
            ? todays.Where(e => e.CarriedByGossip).ToList()
            : todays;
    }

    /// <summary>
    /// <b>READ — deliver.</b> Order → take → pick over already-filtered candidates: the lines the town
    /// actually tells tonight. Pure, and every knob it reads is a rendering knob.
    /// <para>Needs <paramref name="day"/> explicitly rather than reading it off the candidates: the
    /// fatigue ledger is "what was told <i>before</i> tonight", and an empty candidate list still has
    /// a tonight. Inferring the day from the entries would make a barren night un-orderable.</para>
    /// </summary>
    public static List<SummaryLine> Deliver(World world, int day, IEnumerable<ChronicleEntry> candidates)
    {
        double dial = world.Config.Actionability;

        return Order(world, candidates, TellingsBefore(world, day))
            .Select(e => new SummaryLine
            {
                Day = e.Day, Slot = e.Slot, StoryletId = e.StoryletId,
                Text = Actionability.Pick(e, dial), Tellability = e.Tellability,
                PlaceName = e.PlaceName, Participants = e.Participants,
            })
            .ToList();
    }

    /// <summary><b>READ.</b> The day's delivered summary: filter → order → take → pick.</summary>
    public static List<SummaryLine> Render(World world, int day) => Deliver(world, day, Candidates(world, day));

    /// <summary>
    /// How long the town's memory is, in nights. A telling ages out of the ledger after this many
    /// nights and the rule is fully fresh again.
    /// <para><b>Why a window and not a lifetime tally.</b> Two reasons, one of them a bug. The design
    /// reason: a town that will never again mention the mill fire because it mentioned it once, two
    /// hundred nights ago, is not a living town — novelty is about what the <i>player</i> still
    /// remembers, and the player does not remember night 3. The mechanical reason: a lifetime tally
    /// makes the exponent unbounded, so <c>decay^n</c> underflows to zero on a long run and the
    /// ordering silently collapses into the slot/id tiebreak — a fixed leaderboard again, which is
    /// the exact defect this term exists to remove.</para>
    /// <para>Deliberately a constant and not a knob. One knob was the brief; a window knob would be a
    /// second tuning surface to document, and the value is not delicate — the term's reach comes from
    /// <c>novelty_decay</c>, not from this.</para>
    /// </summary>
    public const int NoveltyWindow = 7;

    /// <summary>
    /// <b>READ — the novelty ledger.</b> Rule id → nights inside the trailing window on which that
    /// rule actually reached the player, as of <paramref name="day"/>.
    ///
    /// <para><b>This is why it derives on read, and it is the expensive part.</b> What was told on
    /// night N−1 is not stored anywhere — <c>World</c> holds no summary, on purpose — so it has to be
    /// re-derived, and deriving it needs night N−2's, and so on. The pass therefore starts at the
    /// first chronicled night and walks <i>forward</i>, carrying the ledger. It is a forward fold, not
    /// a recursion: <see cref="Order"/> takes the ledger as an argument and never reaches back for
    /// one, so there is no re-entry and the cost is linear in nights rather than exponential.</para>
    ///
    /// <para><b>The cost is accepted, not overlooked.</b> A render is O(nights × candidates) and
    /// <c>Variety.Measure</c> renders every night, so a measured run is O(nights²). Memoising it is
    /// the obvious fix and is exactly the cache this file exists to not have: a stored ledger would
    /// stop <c>novelty_decay</c> from re-rendering finished days, which is the whole property being
    /// bought. At a fortnight this is a few thousand operations. If it ever matters, the fix is a
    /// caller-scoped memo passed in — never a field on <see cref="World"/>.</para>
    ///
    /// <para><b>Keyed on rule id, never on rendered text.</b> Text is what the variety metric counts,
    /// so keying on it would score better and would be wrong: <see cref="Actionability.Pick"/> makes
    /// the text a function of the <c>actionability</c> dial, so a text-keyed ledger would let moving
    /// the register silently reorder the lines. Two rendering knobs that move each other are not two
    /// knobs. The id is dial-independent, so <c>actionability</c> and <c>novelty_decay</c> stay
    /// orthogonal — and "the town is tired of this story" is the thing being modelled anyway.</para>
    /// </summary>
    public static Dictionary<string, int> TellingsBefore(World world, int day)
    {
        var ledger = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        // Fatigue off ⇒ decay^n == 1 for every n, so the ledger cannot change an ordering. Skipping
        // the pass is not just a shortcut: it is what makes novelty_decay=1.0 provably identical to
        // the pre-novelty Summarizer rather than merely arithmetically equal to it.
        if (world.Config.NoveltyDecay >= 1.0) return Counts(ledger, day);

        for (int night = 1; night < day; night++)
        {
            // Past nights only, so Candidates reads each night's own frozen gate and never re-seals
            // against today's occupancy — the property Re_Reading_An_Old_Day_... pins.
            foreach (var e in Order(world, Candidates(world, night), Counts(ledger, night)))
            {
                if (!ledger.TryGetValue(e.StoryletId, out var nights)) ledger[e.StoryletId] = nights = new List<int>();
                // A night, not a line: two fires of one rule in one night is one thing the player
                // heard about, and counting it twice would fatigue a rule for the engine's verbosity.
                if (nights.Count == 0 || nights[^1] != night) nights.Add(night);
            }
        }
        return Counts(ledger, day);
    }

    /// <summary>Collapse the ledger to "tellings still inside the window looking back from
    /// <paramref name="day"/>".</summary>
    private static Dictionary<string, int> Counts(Dictionary<string, List<int>> ledger, int day) =>
        ledger.ToDictionary(kv => kv.Key, kv => kv.Value.Count(n => n > day - 1 - NoveltyWindow),
            StringComparer.Ordinal);

    /// <summary>
    /// <b>READ — order + take.</b> The ranking, pure over an explicit ledger. Separated from
    /// <see cref="Deliver"/> so the forward fold above can rank a night without recomputing a ledger
    /// it is in the middle of building.
    /// </summary>
    private static IEnumerable<ChronicleEntry> Order(
        World world, IEnumerable<ChronicleEntry> candidates, Dictionary<string, int> told) =>
        candidates
            .OrderByDescending(e => Score(world, e, told))
            .ThenBy(e => e.Slot)
            .ThenBy(e => e.StoryletId, StringComparer.Ordinal)
            .Take(world.Config.SummaryLines);

    /// <summary>
    /// The ordering key: authored tellability, a carrier bump, and geometric fatigue per recent
    /// telling.
    ///
    /// <para><b>Why multiply rather than subtract.</b> Tellability is authored-static per rule, so
    /// <c>Score</c> without fatigue is a fixed leaderboard: <c>rent-quarrel</c> is 0.80 every night it
    /// fires and <c>a-fair-hand</c> is 0.25 every night it fires, forever. Surfacing the bottom of the
    /// bank therefore needs the top of the bank to be pushed <i>below</i> it — a swing of 0.65, not a
    /// nudge. A subtractive penalty big enough to do that would drive high-tellability rules negative
    /// and invert the bank; a multiplier cannot change a sign, and its reach is unbounded in the
    /// exponent. At the default 0.5, two tellings put the best entry in the bank (0.90 with the bump
    /// → 0.225) under the most mundane fresh one (0.25), so every rule that fires can outrank every
    /// rule that has just had its turn. That is the property the term needed, stated as an inequality
    /// rather than hoped for — and it is why the default is set from that inequality rather than from
    /// the variety metric, which keeps rewarding a harder decay long after the reach is bought.</para>
    ///
    /// <para><b>An untold rule never decays.</b> The ledger counts tellings, not firings — which is
    /// the whole design. <c>a-fair-hand</c>, <c>market-cheer</c>, <c>the-daily-grind</c> and
    /// <c>carefuller-math</c> fire every single night and were told zero times; a firing-keyed term
    /// would have fatigued precisely the rules that most needed surfacing and made the bank quieter.
    /// Telling is the event the player experiences, so telling is what tires.</para>
    /// </summary>
    private static double Score(World world, ChronicleEntry e, Dictionary<string, int> told)
    {
        // Tellability, nudged up when a carrier is directly involved (louder gossip travels).
        double bump = e.Participants.Any(p =>
            world.TowneeById.TryGetValue(p, out var t)
            && t.Traits.Any(tr => world.Town.TraitById.TryGetValue(tr, out var td) && td.HearsayCarrier))
            ? 0.05 : 0.0;

        double fatigue = told.TryGetValue(e.StoryletId, out var n) && n > 0
            ? Math.Pow(world.Config.NoveltyDecay, n)
            : 1.0;
        return (e.Tellability + bump) * fatigue;
    }

    private static bool IsCarried(World world, ChronicleEntry e, IReadOnlySet<string> carriers) =>
        WouldBeCarried(world, e.Slot, e.PlaceId, e.Participants, carriers);
}

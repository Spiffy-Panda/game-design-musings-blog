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
///   <item><b>Filter</b> (<c>hearsay_required</c>) · <b>Order</b> (<see cref="Score"/> — tellability
///   plus a carrier bump; reads no knob and no mutable state) · <b>Take</b> (<c>summary_lines</c>) ·
///   <b>Pick</b> (<c>actionability</c>) — all pure over the frozen gate ⇒ <b>live on read</b>.</item>
/// </list>
/// <para>Because the ordering is knob-independent and total, re-rendering a day at the knobs dawn
/// held is byte-identical to what dawn produced — same frozen input, same pure operations. The
/// three rendering knobs therefore re-present the current day without re-simulating it, which is
/// the whole point of a tuning instrument: one variable moves, not two.</para>
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
        var carriers = world.Townees
            .Where(t => t.Traits.Any(tr => world.Town.TraitById.TryGetValue(tr, out var td) && td.HearsayCarrier))
            .Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var e in world.Chronicle.Where(e => e.Day == day))
            e.CarriedByGossip = IsCarried(world, e, carriers);
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
    /// </summary>
    public static List<SummaryLine> Deliver(World world, IEnumerable<ChronicleEntry> candidates)
    {
        double dial = world.Config.Actionability;
        int want = world.Config.SummaryLines;

        return candidates
            .OrderByDescending(e => Score(world, e))
            .ThenBy(e => e.Slot)
            .ThenBy(e => e.StoryletId, StringComparer.Ordinal)
            .Take(want)
            .Select(e => new SummaryLine
            {
                Day = e.Day, Slot = e.Slot, StoryletId = e.StoryletId,
                Text = Actionability.Pick(e, dial), Tellability = e.Tellability,
                PlaceName = e.PlaceName, Participants = e.Participants,
            })
            .ToList();
    }

    /// <summary><b>READ.</b> The day's delivered summary: filter → order → take → pick.</summary>
    public static List<SummaryLine> Render(World world, int day) => Deliver(world, Candidates(world, day));

    private static double Score(World world, ChronicleEntry e)
    {
        // Tellability, nudged up when a carrier is directly involved (louder gossip travels).
        double bump = e.Participants.Any(p =>
            world.TowneeById.TryGetValue(p, out var t)
            && t.Traits.Any(tr => world.Town.TraitById.TryGetValue(tr, out var td) && td.HearsayCarrier))
            ? 0.05 : 0.0;
        return e.Tellability + bump;
    }

    private static bool IsCarried(World world, ChronicleEntry e, IReadOnlySet<string> carriers)
    {
        // (a) a carrier is a participant, or (b) a carrier was co-present at the event, or
        // (c) a carrier later shares a room with a participant that same day.
        if (e.Participants.Any(carriers.Contains)) return true;

        var atEvent = world.OccupantsAt(e.Slot).TryGetValue(e.PlaceId, out var occ) ? occ : new List<string>();
        if (atEvent.Any(carriers.Contains)) return true;

        for (int s = e.Slot + 1; s < world.SlotsPerDay; s++)
        {
            foreach (var (_, here) in world.OccupantsAt(s))
            {
                bool carrierHere = here.Any(carriers.Contains);
                bool participantHere = here.Any(e.Participants.Contains);
                if (carrierHere && participantHere) return true;
            }
        }
        return false;
    }
}

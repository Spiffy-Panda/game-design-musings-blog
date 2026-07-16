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
/// The Summarizer (research-page register + FBS.6 selection lens, thinned): at dawn, picks
/// 5±2 chronicle entries by tellability, filtered through a hearsay-lite layer — an event
/// reaches the summary only if a gossip-carrier witnessed it or later shared a room with a
/// witness. It quotes the town's telephone game, not the engine log. Each line renders
/// through the actionability dial.
/// </summary>
public static class Summarizer
{
    public static List<SummaryLine> Summarize(World world, int day)
    {
        var candidates = Candidates(world, day);
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

    /// <summary>Summary-eligible entries for the day (hearsay-lite applied). Also the
    /// instrument for the starvation warning — a caller can check the count.</summary>
    public static List<ChronicleEntry> Candidates(World world, int day)
    {
        var carriers = world.Townees
            .Where(t => t.Traits.Any(tr => world.Town.TraitById.TryGetValue(tr, out var td) && td.HearsayCarrier))
            .Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

        var todays = world.Chronicle.Where(e => e.Day == day).ToList();
        foreach (var e in todays) e.CarriedByGossip = IsCarried(world, e, carriers);

        return world.Config.HearsayRequired
            ? todays.Where(e => e.CarriedByGossip).ToList()
            : todays;
    }

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

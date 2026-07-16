using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// L1 Clockwork (FBS.1): resolves each townee's authored day-plan into a slot-by-slot
/// itinerary, producing the co-presence timeline — who is where, together, when. Legible,
/// findable, cheap. It never picks actions; it only places bodies.
/// </summary>
public static class Clockwork
{
    /// <summary>Resolve itineraries + occupancy for the world's current day.</summary>
    public static void ResolveDay(World world)
    {
        world.ResetDayStreams(); // fresh named streams for the new day
        int slots = world.SlotsPerDay;
        var occupants = new Dictionary<string, List<string>>[slots];
        for (int s = 0; s < slots; s++) occupants[s] = new Dictionary<string, List<string>>();

        foreach (var t in world.Townees)
        {
            // Departure schedule: departing_today on the scheduled day, Away thereafter.
            if (t.DepartsDay is int dd)
            {
                t.DepartingToday = dd == world.Day;
                if (world.Day > dd) t.Away = true;
            }

            var itinerary = new string[slots];
            var activity = new string[slots];
            var mode = new string[slots];
            var asleep = new bool[slots];
            // Default: home, idle (defensive; authored plans cover the full day).
            for (int s = 0; s < slots; s++) { itinerary[s] = t.Home; activity[s] = "at home"; mode[s] = "home"; }

            var plan = world.Town.DayPlans[t.DayplanId];
            var blocks = (t.Away && plan.Away is { Count: > 0 }) ? plan.Away : plan.Weekday;

            foreach (var block in blocks)
            {
                string anchor = ResolvePlace(t, block.Place);
                string blockMode = ModeOf(block.Place);
                bool blockAsleep = block.Activity.Contains("asleep", StringComparison.OrdinalIgnoreCase);
                for (int s = block.Start; s < block.End && s < slots; s++)
                {
                    itinerary[s] = anchor;
                    activity[s] = block.Activity;
                    mode[s] = blockMode;
                    asleep[s] = blockAsleep;
                    if (anchor == "away") continue;
                    AddOccupant(occupants[s], anchor, t.Id);
                    // Roaming (courier): co-present at every roam place this slot.
                    foreach (var roam in block.Roams ?? Enumerable.Empty<string>())
                        if (roam != anchor) AddOccupant(occupants[s], roam, t.Id);
                }
            }

            t.Itinerary = itinerary;
            t.Activity = activity;
            t.Mode = mode;
            t.Asleep = asleep;
        }

        world.SetOccupants(occupants);
    }

    private static string ResolvePlace(Townee t, string token) => token switch
    {
        "home" => t.Home,
        "work" => t.Work ?? t.Home,
        "away" => "away",
        _ when token.StartsWith("haunt:", StringComparison.Ordinal) => token["haunt:".Length..],
        _ => t.Home,
    };

    private static string ModeOf(string token) => token switch
    {
        "work" => "work",
        "away" => "away",
        _ when token.StartsWith("haunt:", StringComparison.Ordinal) => "haunt",
        _ => "home",
    };

    private static void AddOccupant(Dictionary<string, List<string>> slotOcc, string place, string id)
    {
        if (!slotOcc.TryGetValue(place, out var list)) slotOcc[place] = list = new List<string>();
        list.Add(id);
    }
}

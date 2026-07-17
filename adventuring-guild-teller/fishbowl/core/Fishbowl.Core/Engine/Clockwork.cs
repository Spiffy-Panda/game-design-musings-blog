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
            // Departure schedule: departing_today on the scheduled day, then a BARE outing thereafter —
            // Phase.Outing with no Outing record, which Clockwork routes off-screen ("away"). This is the
            // legacy one-way stand-in (PNO.D6); it only fires from Daily, so it never overrides a real
            // outing a townee took, and Outings.ResolveDay leaves a bare departure alone (no outing to end).
            if (t.DepartsDay is int dd)
            {
                t.DepartingToday = dd == world.Day;
                if (world.Day > dd && t.Phase == Phase.Daily) t.Phase = Phase.Outing;
            }

            var itinerary = new string[slots];
            var activity = new string[slots];
            var mode = new string[slots];
            var asleep = new bool[slots];
            // Default: home, idle (defensive; authored plans cover the full day).
            for (int s = 0; s < slots; s++) { itinerary[s] = t.Home; activity[s] = "at home"; mode[s] = "home"; }

            var plan = world.Town.DayPlans[t.DayplanId];
            var blocks = BlocksFor(world, t, plan);

            foreach (var block in blocks)
            {
                string anchor = ResolvePlace(t, block.Place);
                string blockMode = ModeOf(t, block.Place);
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

    /// <summary>The block list for this townee's current <see cref="Phase"/>. Outing falls back through
    /// the legacy <c>away</c> list to <c>weekday</c>; cooldown falls back through a shared
    /// <c>cooldown-default</c> plan to <c>weekday</c>. The fallbacks mean the 10 non-adventurer townees
    /// (who author no outing/cooldown lists) still resolve to a sane day if the phase machine ever moves
    /// them, rather than defaulting to a blank itinerary.</summary>
    private static IReadOnlyList<DayBlockDto> BlocksFor(World world, Townee t, DayPlanDto plan) => t.Phase switch
    {
        Phase.Outing => plan.Outing is { Count: > 0 } ? plan.Outing
                      : plan.Away is { Count: > 0 } ? plan.Away
                      : plan.Weekday,
        Phase.Cooldown => plan.Cooldown is { Count: > 0 } ? plan.Cooldown
                        : world.Town.DayPlans.TryGetValue("cooldown-default", out var cd) && cd.Weekday.Count > 0 ? cd.Weekday
                        : plan.Weekday,
        _ => plan.Weekday,
    };

    private static string ResolvePlace(Townee t, string token) => token switch
    {
        "home" => t.Home,
        "work" => t.Work ?? t.Home,
        "away" => "away",
        // The dynamic outing anchor: the townee's active site, or off-screen for a bare departure that
        // has no outing (departs_day / the SetAway knob). This is why the token is `site`, not `site:<id>`
        // — the destination is runtime state (which posting they took), not an authored constant.
        "site" => t.Outing?.SiteId ?? "away",
        _ when token.StartsWith("haunt:", StringComparison.Ordinal) => token["haunt:".Length..],
        _ => t.Home,
    };

    private static string ModeOf(Townee t, string token) => token switch
    {
        "work" => "work",
        "away" => "away",
        // At a real site: `outing` mode, which burns restlessness (the trip IS the wanderlust satisfied,
        // PNO.M2 ruling) and — unlike `away` — is NOT frozen by Pressures, so drives drift while out.
        // A bare departure with no site falls back to `away` (off-screen, frozen), unchanged.
        "site" => t.Outing is not null ? "outing" : "away",
        _ when token.StartsWith("haunt:", StringComparison.Ordinal) => "haunt",
        _ => "home",
    };

    private static void AddOccupant(Dictionary<string, List<string>> slotOcc, string place, string id)
    {
        if (!slotOcc.TryGetValue(place, out var list)) slotOcc[place] = list = new List<string>();
        list.Add(id);
    }
}

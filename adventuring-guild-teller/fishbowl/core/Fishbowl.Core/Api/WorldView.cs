using System.Text.Json.Nodes;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Text;

namespace Fishbowl.Core.Api;

/// <summary>
/// Projects world state to JSON strings for the GDScript observatory. Lives in the engine-free
/// core (not the bridge) so the projections are unit-testable and the FishbowlBridge stays a
/// thin marshalling shim. Every method returns a compact JSON string.
/// </summary>
public static class WorldView
{
    private static string S(JsonNode n) => n.ToJsonString();
    private static int DisplaySlot(World w) => Math.Clamp(w.Slot, 0, w.SlotsPerDay - 1);

    public static string ClockJson(World w) => S(new JsonObject
    {
        ["day"] = w.Day,
        ["slot"] = w.Slot,
        ["slots_per_day"] = w.SlotsPerDay,
        ["seed"] = w.Seed,
        ["hash"] = w.DayHashes.TryGetValue(w.Day - 1, out var h) ? h : "—",
        ["clock"] = SlotClock(w.Slot, w.SlotsPerDay),
    });

    public static string RosterJson(World w)
    {
        int slot = DisplaySlot(w);
        var arr = new JsonArray();
        foreach (var t in w.Townees)
        {
            string place = w.PlaceOf(t, slot);
            var (drive, val) = TopPressure(t);
            arr.Add(new JsonObject
            {
                ["id"] = t.Id, ["name"] = t.Name, ["role"] = t.Role, ["away"] = t.Away,
                ["place"] = place,
                ["place_name"] = w.Town.PlaceById.TryGetValue(place, out var p) ? p.Name : place,
                ["activity"] = t.Activity.Length > slot ? t.Activity[slot] : "",
                ["top_drive"] = drive, ["top_value"] = Math.Round(val, 3),
            });
        }
        return S(new JsonObject { ["slot"] = slot, ["townees"] = arr });
    }

    public static string TowneeJson(World w, string id)
    {
        if (!w.TowneeById.TryGetValue(id, out var t)) return "{}";
        int slot = DisplaySlot(w);

        var pressures = new JsonObject();
        foreach (var d in Model.Town.Drives) pressures[d] = Math.Round(t.Pressure(d), 3);

        var regardOut = new JsonArray();
        foreach (var (target, e) in t.Regard.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            regardOut.Add(Edge(w, target, e));

        var regardIn = new JsonArray();
        foreach (var other in w.Townees)
            if (other.Id != id && other.Regard.TryGetValue(id, out var e))
                regardIn.Add(Edge(w, other.Id, e));

        var marks = new JsonArray();
        foreach (var m in t.Marks) marks.Add(new JsonObject { ["day"] = m.Day, ["line"] = m.Line });

        var itinerary = new JsonArray();
        for (int s = 0; s < w.SlotsPerDay; s++)
        {
            string place = w.PlaceOf(t, s);
            itinerary.Add(new JsonObject
            {
                ["slot"] = s, ["clock"] = SlotClock(s, w.SlotsPerDay), ["place"] = place,
                ["place_name"] = w.Town.PlaceById.TryGetValue(place, out var p) ? p.Name : place,
                ["activity"] = t.Activity.Length > s ? t.Activity[s] : "",
            });
        }

        return S(new JsonObject
        {
            ["id"] = t.Id, ["name"] = t.Name, ["role"] = t.Role, ["adventurer"] = t.Adventurer,
            ["away"] = t.Away, ["bio"] = t.Bio,
            ["traits"] = new JsonArray(t.Traits.Select(x => (JsonNode)x!).ToArray()),
            ["pressures"] = pressures, ["regard_out"] = regardOut, ["regard_in"] = regardIn,
            ["marks"] = marks, ["place_now"] = w.PlaceOf(t, slot), ["itinerary"] = itinerary,
        });
    }

    public static string PlacesJson(World w)
    {
        int slot = DisplaySlot(w);
        var occ = w.OccupantsAt(slot);
        var arr = new JsonArray();
        foreach (var p in w.Town.Places.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var people = new JsonArray();
            if (occ.TryGetValue(p.Id, out var ids))
                foreach (var pid in ids)
                    people.Add(new JsonObject { ["id"] = pid, ["name"] = Name(w, pid) });
            arr.Add(new JsonObject
            {
                ["id"] = p.Id, ["name"] = p.Name, ["kind"] = p.Kind, ["board"] = p.Board,
                ["shut"] = p.Shut, ["occupants"] = people,
            });
        }
        return S(new JsonObject { ["slot"] = slot, ["places"] = arr });
    }

    public static string ChronicleJson(World w, int day)
    {
        var arr = new JsonArray();
        foreach (var e in w.Chronicle.Where(e => e.Day == day).OrderBy(e => e.Slot))
        {
            var because = new JsonArray();
            foreach (var b in e.Because) because.Add(new JsonObject { ["label"] = b.Label, ["value"] = b.Value });
            var who = new JsonArray();
            foreach (var pid in e.Participants) who.Add(new JsonObject { ["id"] = pid, ["name"] = Name(w, pid) });
            arr.Add(new JsonObject
            {
                ["day"] = e.Day, ["slot"] = e.Slot, ["clock"] = SlotClock(e.Slot, w.SlotsPerDay),
                ["storylet"] = e.StoryletId, ["kind"] = e.Kind, ["place"] = e.PlaceId, ["place_name"] = e.PlaceName,
                ["participants"] = who, ["tellability"] = Math.Round(e.Tellability, 3), ["because"] = because,
                ["hearsay"] = e.Hearsay, ["gossip"] = e.Gossip, ["report"] = e.Report,
            });
        }
        return S(new JsonObject { ["day"] = day, ["events"] = arr });
    }

    public static string SummaryJson(World w, int day)
    {
        var arr = new JsonArray();
        if (w.Summaries.TryGetValue(day, out var lines))
            foreach (var l in lines)
                arr.Add(new JsonObject
                {
                    ["text"] = l.Text, ["storylet"] = l.StoryletId, ["slot"] = l.Slot,
                    ["place_name"] = l.PlaceName, ["tellability"] = Math.Round(l.Tellability, 3),
                });
        return S(new JsonObject
        {
            ["day"] = day, ["dial"] = w.Config.Actionability,
            ["register"] = Actionability.Of(w.Config.Actionability).ToString().ToLowerInvariant(),
            ["lines"] = arr,
        });
    }

    public static string StatsJson(World w, int day)
    {
        var events = w.Chronicle.Where(e => e.Day == day).ToList();
        var byType = new JsonObject();
        foreach (var g in events.GroupBy(e => e.StoryletId).OrderBy(g => g.Key, StringComparer.Ordinal))
            byType[g.Key] = g.Count();
        var candidates = Summarizer.Candidates(w, day);
        int distinct = candidates.Select(e => e.StoryletId).Distinct().Count();
        return S(new JsonObject
        {
            ["day"] = day, ["events"] = events.Count, ["distinct_candidates"] = distinct,
            ["by_type"] = byType, ["starvation"] = distinct < 4,
        });
    }

    public static string KnobsJson(World w)
    {
        var c = w.Config;
        var rates = new JsonObject();
        foreach (var d in Model.Town.Drives)
            rates[d] = c.PressureRates.TryGetValue(d, out var r) ? r : 1.0;
        return S(new JsonObject
        {
            ["seed"] = w.Seed, ["pressure_rates"] = rates, ["storylet_rate"] = c.StoryletRate,
            ["storylet_cooldown_scale"] = c.StoryletCooldownScale, ["copresence_bonus"] = c.CopresenceBonus,
            ["hearsay_required"] = c.HearsayRequired, ["actionability"] = c.Actionability,
            ["summary_lines"] = c.SummaryLines, ["bio_marks_enabled"] = c.BioMarksEnabled,
        });
    }

    public static string PressureSeriesJson(World w, string id, string drive)
    {
        var key = $"{id}.{drive}";
        var values = new JsonArray();
        if (w.PressureLog.TryGetValue(key, out var list))
        {
            int window = w.SlotsPerDay * 3; // trailing 3 days
            foreach (var v in list.Skip(Math.Max(0, list.Count - window)))
                values.Add(Math.Round(v, 4));
        }
        return S(new JsonObject { ["id"] = id, ["drive"] = drive, ["values"] = values });
    }

    // --- helpers ---
    private static JsonObject Edge(World w, string other, RegardEdge e) => new()
    {
        ["id"] = other, ["name"] = Name(w, other), ["score"] = Math.Round(e.Score, 3),
        ["tags"] = new JsonArray(e.Tags.Select(x => (JsonNode)x!).ToArray()),
    };

    private static (string drive, double val) TopPressure(Townee t)
    {
        string best = Model.Town.Drives[0];
        double bestVal = -1;
        foreach (var d in Model.Town.Drives)
            if (t.Pressure(d) > bestVal) { bestVal = t.Pressure(d); best = d; }
        return (best, bestVal);
    }

    private static string Name(World w, string id) => w.TowneeById.TryGetValue(id, out var t) ? t.Name : id;

    private static string SlotClock(int slot, int slotsPerDay)
    {
        int minutes = slot * (1440 / slotsPerDay);
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }
}

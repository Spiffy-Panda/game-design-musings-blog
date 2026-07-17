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
            bool known = w.Town.PlaceById.TryGetValue(place, out var p);
            arr.Add(new JsonObject
            {
                ["id"] = t.Id, ["name"] = t.Name, ["role"] = t.Role, ["away"] = t.Away,
                ["place"] = place,
                ["place_name"] = known ? p!.Name : place,
                // place_kind / mode / asleep are additive readout-only fields: they give the view a
                // *stable, closed* key to map a row onto (place kind, clockwork mode) instead of the
                // authored display prose, which is free text and changes whenever a day-plan is
                // re-authored. Nothing here feeds ToHashNode — the day-hash is untouched.
                ["place_kind"] = known ? p!.Kind : "",
                ["activity"] = t.Activity.Length > slot ? t.Activity[slot] : "",
                ["mode"] = t.Mode.Length > slot ? t.Mode[slot] : "",
                ["asleep"] = t.Asleep.Length > slot && t.Asleep[slot],
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
            ["away"] = t.Away, ["phase"] = t.Phase.ToString().ToLowerInvariant(), ["outing"] = OutingJson(w, t),
            ["bio"] = t.Bio,
            ["traits"] = new JsonArray(t.Traits.Select(x => (JsonNode)x!).ToArray()),
            ["pressures"] = pressures, ["regard_out"] = regardOut, ["regard_in"] = regardIn,
            ["marks"] = marks, ["place_now"] = w.PlaceOf(t, slot), ["itinerary"] = itinerary,
        });
    }

    /// <summary>An adventurer's current outing — the rail inspector's phase line and the expand modal's
    /// outing block both read this. Null when the townee is not on a real trip (a bare departure has a
    /// phase but no outing record). Leg progress is against the site's authored track.</summary>
    private static JsonNode? OutingJson(World w, Townee t)
    {
        if (t.Outing is not { } o) return null;
        var site = w.Town.SiteById(o.SiteId);
        int legCount = site?.Legs.Count ?? 0;
        string activity = site is not null && o.LegIndex < legCount ? site.Legs[o.LegIndex].Activity : "";
        return new JsonObject
        {
            ["posting"] = o.PostingId, ["site"] = o.SiteId, ["site_name"] = SiteName(o.SiteId),
            ["leg"] = o.LegIndex, ["leg_count"] = legCount, ["activity"] = activity,
            ["slots_into_leg"] = o.SlotsIntoLeg, ["complete"] = o.Complete,
            ["outcome"] = o.Outcome.ToString().ToLowerInvariant(),
        };
    }

    /// <summary>
    /// A townee's whole event log — every chronicle beat they were a participant in, across all days,
    /// <b>newest first</b>. The expand modal's townee view. Distinct from <see cref="ChronicleJson"/>
    /// (one day, everyone): this is one townee, every day, which is the "what has happened to this
    /// person" a player scrubbing a life wants — and the natural place an adventurer's outings read as a
    /// history once they return and the trip itself is silent (`AGT.10`).
    /// </summary>
    public static string TowneeEventsJson(World w, string id)
    {
        var arr = new JsonArray();
        foreach (var e in w.Chronicle.Where(e => e.Participants.Contains(id))
                     .OrderByDescending(e => e.Day).ThenByDescending(e => e.Slot))
            arr.Add(new JsonObject
            {
                ["day"] = e.Day, ["slot"] = e.Slot, ["clock"] = SlotClock(e.Slot, w.SlotsPerDay),
                ["storylet"] = e.StoryletId, ["kind"] = e.Kind, ["place_name"] = e.PlaceName,
                ["gossip"] = e.Gossip, ["report"] = e.Report,
            });
        return S(new JsonObject { ["id"] = id, ["name"] = Name(w, id), ["events"] = arr });
    }

    /// <summary>
    /// The quest board as a kanban (the expand modal's board view): <b>Template</b> (the seed bank — jobs
    /// the town can produce) → <b>Standing</b> (posted, untaken) → <b>Taken</b> (by whom) →
    /// <b>Completed</b> (resolved + expired). A posting's own lifecycle IS the board — no PM vocabulary,
    /// no priority, no points (ruled 2026-07-17). Runtime columns are in filing order (deterministic).
    /// </summary>
    public static string QuestBoardJson(World w)
    {
        var templates = new JsonArray();
        foreach (var tpl in w.Town.Postings.OrderBy(p => p.Id, StringComparer.Ordinal))
            templates.Add(new JsonObject
            {
                ["id"] = tpl.Id, ["requester"] = tpl.Requester, ["requester_name"] = Name(w, tpl.Requester),
                ["reach"] = tpl.Reach, ["reward"] = Math.Round(tpl.Reward, 3),
                ["site_name"] = SiteName(string.IsNullOrWhiteSpace(tpl.Site) ? null : tpl.Site),
            });

        JsonArray Column(Func<Posting, bool> pick)
        {
            var a = new JsonArray();
            foreach (var p in w.Postings.Where(pick)) a.Add(PostingCard(w, p));
            return a;
        }

        return S(new JsonObject
        {
            ["day"] = w.Day,
            ["templates"] = templates,
            ["standing"] = Column(p => p.State == PostingState.Standing),
            ["taken"] = Column(p => p.State == PostingState.Taken),
            ["completed"] = Column(p => p.State is PostingState.Resolved or PostingState.Expired),
        });
    }

    private static JsonObject PostingCard(World w, Posting p) => new()
    {
        ["id"] = p.Id, ["requester"] = p.RequesterId, ["requester_name"] = Name(w, p.RequesterId),
        ["reach"] = p.Reach, ["site_name"] = SiteName(p.SiteId), ["reward"] = Math.Round(p.Reward, 3),
        ["state"] = p.State.ToString().ToLowerInvariant(),
        ["taker"] = p.TakerId, ["taker_name"] = p.TakerId is null ? null : Name(w, p.TakerId),
        ["filed_day"] = p.FiledDay, ["resolved_day"] = p.ResolvedDay, ["days_to_expiry"] = p.ExpiresDay - w.Day,
    };

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

    /// <summary>
    /// The board — the postings standing right now, in filing order (deterministic by construction,
    /// so it never needs re-sorting).
    /// <para><b>Current-state, not per-day, and that is the one design decision here.</b> Every other
    /// projection with a timeline takes a <c>day</c> parameter; this one must not. The board is a
    /// thing that is true *now* — "which paper hung on day 3" is a different question, answerable
    /// only by replaying filing and expiry dates, and nobody has asked it. Handing this a
    /// <c>view_day</c> would silently answer the unasked question with today's board.</para>
    /// <para><b><c>days_to_expiry</c> is derived here, against <c>w.Day</c>, not in GDScript</b> —
    /// the view has no business owning the boundary. <see cref="Board.ResolveDay"/> expires at
    /// <c>day &gt;= ExpiresDay</c> and runs at the dawn of the incoming day, so anything still on the
    /// board has <c>ExpiresDay &gt; Day</c> and the smallest value this can emit is <b>1</b> — its
    /// last day up. A 0 is unreachable: that paper came down before any projection could show it.</para>
    /// </summary>
    public static string BoardJson(World w)
    {
        var arr = new JsonArray();
        foreach (var p in w.Board)
            arr.Add(new JsonObject
            {
                ["id"] = p.Id,
                ["template"] = p.TemplateId,
                ["requester"] = p.RequesterId,
                ["requester_name"] = Name(w, p.RequesterId),
                ["reach"] = p.Reach,
                ["site"] = p.SiteId,
                ["site_name"] = SiteName(p.SiteId),
                ["tags"] = new JsonArray(p.Tags.Select(x => (JsonNode)x!).ToArray()),
                ["reward"] = Math.Round(p.Reward, 3),
                ["filed_day"] = p.FiledDay,
                ["expires_day"] = p.ExpiresDay,
                ["days_to_expiry"] = p.ExpiresDay - w.Day,
                // Null for every row at PNO.M1, and that is a decided value rather than missing data:
                // a posting WITH a taker is PostingState.Taken, and Taken is by definition not on the
                // board. The view says "untaken" rather than rendering a blank, for that reason.
                ["taker"] = p.TakerId,
                ["taker_name"] = p.TakerId is null ? null : Name(w, p.TakerId),
            });

        bool known = w.Town.PlaceById.TryGetValue(Board.BoardPlaceId, out var place);
        return S(new JsonObject
        {
            ["day"] = w.Day,
            ["place"] = Board.BoardPlaceId,
            ["place_name"] = known ? place!.Name : Board.BoardPlaceId,
            ["postings"] = arr,
        });
    }

    /// <summary>
    /// A site's display name. <b><c>sites.json</c> lands at <c>PNO.M2</c></b> — until then no site has
    /// an authored name anywhere in the town, so this de-slugifies the id (<c>the-sedge-fen</c> →
    /// <c>the Sedge Fen</c>, which is what the authored posting prose already calls it). When sites
    /// land, resolve from them and keep this as the fallback: the same <c>known ? name : derived</c>
    /// shape every other lookup in this file uses.
    /// <para>What it must never do is put the raw slug on screen. <see cref="Board"/>'s expiry entry
    /// refuses exactly that for <c>Posting.Id</c> on <c>AGT.10</c> grounds — the summary is gossip,
    /// not telemetry — and a site id is the same class of thing. Leading "the" stays lowercase to
    /// match the town's own convention: <c>places.json</c> authors "the Cartyard", not "The Cartyard".</para>
    /// </summary>
    private static string? SiteName(string? siteId)
    {
        if (string.IsNullOrWhiteSpace(siteId)) return null;
        var words = siteId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select((word, i) =>
            i == 0 && word == "the" ? word : char.ToUpperInvariant(word[0]) + word[1..]));
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

    /// <summary>
    /// The day's summary. <b>One time base:</b> `lines`, `dial` and `register` are all computed from
    /// the config as it is right now, so the label and the text below it can never disagree about
    /// which register is being shown. (They could, and did: `lines` came from a dawn-baked cache
    /// while `register` was derived live, so the strip announced "report" over gossip prose.)
    /// </summary>
    public static string SummaryJson(World w, int day)
    {
        var arr = new JsonArray();
        foreach (var l in Summarizer.Render(w, day))
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

    /// <summary>
    /// The stats strip — the on-screen `VFB.Q1` instrument.
    /// <para><b>`tellable` is the `VFB.Q1` number</b> and is defined to match the CLI soak's metric
    /// exactly (<c>Fishbowl.Cli/Program.cs</c>: distinct rendered text among the <i>delivered</i>
    /// lines), so the screen and the soak cannot report different answers to the same question.
    /// The strip used to show the distinct <i>candidate</i> StoryletIds instead — a pre-truncation
    /// pool bounded by the 12-rule bank rather than by `summary_lines`, so it read 12 on a night
    /// that told at most 5 lines, and no rendering knob could move it.</para>
    /// <para><b>`pool` keeps the old quantity</b>, honestly named, because the two numbers separate
    /// the two starvations and they need opposite knobs: a small pool is a <i>generation</i> problem
    /// (raise `storylet_rate`, loosen `hearsay_required`); a healthy pool with small `tellable` is a
    /// <i>truncation</i> problem (raise `summary_lines`).</para>
    /// </summary>
    public static string StatsJson(World w, int day)
    {
        var events = w.Chronicle.Where(e => e.Day == day).ToList();
        var byType = new JsonObject();
        foreach (var g in events.GroupBy(e => e.StoryletId).OrderBy(g => g.Key, StringComparer.Ordinal))
            byType[g.Key] = g.Count();

        var candidates = Summarizer.Candidates(w, day);
        int pool = candidates.Select(e => e.StoryletId).Distinct().Count();
        int tellable = Summarizer.Deliver(w, day, candidates).Select(l => l.Text).Distinct().Count();

        // An unstarted day is not a starved day. DayHashes is the exact "this day finalized"
        // witness (Simulation.FinalizeDay writes it), so a day-0 boot and every mid-day refresh
        // stop claiming starvation just because the chronicle has not been written yet.
        bool finalized = w.DayHashes.ContainsKey(day);

        return S(new JsonObject
        {
            ["day"] = day, ["events"] = events.Count,
            ["tellable"] = tellable, ["pool"] = pool,
            ["by_type"] = byType, ["starvation"] = finalized && tellable < 4,
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

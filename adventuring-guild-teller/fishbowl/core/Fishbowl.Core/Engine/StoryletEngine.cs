using System.Globalization;
using Fishbowl.Core.Model;
using Fishbowl.Core.Text;

namespace Fishbowl.Core.Engine;

/// <summary>
/// L3 Storylets (FBS.4): JSON-authored rules with predicates over co-presence + pressures
/// + regard + calendar. When one fires it mutates state and appends a chronicle entry that
/// records the predicate snapshot that let it fire — explainability by construction (AGR.2).
///
/// Binding: a storylet with an authored <c>_binding</c> is anchored to that cast (still gated
/// by every predicate each slot); one without binds by predicate search over co-present
/// townees. v0's bank is all anchored, which is what makes the golden day reproduce exactly
/// while leaving the search path open for emergent rules later.
///
/// Within a slot, all predicates are evaluated against the slot-start snapshot and every
/// firing's effects are applied afterward, so two rules keying on the same pressure both see
/// the pre-effect value (e.g. Petch's stock-runs-low and Sela's fetch-arranged on one slot).
/// </summary>
public static class StoryletEngine
{
    public static void RunSlot(World world, int slot, Action<ChronicleEntry>? onEvent)
    {
        var firings = new List<Firing>();

        foreach (var s in world.Town.Storylets
                     .OrderByDescending(x => x.Weight).ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            if (OnCooldown(world, s)) continue;
            if (!TryBind(world, s, slot, out var firing)) continue;
            if (!FireGate(world, s)) continue;
            firings.Add(firing);
        }

        foreach (var f in firings) Apply(world, f, slot, onEvent);
    }

    private sealed record Firing(StoryletDto Storylet, Dictionary<string, string> Bind, string Place, List<BecauseFact> Because);

    /// <summary>Debug force-fire (storylet browser / bridge InjectStorylet): bind the given
    /// participants to the copresent roles and apply effects immediately, bypassing predicates.</summary>
    public static ChronicleEntry? ForceFire(World world, string storyletId, IReadOnlyList<string> participants,
        int slot, Action<ChronicleEntry>? onEvent = null)
    {
        if (!world.Town.StoryletById.TryGetValue(storyletId, out var s)) return null;
        var roles = s.Predicates.Copresent;
        var bind = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < roles.Count; i++)
            bind[roles[i]] = i < participants.Count ? participants[i]
                : s.Binding?.GetValueOrDefault(roles[i]) ?? participants.FirstOrDefault() ?? "";

        string place = CommonPlace(world, slot, roles.Select(r => bind[r]));
        if (place is "" && roles.Count > 0)
        {
            var first = bind[roles[0]];
            place = PlacesOf(world, slot, first).FirstOrDefault()
                    ?? (world.TowneeById.TryGetValue(first, out var t) ? t.Home : "");
        }
        int before = world.Chronicle.Count;
        Apply(world, new Firing(s, bind, place, new List<BecauseFact> { new("forced", "debug force-fire") }), slot, onEvent);
        return world.Chronicle.Count > before ? world.Chronicle[^1] : null;
    }

    /// <summary>Is this rule still inside its cooldown today? The gate that runs before any predicate
    /// is read, so a rule on cooldown is not "gated by its predicate" — nobody asked it.
    /// <para>Public because <c>--lint</c> has to separate "did not fire because a predicate said no"
    /// from "did not fire because it was not eligible", and the answer depends on
    /// <c>storylet_cooldown_scale</c> and a ceiling — a linter that re-derived that arithmetic would
    /// be one refactor away from reporting a rule at its cap as a rule that died.</para></summary>
    public static bool OnCooldown(World world, StoryletDto s)
    {
        if (!world.Cooldowns.TryGetValue(s.Id, out int last)) return false;
        int scaledCooldown = (int)Math.Ceiling(s.Predicates.CooldownDays * world.Config.StoryletCooldownScale);
        return world.Day - last < scaledCooldown;
    }

    /// <summary>Does a drive value satisfy a pressure threshold? Absent bound matches everything, so
    /// <c>{}</c> is unconstrained — the same convention <see cref="PlaceMatches"/> follows.
    /// <para>Public for the same reason: <c>--lint</c> observes whether a predicate ever says no, and
    /// two definitions of "says no" is two things to keep in sync.</para></summary>
    public static bool PressureMatches(PressurePredicateDto pp, double value) =>
        (pp.Below is not double below || value < below) && (pp.Above is not double above || value > above);

    private static bool FireGate(World world, StoryletDto s)
    {
        double rate = world.Config.StoryletRate;
        if (s.MustFire || rate >= 1.0) return true;
        return world.RngFor(s.Streams).NextDouble() < rate; // thinning below 1×
    }

    // --- binding + predicate evaluation ------------------------------------------------

    private static bool TryBind(World world, StoryletDto s, int slot, out Firing firing)
    {
        firing = null!;
        var roles = s.Predicates.Copresent;
        foreach (var bind in CandidateBindings(world, s, slot, roles))
        {
            var because = new List<BecauseFact>();
            if (!CheckPredicates(world, s, slot, bind, because, out string place)) continue;
            firing = new Firing(s, bind, place, because);
            return true;
        }
        return false;
    }

    private static IEnumerable<Dictionary<string, string>> CandidateBindings(
        World world, StoryletDto s, int slot, IReadOnlyList<string> roles)
    {
        if (s.Binding is { Count: > 0 })
        {
            var b = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var role in roles)
                if (s.Binding.TryGetValue(role, out var id)) b[role] = id;
            if (b.Count == roles.Count) yield return b;
            yield break;
        }

        // Search path: co-present townees, in stable order, no repeats. (v0 handles 1–2 roles.)
        var present = world.Townees.Where(t => PlacesOf(world, slot, t.Id).Count > 0)
                                    .Select(t => t.Id).ToList();
        if (roles.Count == 1)
        {
            foreach (var a in present) yield return new(StringComparer.Ordinal) { [roles[0]] = a };
        }
        else if (roles.Count == 2)
        {
            foreach (var a in present)
                foreach (var b in present)
                    if (a != b) yield return new(StringComparer.Ordinal) { [roles[0]] = a, [roles[1]] = b };
        }
    }

    private static bool CheckPredicates(World world, StoryletDto s, int slot,
        Dictionary<string, string> bind, List<BecauseFact> because, out string place)
    {
        place = "";
        var pred = s.Predicates;

        // Co-presence: all bound roles share a common place this slot.
        //
        // The `place` predicate filters the *candidate set*, not the single ordinal-first pick: a
        // roaming courier can be co-present with someone at two places in one slot, and constraining
        // the winner after the fact would fail a rule that had a perfectly good place available.
        // Absent predicate => matches everything => first candidate => byte-identical to the
        // pre-predicate behaviour, which is what keeps the pinned golden hashes still.
        var candidates = CommonPlaces(world, slot, pred.Copresent.Select(r => bind[r]));
        place = candidates.FirstOrDefault(p => PlaceMatches(world, pred.Place, p)) ?? "";
        if (place is "") return false;

        // Awake gate: nobody quarrels or gossips in their sleep. Must-fire beats (a departure)
        // are exempt — they represent something happening regardless of the itinerary label.
        if (!s.MustFire && pred.Copresent.Any(r => IsAsleep(world, bind[r], slot))) return false;
        string placeName = world.Town.PlaceById.TryGetValue(place, out var pl) ? pl.Name : place;
        because.Add(new BecauseFact("co-present",
            $"{string.Join(", ", pred.Copresent.Select(r => Name(world, bind[r])))} at {placeName}, slot {slot}"));

        // Place: only when it actually constrained something, same as regard.tag above. A because-list
        // is the record of what made this fire, not a transcript of every check that passed trivially.
        if (pred.Place is { } pl2)
        {
            if (pl2.Any.Count > 0)
                because.Add(new BecauseFact("place", $"{placeName} is one of [{string.Join(", ", pl2.Any)}]"));
            if (pl2.Kind.Count > 0)
                because.Add(new BecauseFact("place", $"{placeName} is a {KindOf(world, place)}"));
        }

        // Posting: bind a standing posting off the board to a NON-townee role (`PNO.M2`). The board is the
        // standing subset in filing order, which is deterministic, so "the first that matches" is a stable
        // choice and the binder works down the board slot by slot as papers are taken. This role sits in
        // `bind` beside the townee roles but is NOT in `pred.Copresent`, so the co-presence intersection
        // above never saw it — the exclusion the drift check promised, for free.
        if (pred.Posting is { } postingPred)
        {
            var match = world.Board.FirstOrDefault(p =>
                (postingPred.State is null || string.Equals(p.State.ToString(), postingPred.State, StringComparison.OrdinalIgnoreCase))
                && postingPred.Tags.All(p.HasTag));
            if (match is null) return false;
            bind[postingPred.Role] = match.Id;
            because.Add(new BecauseFact("posting", $"{match.Id} standing on the board"));
        }

        // Phase: the bound townee must be in the required lifecycle phase (`PNO.M2`). "Only a daily-life
        // adventurer may take a posting" is `{"A": "daily"}`. Unlike the hardcoded Flag check below, an
        // unparseable phase name is a load-time error (SchemaValidator), not a silent false here.
        foreach (var (role, phaseName) in pred.Phase)
        {
            if (!Enum.TryParse<Phase>(phaseName, ignoreCase: true, out var want)) return false;
            if (world.TowneeById[bind[role]].Phase != want) return false;
            because.Add(new BecauseFact("phase", $"{Name(world, bind[role])} is {phaseName.ToLowerInvariant()}"));
        }

        // Adventurer: the villager/adventurer split (PNO.T1), read straight off the authored bit. A
        // villager at the board never takes a posting; that is the whole reason this predicate exists.
        foreach (var (role, want) in pred.Adventurer)
        {
            if (world.TowneeById[bind[role]].Adventurer != want) return false;
            because.Add(new BecauseFact("adventurer", $"{Name(world, bind[role])} {(want ? "is" : "is not")} an adventurer"));
        }

        // Regard tags / scores.
        foreach (var (key, rp) in pred.Regard)
        {
            var (r1, r2) = SplitEdge(key);
            string from = rp.Flip ? bind[r2] : bind[r1];
            string to = rp.Flip ? bind[r1] : bind[r2];
            var edge = world.TowneeById[from].Regard.GetValueOrDefault(to);
            if (rp.Tag is { Length: > 0 })
            {
                if (edge is null || !edge.HasTag(rp.Tag)) return false;
                because.Add(new BecauseFact("regard", $"{Name(world, from)}→{Name(world, to)} is '{rp.Tag}'"));
            }
            if (rp.ScoreBelow is double sb) { if (!(edge?.Score < sb)) return false; }
            if (rp.ScoreAbove is double sa) { if (!(edge?.Score > sa)) return false; }
        }

        // Pressure thresholds. The test itself is PressureMatches (shared with --lint); the
        // because-facts are this path's own business, and are only recorded once the whole predicate
        // has passed — a partially-filled list is discarded by TryBind anyway.
        foreach (var (key, pp) in pred.Pressure)
        {
            var (role, drive) = SplitDrive(key);
            double v = world.TowneeById[bind[role]].Pressure(drive);
            if (!PressureMatches(pp, v)) return false;
            if (pp.Below is double bl)
                because.Add(new BecauseFact("pressure", $"{Name(world, bind[role])} {drive} {v:0.00} < {bl:0.00}"));
            if (pp.Above is double ab)
                because.Add(new BecauseFact("pressure", $"{Name(world, bind[role])} {drive} {v:0.00} > {ab:0.00}"));
        }

        // Trait requirements.
        foreach (var (role, traitId) in pred.Trait)
        {
            if (!world.TowneeById[bind[role]].Traits.Contains(traitId)) return false;
            because.Add(new BecauseFact("trait", $"{Name(world, bind[role])} is {traitId}"));
        }

        // Sim flags (v0: departing_today only).
        foreach (var (key, want) in pred.Flag)
        {
            var (role, flag) = SplitDrive(key);
            bool actual = flag == "departing_today" && world.TowneeById[bind[role]].DepartingToday;
            if (actual != want) return false;
            because.Add(new BecauseFact("flag", $"{Name(world, bind[role])} {flag}={want.ToString().ToLowerInvariant()}"));
        }

        // Calendar: a recent chronicle entry of a kind (hearsay chains).
        if (pred.ChronicleSince is { } cs)
        {
            bool found = world.Chronicle.Any(e =>
                e.Day >= world.Day - cs.Days && (e.Day < world.Day || e.Slot < slot)
                && (cs.Kind is null || e.Kind == cs.Kind));
            if (!found) return false;
            because.Add(new BecauseFact("recent-event", $"a {cs.Kind ?? "recent"} beat within {cs.Days}d to retell"));
        }

        return true;
    }

    // --- effects -----------------------------------------------------------------------

    private static void Apply(World world, Firing f, int slot, Action<ChronicleEntry>? onEvent)
    {
        var s = f.Storylet;
        ChronicleEntry? entry = null;

        // Postings this firing touched, in effect order. Collected rather than applied straight to a
        // chronicle entry because the `post`/`take` and `chronicle` effects are separate entries and the
        // chain below does not guarantee which lands first. `filed` and `taken` are kept apart only so the
        // because-list reads true — both land in the same PostingIds field ("which paper").
        var filed = new List<string>();
        var taken = new List<string>();

        foreach (var e in s.Effects)
        {
            if (e.Regard is { Length: > 0 })
            {
                var (r1, r2) = SplitEdge(e.Regard);
                var from = world.TowneeById[f.Bind[r1]];
                string toId = f.Bind[r2];
                var edge = from.Regard.TryGetValue(toId, out var ex) ? ex : (from.Regard[toId] = new RegardEdge());
                edge.Score = Math.Clamp(edge.Score + e.Delta, -1.0, 1.0);
            }
            else if (e.Pressure is { Length: > 0 })
            {
                var (role, drive) = SplitDrive(e.Pressure);
                var t = world.TowneeById[f.Bind[role]];
                t.Pressures[drive] = Math.Clamp(t.Pressure(drive) + e.Delta, 0.0, 1.0);
            }
            else if (e.Post is { } post)
            {
                // The requester is a ROLE, resolved through the binding. SchemaValidator has already
                // proved the role is copresent and the template exists, so a null here means the
                // paper is a duplicate (same requester, same template, same day) — not an error.
                string requesterId = f.Bind.TryGetValue(post.Requester, out var rid) ? rid : "";
                var posting = Board.File(world, post.Template, requesterId, world.Day, slot);
                if (posting is not null) filed.Add(posting.Id);
            }
            else if (e.Take is { } take)
            {
                // Both are roles: the adventurer from copresent, the posting from the `posting` predicate
                // (bound into f.Bind during CheckPredicates). Outings.Take is the guard — it refuses a
                // non-daily taker, a gone posting, or an errand — so a null here is a race the board lost,
                // not an error. Same shape as `post` returning null on a duplicate.
                string takerId = f.Bind.TryGetValue(take.Adventurer, out var aid) ? aid : "";
                string postingId = f.Bind.TryGetValue(take.Posting, out var pid) ? pid : "";
                var outing = Outings.Take(world, takerId, postingId, world.Day);
                if (outing is not null) taken.Add(outing.PostingId);
            }
            else if (e.Chronicle)
            {
                entry = BuildEntry(world, f, slot, e);
            }
        }

        if (entry is not null)
        {
            // The paper reaches the chronicle. `Participants` stays townee-ids-only (WorldView
            // resolves every one of them to a name), so a posting rides its own field rather than
            // being smuggled into a list whose whole contract is "these are people".
            entry.PostingIds.AddRange(filed);
            entry.PostingIds.AddRange(taken);
            foreach (var id in filed) entry.Because.Add(new BecauseFact("filed", id));
            foreach (var id in taken) entry.Because.Add(new BecauseFact("taken", id));

            world.Chronicle.Add(entry);
            onEvent?.Invoke(entry);
            ApplyMarks(world, f, slot);
        }

        world.Cooldowns[s.Id] = world.Day; // fired today
    }

    private static ChronicleEntry BuildEntry(World world, Firing f, int slot, StoryletEffectDto chron)
    {
        var s = f.Storylet;
        var participants = s.Predicates.Copresent.Select(r => f.Bind[r]).ToList();
        var entry = new ChronicleEntry
        {
            Day = world.Day, Slot = slot, StoryletId = s.Id, Kind = s.Kind,
            PlaceId = f.Place,
            PlaceName = world.Town.PlaceById.TryGetValue(f.Place, out var pl) ? pl.Name : f.Place,
            Participants = participants, Tellability = chron.Tellability, Because = f.Because,
            Hearsay = LineRenderer.Render(s.Lines.Hearsay, world, f.Bind, f.Place, slot),
            Gossip = LineRenderer.Render(s.Lines.Gossip, world, f.Bind, f.Place, slot),
            Report = LineRenderer.Render(s.Lines.Report, world, f.Bind, f.Place, slot),
        };
        return entry;
    }

    private static void ApplyMarks(World world, Firing f, int slot)
    {
        if (!world.Config.BioMarksEnabled) return; // FB.8 toggle
        var chron = f.Storylet.Effects.FirstOrDefault(e => e.Chronicle);
        if (chron is null) return;
        foreach (var role in chron.Mark)
        {
            if (!f.Bind.TryGetValue(role, out var id)) continue;
            // Not every bound role is a townee: `post` (and, at PNO.M2, a `posting` predicate role)
            // put non-townee ids in the binding. `world.TowneeById[id]` would throw on one — an
            // indexer on a dictionary keyed by townee id, reached from a list of arbitrary role
            // names. Marks are a bio feature; a posting has no bio, so skipping is the whole fix.
            if (!world.TowneeById.TryGetValue(id, out var t)) continue;
            string line = LineRenderer.Render(f.Storylet.Lines.Gossip, world, f.Bind, f.Place, slot);
            t.Marks.Add(new MarkDto { Day = world.Day, Line = line });
        }
    }

    // --- helpers -----------------------------------------------------------------------

    private static string Name(World world, string id) =>
        world.TowneeById.TryGetValue(id, out var t) ? t.Name : id;

    private static bool IsAsleep(World world, string id, int slot) =>
        world.TowneeById.TryGetValue(id, out var t) && t.Asleep.Length > slot && t.Asleep[slot];

    /// <summary>Places (ordinal-sorted) where a townee is present this slot.</summary>
    private static List<string> PlacesOf(World world, int slot, string id)
    {
        var result = new List<string>();
        foreach (var (place, occ) in world.OccupantsAt(slot))
            if (occ.Contains(id)) result.Add(place);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    /// <summary>Every place (ordinal-sorted) shared by all given townees this slot. Usually one, but a
    /// roamer (the courier) can share two rooms with the same person in the same slot.</summary>
    private static List<string> CommonPlaces(World world, int slot, IEnumerable<string> ids)
    {
        HashSet<string>? common = null;
        foreach (var id in ids)
        {
            var here = PlacesOf(world, slot, id).ToHashSet(StringComparer.Ordinal);
            if (common is null) common = here; else common.IntersectWith(here);
            if (common.Count == 0) return new List<string>();
        }
        if (common is null) return new List<string>();
        return common.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    /// <summary>The ordinal-first place shared by all given townees this slot, or "" if none.</summary>
    private static string CommonPlace(World world, int slot, IEnumerable<string> ids) =>
        CommonPlaces(world, slot, ids).FirstOrDefault() ?? "";

    private static string KindOf(World world, string placeId) =>
        world.Town.PlaceById.TryGetValue(placeId, out var p) ? p.Kind : "";

    /// <summary>Does a place satisfy the (optional) place predicate? Null predicate matches
    /// everything — absent means unconstrained, which is what makes this addition hash-neutral.
    /// <para>Public because <c>--lint</c> enumerates where a beat could fire, and a rule constrained
    /// to one room must not be reported as able to fire in every room its cast passes through.</para></summary>
    public static bool PlaceMatches(World world, PlacePredicateDto? pred, string placeId)
    {
        if (pred is null) return true;
        if (pred.Any.Count > 0 && !pred.Any.Contains(placeId, StringComparer.Ordinal)) return false;
        if (pred.Kind.Count > 0 && !pred.Kind.Contains(KindOf(world, placeId), StringComparer.Ordinal)) return false;
        return true;
    }

    private static (string, string) SplitEdge(string key)
    {
        int i = key.IndexOf("->", StringComparison.Ordinal);
        return i < 0 ? (key, key) : (key[..i], key[(i + 2)..]);
    }

    private static (string, string) SplitDrive(string key)
    {
        int i = key.IndexOf('.');
        return i < 0 ? (key, "") : (key[..i], key[(i + 1)..]);
    }
}

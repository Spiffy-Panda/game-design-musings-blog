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

    private static bool OnCooldown(World world, StoryletDto s)
    {
        if (!world.Cooldowns.TryGetValue(s.Id, out int last)) return false;
        int scaledCooldown = (int)Math.Ceiling(s.Predicates.CooldownDays * world.Config.StoryletCooldownScale);
        return world.Day - last < scaledCooldown;
    }

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
        place = CommonPlace(world, slot, pred.Copresent.Select(r => bind[r]));
        if (place is "") return false;

        // Awake gate: nobody quarrels or gossips in their sleep. Must-fire beats (a departure)
        // are exempt — they represent something happening regardless of the itinerary label.
        if (!s.MustFire && pred.Copresent.Any(r => IsAsleep(world, bind[r], slot))) return false;
        string placeName = world.Town.PlaceById.TryGetValue(place, out var pl) ? pl.Name : place;
        because.Add(new BecauseFact("co-present",
            $"{string.Join(", ", pred.Copresent.Select(r => Name(world, bind[r])))} at {placeName}, slot {slot}"));

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

        // Pressure thresholds.
        foreach (var (key, pp) in pred.Pressure)
        {
            var (role, drive) = SplitDrive(key);
            double v = world.TowneeById[bind[role]].Pressure(drive);
            if (pp.Below is double bl) { if (!(v < bl)) return false;
                because.Add(new BecauseFact("pressure", $"{Name(world, bind[role])} {drive} {v:0.00} < {bl:0.00}")); }
            if (pp.Above is double ab) { if (!(v > ab)) return false;
                because.Add(new BecauseFact("pressure", $"{Name(world, bind[role])} {drive} {v:0.00} > {ab:0.00}")); }
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
            else if (e.Chronicle)
            {
                entry = BuildEntry(world, f, slot, e);
            }
        }

        if (entry is not null)
        {
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
            string line = LineRenderer.Render(f.Storylet.Lines.Gossip, world, f.Bind, f.Place, slot);
            world.TowneeById[id].Marks.Add(new MarkDto { Day = world.Day, Line = line });
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

    /// <summary>The ordinal-first place shared by all given townees this slot, or "" if none.</summary>
    private static string CommonPlace(World world, int slot, IEnumerable<string> ids)
    {
        HashSet<string>? common = null;
        foreach (var id in ids)
        {
            var here = PlacesOf(world, slot, id).ToHashSet(StringComparer.Ordinal);
            if (common is null) common = here; else common.IntersectWith(here);
            if (common.Count == 0) return "";
        }
        if (common is null || common.Count == 0) return "";
        return common.OrderBy(x => x, StringComparer.Ordinal).First();
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

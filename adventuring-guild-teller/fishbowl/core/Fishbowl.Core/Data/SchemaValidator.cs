using Fishbowl.Core.Model;

namespace Fishbowl.Core.Data;

/// <summary>
/// Validate-then-run discipline (appendix MUA.M8): range checks and ID-reference
/// integrity before the sim starts, failing loud with every problem at once. A dead or
/// dangling reference is poison — better to stop here than silently resolve to nothing mid-tick.
/// </summary>
public static class SchemaValidator
{
    /// <summary>Throw if the town has any validation error, listing all of them at once.</summary>
    public static void Validate(Town t)
    {
        var errors = Collect(t);
        if (errors.Count > 0)
            throw new InvalidDataException("Town data failed validation:\n  - " + string.Join("\n  - ", errors));
    }

    /// <summary>
    /// The same checks, returned rather than thrown. <c>--lint</c> needs this: a linter that can only
    /// read a town that already loads is useless exactly when an author most needs it — mid-rebuild,
    /// with the data half-swapped. Reporting a dangling reference as a finding beats a stack trace.
    /// </summary>
    public static List<string> Collect(Town t)
    {
        var errors = new List<string>();
        int slots = t.Config.SlotsPerDay;

        if (slots <= 0) errors.Add($"config.slots_per_day must be positive, got {slots}.");
        if (t.Config.SummaryLines is < 1 or > 20) errors.Add($"config.summary_lines out of range: {t.Config.SummaryLines}.");
        // World.SetKnob clamps the live dial, but an authored file bypasses it. The bound is not
        // cosmetic: Score raises this to the power of a telling count, so a negative value would
        // alternate the sign of a rule's score by parity and shuffle the bank rather than fatigue it.
        if (t.Config.NoveltyDecay is < 0.0 or > 1.0)
            errors.Add($"config.novelty_decay must be in [0,1], got {t.Config.NoveltyDecay}.");

        foreach (var p in t.Places)
        {
            if (p.Hours.Open < 0 || p.Hours.Open > slots || p.Hours.Close < 0 || p.Hours.Close > slots)
                errors.Add($"place '{p.Id}' hours {p.Hours.Open}..{p.Hours.Close} outside 0..{slots}.");
            if (p.Capacity < 0) errors.Add($"place '{p.Id}' negative capacity.");
        }

        foreach (var d in t.Config.PressureRates.Keys)
            if (!Town.Drives.Contains(d)) errors.Add($"config.pressure_rates has unknown drive '{d}'.");

        // Traits. Both maps are keyed by drive, and a key the engine does not know is a number nobody
        // will ever read — authored in good faith, silently inert, indistinguishable from a trait that
        // simply does nothing. Same reasoning as `pressure_rates` above; caught at load, not at a playtest.
        foreach (var tr in t.Traits)
        {
            foreach (var (drive, m) in tr.PressureRateMods)
            {
                if (!Town.Drives.Contains(drive))
                    errors.Add($"trait '{tr.Id}' pressure_rate_mods has unknown drive '{drive}' "
                             + $"(known: {string.Join(", ", Town.Drives)}).");
                if (m.Gain < 0 || m.Decay < 0)
                    errors.Add($"trait '{tr.Id}' pressure_rate_mods '{drive}' is negative "
                             + $"(gain {m.Gain}, decay {m.Decay}). A mod scales a drift's magnitude; a negative "
                             + "flips its direction, which would let a trait silently invert a rule the dayplan "
                             + "is supposed to own. To move where a drive settles, use pressure_targets.");
            }

            foreach (var (drive, target) in tr.PressureTargets)
            {
                if (!Town.Drives.Contains(drive))
                    errors.Add($"trait '{tr.Id}' pressure_targets has unknown drive '{drive}' "
                             + $"(known: {string.Join(", ", Town.Drives)}).");
                else if (!Town.TargetedDrives.Contains(drive))
                    errors.Add($"trait '{tr.Id}' pressure_targets sets '{drive}', but only "
                             + $"[{string.Join(", ", Town.TargetedDrives)}] read a target — Pressures.BaseDaily "
                             + $"would never look at this number. '{drive}' is not a restoring drive with a single "
                             + "rest point, so a target here is not a thing the engine can honour.");
                if (target is < 0.0 or > 1.0)
                    errors.Add($"trait '{tr.Id}' pressure_targets '{drive}' = {target} is outside 0..1 — drives are "
                             + "clamped to that range, so a target beyond it is a rest point the drive can never "
                             + "reach and would read as a permanent one-way pull.");
            }
        }

        foreach (var n in t.Townees)
        {
            if (!t.PlaceById.ContainsKey(n.Home)) errors.Add($"townee '{n.Id}' home '{n.Home}' is not a place.");
            if (n.Work is not null && !t.PlaceById.ContainsKey(n.Work)) errors.Add($"townee '{n.Id}' work '{n.Work}' is not a place.");
            foreach (var h in n.Haunts)
                if (!t.PlaceById.ContainsKey(h)) errors.Add($"townee '{n.Id}' haunt '{h}' is not a place.");
            if (!t.DayPlans.ContainsKey(n.Dayplan)) errors.Add($"townee '{n.Id}' dayplan '{n.Dayplan}' is not defined.");
            foreach (var tr in n.Traits)
                if (!t.TraitById.ContainsKey(tr)) errors.Add($"townee '{n.Id}' trait '{tr}' is not defined.");
            foreach (var dr in n.Pressures.Keys)
                if (!Town.Drives.Contains(dr)) errors.Add($"townee '{n.Id}' has unknown drive '{dr}'.");
            foreach (var (target, _) in n.Regard)
                if (!t.TowneeById.ContainsKey(target)) errors.Add($"townee '{n.Id}' regards unknown townee '{target}'.");
        }

        // Day-plan blocks resolve place tokens and stay in range.
        foreach (var (id, plan) in t.DayPlans)
        {
            foreach (var block in plan.Weekday.Concat(plan.Away ?? Enumerable.Empty<DayBlockDto>()))
            {
                if (block.Start < 0 || block.End > slots || block.Start >= block.End)
                    errors.Add($"dayplan '{id}' block {block.Start}..{block.End} malformed (0..{slots}).");
                if (block.Place.StartsWith("haunt:", StringComparison.Ordinal))
                {
                    var pid = block.Place["haunt:".Length..];
                    if (!t.PlaceById.ContainsKey(pid)) errors.Add($"dayplan '{id}' haunt '{pid}' is not a place.");
                }
                else if (block.Place is not ("work" or "home" or "away"))
                {
                    errors.Add($"dayplan '{id}' block place token '{block.Place}' unrecognized (work|home|away|haunt:<id>).");
                }
                foreach (var r in block.Roams ?? Enumerable.Empty<string>())
                    if (!t.PlaceById.ContainsKey(r)) errors.Add($"dayplan '{id}' roam '{r}' is not a place.");
            }
        }

        // The kind vocabulary is whatever this town's places actually use — not a hardcoded enum, so
        // a town may invent a kind without a code change, and a typo still fails because it matches
        // nothing the town has.
        var placeKinds = t.Places.Select(p => p.Kind).ToHashSet(StringComparer.Ordinal);

        foreach (var s in t.Storylets)
        {
            if (s.Lines.Hearsay.Length == 0 || s.Lines.Gossip.Length == 0 || s.Lines.Report.Length == 0)
                errors.Add($"storylet '{s.Id}' is missing one of hearsay/gossip/report lines.");
            if (s.Predicates.Copresent.Count == 0)
                errors.Add($"storylet '{s.Id}' has no copresent roles.");
            foreach (var (role, id) in s.Binding ?? new Dictionary<string, string>())
                if (!t.TowneeById.ContainsKey(id)) errors.Add($"storylet '{s.Id}' _binding {role}='{id}' is not a townee.");

            // The `place` predicate. A dangling place id or an unknown kind is a rule that can never
            // fire — silently, at runtime, forever. Catch it here (validate then run).
            if (s.Predicates.Place is { } pp)
            {
                foreach (var pid in pp.Any)
                    if (!t.PlaceById.ContainsKey(pid))
                        errors.Add($"storylet '{s.Id}' place.any '{pid}' is not a place.");
                foreach (var kind in pp.Kind)
                    if (!placeKinds.Contains(kind))
                        errors.Add($"storylet '{s.Id}' place.kind '{kind}' matches no place in this town "
                                 + $"(kinds present: {string.Join(", ", placeKinds.OrderBy(k => k, StringComparer.Ordinal))}).");
            }

            for (int i = 0; i < s.Effects.Count; i++)
            {
                var e = s.Effects[i];

                // The effect union (the contract stated on StoryletEffectDto). StoryletEngine.Apply
                // dispatches on an if/else-if chain, so an entry setting two members applies the first
                // in chain order and silently drops the rest — {"regard": ..., "chronicle": true} is a
                // beat that happens and is never told, with no error anywhere. The chain is not widened
                // into independent ifs on purpose: that would redefine this DTO as a bag-of-effects,
                // which ApplyMarks already contradicts (it takes the single Effects.FirstOrDefault(
                // e => e.Chronicle)). So the union is enforced here, at load, instead.
                //
                // Tested in the chain's own order, and with the chain's own conditions — so a member is
                // "set" here exactly when Apply would take that branch, and an explicit "chronicle":
                // false or "regard": "" is correctly not a member. delta/tellability/mark are
                // modifiers, not members: they legitimately ride along with the member they belong to.
                var set = new List<string>(4);
                if (e.Regard is { Length: > 0 }) set.Add("regard");
                if (e.Pressure is { Length: > 0 }) set.Add("pressure");
                if (e.Post is not null) set.Add("post");
                if (e.Chronicle) set.Add("chronicle");
                if (set.Count > 1)
                    errors.Add($"storylet '{s.Id}' effects[{i}] sets {set.Count} of regard/pressure/post/chronicle "
                             + $"({string.Join(" + ", set)}) — exactly one is allowed per effect entry. "
                             + $"StoryletEngine.Apply dispatches on an if/else-if chain, so this entry would apply "
                             + $"'{set[0]}' and silently drop {string.Join(" and ", set.Skip(1).Select(k => $"'{k}'"))}. "
                             + $"Split it into {set.Count} entries, one per key (delta/tellability/mark are modifiers "
                             + $"and stay with the key they belong to).");

                // The `post` effect. Same reasoning as `place` above: a template id that resolves to
                // nothing makes Board.File return null and the filing vanish without a word.
                if (e.Post is not { } post) continue;
                if (post.Template.Length == 0 || t.Postings.All(p => p.Id != post.Template))
                    errors.Add($"storylet '{s.Id}' post.template '{post.Template}' is not a posting template.");
                if (post.Requester.Length == 0 || !s.Predicates.Copresent.Contains(post.Requester))
                    errors.Add($"storylet '{s.Id}' post.requester '{post.Requester}' is not one of its copresent roles "
                             + $"[{string.Join(", ", s.Predicates.Copresent)}].");
            }
        }

        return errors;
    }
}

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
            // All FOUR block lists are walked now (`PNO.M2`) — Outing and Cooldown were unvalidated, and
            // because Clockwork.ResolvePlace has a silent catch-all (`_ => t.Home`), a typo'd token in a
            // cooldown block would send the townee home with no error, defeating validate-then-run.
            foreach (var block in plan.Weekday
                         .Concat(plan.Away ?? Enumerable.Empty<DayBlockDto>())
                         .Concat(plan.Outing ?? Enumerable.Empty<DayBlockDto>())
                         .Concat(plan.Cooldown ?? Enumerable.Empty<DayBlockDto>()))
            {
                if (block.Start < 0 || block.End > slots || block.Start >= block.End)
                    errors.Add($"dayplan '{id}' block {block.Start}..{block.End} malformed (0..{slots}).");
                if (block.Place.StartsWith("haunt:", StringComparison.Ordinal))
                {
                    var pid = block.Place["haunt:".Length..];
                    if (!t.PlaceById.ContainsKey(pid)) errors.Add($"dayplan '{id}' haunt '{pid}' is not a place.");
                }
                // `site` is the dynamic outing token — it resolves to the townee's runtime outing site, so
                // there is nothing to existence-check here (the site is not known until an outing starts).
                else if (block.Place is not ("work" or "home" or "away" or "site"))
                {
                    errors.Add($"dayplan '{id}' block place token '{block.Place}' unrecognized (work|home|away|site|haunt:<id>).");
                }
                foreach (var r in block.Roams ?? Enumerable.Empty<string>())
                    if (!t.PlaceById.ContainsKey(r)) errors.Add($"dayplan '{id}' roam '{r}' is not a place.");
            }
        }

        // Posting templates. `postings.json` is OPTIONAL — an absent file means a posting-free town,
        // which is exactly the frozen golden fixture (PNO.D2) — so this loop is a no-op there and
        // costs the fixture nothing. But every template that does exist is authored identity, and
        // `Board.File` is a copier: it stamps reach/site/tags/reward onto the runtime posting and
        // never looks back. Nothing downstream re-checks any of it. So a bad value here is not a
        // crash, it is a posting that exists and is quietly wrong for the rest of the run — the
        // failure mode this whole file was written to refuse.
        var reaches = new[] { "posting", "errand" };
        var seenPostings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in t.Postings)
        {
            // Board.File resolves a template with FirstOrDefault(p => p.Id == templateId), so a
            // duplicate id does not conflict — the first one silently wins and the second is
            // unreachable authored prose. Same shape as `unreachable-posting` in --lint, but that
            // is a warn about a template nothing names; this is two templates fighting over a name.
            if (!seenPostings.Add(p.Id))
                errors.Add($"posting '{p.Id}' is declared more than once — Board.File takes the first "
                         + "match by id, so the later one can never be filed.");

            if (!t.TowneeById.ContainsKey(p.Requester))
                errors.Add($"posting '{p.Id}' requester '{p.Requester}' is not a townee. Board.File "
                         + "copies it onto the posting unchecked, and both readers fall back to the raw "
                         + "id when the lookup misses — so the board would render paper filed by a slug, "
                         + "and the expiry line would name that slug as a person.");

            if (!reaches.Contains(p.Reach, StringComparer.Ordinal))
                errors.Add($"posting '{p.Id}' reach '{p.Reach}' unrecognized (posting|errand). No engine "
                         + "code branches on reach yet, so this does not throw anywhere — it is a word "
                         + "that reaches the board projection and decides how the card reads, and a typo "
                         + "would render a site posting as an in-town errand with no error.");

            // reach and site have to agree, in both directions, because each disagreement loses a
            // different thing silently. Existence is NOT checked: sites.json lands at PNO.M2, so a
            // site id has nothing to resolve against yet and shape is all that can honestly be
            // asserted here. (postings.json says so in its own header.)
            bool hasSite = !string.IsNullOrWhiteSpace(p.Site);
            if (p.Reach == "posting" && !hasSite)
                errors.Add($"posting '{p.Id}' has reach 'posting' but no site — a posting is the board, "
                         + "an adventurer, and a site, and the board projection has nothing to name as the "
                         + "destination. If it is handled in town by a neighbour, its reach is 'errand'.");
            if (p.Reach == "errand" && hasSite)
                errors.Add($"posting '{p.Id}' has reach 'errand' but names site '{p.Site}' — an errand is "
                         + "in-town by definition, so Board.File would carry a site that nothing will ever "
                         + "route to and the board would still read it as in-town.");

            // Site existence, now that sites exist (`PNO.M2`; at M1 this was shape-only, sites.json not yet
            // loaded). An outing against a posting whose site resolves to nothing has nowhere to walk.
            if (p.Reach == "posting" && hasSite && t.Sites.Count > 0 && t.SiteById(p.Site!) is null)
                errors.Add($"posting '{p.Id}' site '{p.Site}' is not a site in sites.json — an outing "
                         + "against it would have nowhere to go.");

            // Board.File does Math.Max(1, Round(ExpiresDays * PostingExpiryScale)), so a 0 or a
            // negative does not fail — it silently becomes 1 and the author's intent is gone. The
            // clamp is right (paper that expires before it is filed is not a state); catching the
            // input that needs clamping is this file's job.
            if (p.ExpiresDays < 1)
                errors.Add($"posting '{p.Id}' expires_days is {p.ExpiresDays} — Board.File clamps the span "
                         + "to a minimum of 1 day, so this would be silently rewritten rather than honoured.");

            // Not consumed until PNO.M3 pays it into a taker's purse — but it is already on screen:
            // the board projection emits it and the panel prints it as the terms of the job. A
            // negative reward reads as paper that charges you for taking it.
            if (p.Reward < 0)
                errors.Add($"posting '{p.Id}' reward is {p.Reward} — a reward is paid into the taker's purse "
                         + "(PNO.M3) and is rendered on the board as the job's terms; a negative one is a "
                         + "posting that bills the person who takes it.");
        }

        // Sites (`PNO.M2`). Optional like postings; empty in the frozen fixture, so this is a no-op there.
        // Each is also synthesized into a place at load, so co-presence works — but the leg TRACK is what
        // Outings walks, and a defect in it is a silent-wrong outing (no crash), the failure this file exists
        // to refuse.
        var siteIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var site in t.Sites)
        {
            if (!siteIds.Add(site.Id))
                errors.Add($"site '{site.Id}' is declared more than once.");
            if (site.Legs.Count == 0)
                errors.Add($"site '{site.Id}' has no legs — an outing there completes instantly, walking nothing "
                         + "and rolling no hazard, so the trip is a no-op that still burns a cooldown.");
            foreach (var leg in site.Legs)
            {
                if (leg.Slots < 1)
                    errors.Add($"site '{site.Id}' leg '{leg.Id}' slots is {leg.Slots} — Outings.StepSlot does "
                             + "Max(1, Round(slots × pace)), so a 0 or negative is silently rewritten to 1.");
                if (leg.Hazard is < 0.0 or > 1.0)
                    errors.Add($"site '{site.Id}' leg '{leg.Id}' hazard {leg.Hazard} is outside 0..1 — leg hazards "
                             + "sum (× outing_hazard_scale) into the rout probability rolled against the outings stream.");
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

            // Flags (`PNO.M2` trap #1). Only `departing_today` is wired — StoryletEngine.CheckPredicates
            // hardcodes it, so any other key evaluates to false and FAILS the predicate silently rather
            // than erroring. Lifecycle conditions go through `phase`, never a flag.
            foreach (var (key, _) in s.Predicates.Flag)
            {
                int dot = key.IndexOf('.');
                string role = dot < 0 ? key : key[..dot];
                string flag = dot < 0 ? "" : key[(dot + 1)..];
                if (flag != "departing_today")
                    errors.Add($"storylet '{s.Id}' flag '{key}' is unknown — only 'departing_today' is wired, and "
                             + "StoryletEngine evaluates any other flag to false, so the rule silently never fires. "
                             + "Use a `phase` predicate for outing/cooldown conditions.");
                else if (!s.Predicates.Copresent.Contains(role))
                    errors.Add($"storylet '{s.Id}' flag '{key}' role '{role}' is not one of its copresent roles.");
            }

            // Phase predicate (`PNO.M2`): a valid phase name on a copresent role.
            foreach (var (role, phaseName) in s.Predicates.Phase)
            {
                if (!Enum.TryParse<Engine.Phase>(phaseName, ignoreCase: true, out _))
                    errors.Add($"storylet '{s.Id}' phase '{role}: {phaseName}' is not a phase (daily|outing|cooldown).");
                if (!s.Predicates.Copresent.Contains(role))
                    errors.Add($"storylet '{s.Id}' phase role '{role}' is not one of its copresent roles.");
            }

            // Adventurer predicate (`PNO.M2`/`T1`): reads TowneeDto.Adventurer, so the only check is that
            // the role is a real bound townee role.
            foreach (var (role, _) in s.Predicates.Adventurer)
                if (!s.Predicates.Copresent.Contains(role))
                    errors.Add($"storylet '{s.Id}' adventurer role '{role}' is not one of its copresent roles.");

            // Posting predicate (`PNO.M2`): binds a NON-townee role, so it must NOT also be co-presence-bound
            // (the intersection would try to place a posting in a room).
            if (s.Predicates.Posting is { } postPred)
            {
                if (postPred.Role.Length == 0)
                    errors.Add($"storylet '{s.Id}' posting predicate has an empty role.");
                else if (s.Predicates.Copresent.Contains(postPred.Role))
                    errors.Add($"storylet '{s.Id}' posting role '{postPred.Role}' also appears in copresent — a posting "
                             + "is not a townee and must not be co-presence-bound.");
                if (postPred.State is { Length: > 0 } pst && !Enum.TryParse<Engine.PostingState>(pst, ignoreCase: true, out _))
                    errors.Add($"storylet '{s.Id}' posting state '{pst}' is not a posting state.");
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
                var set = new List<string>(5);
                if (e.Regard is { Length: > 0 }) set.Add("regard");
                if (e.Pressure is { Length: > 0 }) set.Add("pressure");
                if (e.Post is not null) set.Add("post");
                if (e.Take is not null) set.Add("take");
                if (e.Chronicle) set.Add("chronicle");
                if (set.Count > 1)
                    errors.Add($"storylet '{s.Id}' effects[{i}] sets {set.Count} of regard/pressure/post/take/chronicle "
                             + $"({string.Join(" + ", set)}) — exactly one is allowed per effect entry. "
                             + $"StoryletEngine.Apply dispatches on an if/else-if chain, so this entry would apply "
                             + $"'{set[0]}' and silently drop {string.Join(" and ", set.Skip(1).Select(k => $"'{k}'"))}. "
                             + $"Split it into {set.Count} entries, one per key (delta/tellability/mark are modifiers "
                             + $"and stay with the key they belong to).");

                // The `take` effect (`PNO.M2`): adventurer is a copresent role; posting is THIS storylet's
                // posting-predicate role — there is nothing else on the board to take. A take with no
                // posting predicate to bind its role can never resolve its own effect.
                if (e.Take is { } take)
                {
                    if (take.Adventurer.Length == 0 || !s.Predicates.Copresent.Contains(take.Adventurer))
                        errors.Add($"storylet '{s.Id}' take.adventurer '{take.Adventurer}' is not one of its copresent roles "
                                 + $"[{string.Join(", ", s.Predicates.Copresent)}].");
                    if (s.Predicates.Posting is not { } pr || pr.Role != take.Posting)
                        errors.Add($"storylet '{s.Id}' take.posting '{take.Posting}' must be the role of this storylet's "
                                 + "`posting` predicate — that is the paper being taken, and there is nothing else to bind it to.");
                    continue;
                }

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

using Fishbowl.Core.Model;

namespace Fishbowl.Core.Data;

/// <summary>
/// Validate-then-run discipline (appendix MUA.M8): range checks and ID-reference
/// integrity before the sim starts, failing loud with every problem at once. A dead or
/// dangling reference is poison — better to stop here than silently resolve to nothing mid-tick.
/// </summary>
public static class SchemaValidator
{
    public static void Validate(Town t)
    {
        var errors = new List<string>();
        int slots = t.Config.SlotsPerDay;

        if (slots <= 0) errors.Add($"config.slots_per_day must be positive, got {slots}.");
        if (t.Config.SummaryLines is < 1 or > 20) errors.Add($"config.summary_lines out of range: {t.Config.SummaryLines}.");

        foreach (var p in t.Places)
        {
            if (p.Hours.Open < 0 || p.Hours.Open > slots || p.Hours.Close < 0 || p.Hours.Close > slots)
                errors.Add($"place '{p.Id}' hours {p.Hours.Open}..{p.Hours.Close} outside 0..{slots}.");
            if (p.Capacity < 0) errors.Add($"place '{p.Id}' negative capacity.");
        }

        foreach (var d in t.Config.PressureRates.Keys)
            if (!Town.Drives.Contains(d)) errors.Add($"config.pressure_rates has unknown drive '{d}'.");

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

        foreach (var s in t.Storylets)
        {
            if (s.Lines.Hearsay.Length == 0 || s.Lines.Gossip.Length == 0 || s.Lines.Report.Length == 0)
                errors.Add($"storylet '{s.Id}' is missing one of hearsay/gossip/report lines.");
            if (s.Predicates.Copresent.Count == 0)
                errors.Add($"storylet '{s.Id}' has no copresent roles.");
            foreach (var (role, id) in s.Binding ?? new Dictionary<string, string>())
                if (!t.TowneeById.ContainsKey(id)) errors.Add($"storylet '{s.Id}' _binding {role}='{id}' is not a townee.");
        }

        if (errors.Count > 0)
            throw new InvalidDataException("Town data failed validation:\n  - " + string.Join("\n  - ", errors));
    }
}

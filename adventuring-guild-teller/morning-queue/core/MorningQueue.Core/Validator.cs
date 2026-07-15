using System.Text.Json;

namespace MorningQueue.Core;

/// <summary>
/// The desk's schema sanity pass, ported 1:1 from DeckLoader.gd's `_validate_banks`,
/// `_validate_standing_orders`, `_validate_shift` and `_validate_inspections`. Same
/// strictness, same human-readable error strings — this is the ONLY home for these checks
/// now. No new rules were added; any rule that moved (rather than changed) is noted in the
/// handoff report, not here.
/// </summary>
public static class Validator
{
    // The enums the banks are checked against — the extended task/axis/pool vocabularies.
    public static readonly string[] TaskTypes =
        { "item_check", "rank_gate", "quest_file", "completion_claim", "rank_up", "roster_change", "dungeon_drop" };

    public static readonly string[] FailureAxes =
        { "identity", "rank", "unverifiable", "claimant", "authenticity", "paperwork",
          "duplicate", "fieldability", "season", "reach", "dues", "amount" };

    public static readonly string[] ActorPools =
        { "townee_walkin", "townee_owner", "townee_directory", "adventurer_directory", "mixed" };

    private static readonly string[] DuesEnum = { "current", "owing" };

    /// <summary>
    /// Validate the static banks (directories + generation config + standing-order limits)
    /// against the rulebook they cross-reference. Returns the list of problems, empty when
    /// sane. Mirrors `_validate_banks` + `_validate_standing_orders`.
    /// </summary>
    public static List<string> ValidateBanks(MorningQueueData data)
    {
        var errors = new List<string>();
        var refs = data.References;

        // --- townees ---
        foreach (var (tid, t) in data.Townees)
        {
            if (!DuesEnum.Contains(t.Dues))
                errors.Add($"townee '{tid}' dues not current|owing");
            // GDScript: t.get("owed", 0) defaults to a number; only a present, non-number
            // value fails. An absent `owed` is fine.
            if (t.Owed.HasValue && t.Owed.Value.ValueKind != JsonValueKind.Number)
                errors.Add($"townee '{tid}' owed is not a number");
            foreach (var oid in t.Owns ?? Array.Empty<string>())
                if (!refs.Postings.ContainsKey(oid))
                    errors.Add($"townee '{tid}' owns unknown posting '{oid}'");
        }

        // --- adventurers ---
        foreach (var (aid, a) in data.Adventurers)
        {
            if (!refs.RankOrder.Contains(a.Rank ?? ""))
                errors.Add($"adventurer '{aid}' rank '{a.Rank}' not in rank_order");
            if (!DuesEnum.Contains(a.Dues))
                errors.Add($"adventurer '{aid}' dues not current|owing");
            if (!refs.CipherTable.ContainsKey(a.Chapter ?? ""))
                errors.Add($"adventurer '{aid}' chapter '{a.Chapter}' not in cipher_table");
            if (a.Logbook is not null && !refs.Archive.ContainsKey(a.Logbook.ArchiveId ?? ""))
                errors.Add($"adventurer '{aid}' logbook archive_id not in archive");
        }

        // --- generation knobs (skipped entirely when generation is absent/empty) ---
        var gen = data.Generation;
        if (gen is not null && !IsEmpty(gen))
        {
            foreach (var tk in Keys(gen.TaskWeights))
                if (!TaskTypes.Contains(tk))
                    errors.Add($"generation task_weights has unknown task '{tk}'");

            foreach (var (tk, spec) in gen.PerTask ?? new())
            {
                if (!ActorPools.Contains(spec.ActorPool))
                    errors.Add($"generation per_task['{tk}'] actor_pool invalid");
                foreach (var ax in spec.FailureAxes ?? Array.Empty<string>())
                    if (!FailureAxes.Contains(ax))
                        errors.Add($"generation per_task['{tk}'] axis '{ax}' not in enum");
            }

            var wheel = gen.SeasonSchedule?.Wheel ?? Array.Empty<string>();
            foreach (var (dk, sv) in gen.SeasonSchedule?.ByDay ?? new())
            {
                if (dk.StartsWith('_')) continue;
                var s = sv.ValueKind == JsonValueKind.String ? sv.GetString() ?? "" : sv.ToString();
                if (!wheel.Contains(s))
                    errors.Add($"generation season_schedule.by_day['{dk}'] = '{s}' not in wheel");
            }

            CheckRate(errors, gen.InvalidRate, "invalid_rate");
            foreach (var (dk, rv) in gen.InvalidRateByDay ?? new())
            {
                if (dk.StartsWith('_')) continue;
                CheckRate(errors, rv, $"invalid_rate_by_day['{dk}']");
            }
        }

        // --- standing-order limits (was _validate_standing_orders; a references check) ---
        foreach (var (pid, p) in refs.Postings)
            if (p.Type == "standing_order" && !p.HasLimit)
                errors.Add($"standing_order '{pid}' has no accept/total limit");

        return errors;
    }

    /// <summary>
    /// Validate a loaded shift: every visitor carries the fields the scenes rely on, and
    /// both inspection-tool readings are present and non-empty. Mirrors `_validate_shift`
    /// + `_validate_inspections`.
    /// </summary>
    public static List<string> ValidateShift(IEnumerable<Visit> visits)
    {
        var errors = new List<string>();
        foreach (var v in visits)
        {
            var id = v.Id ?? "?";
            if (v.Id is null) errors.Add($"visitor '{id}' missing field 'id'");
            if (v.Name is null) errors.Add($"visitor '{id}' missing field 'name'");
            if (v.Affiliation is null) errors.Add($"visitor '{id}' missing field 'affiliation'");
            if (v.Profession is null) errors.Add($"visitor '{id}' missing field 'profession'");
            if (v.TaskType is null) errors.Add($"visitor '{id}' missing field 'task_type'");
            if (v.Claim is null) errors.Add($"visitor '{id}' missing field 'claim'");
            if (v.Truth is null) errors.Add($"visitor '{id}' missing field 'truth'");
            ValidateInspections(errors, v);
        }
        return errors;
    }

    private static void ValidateInspections(List<string> errors, Visit v)
    {
        var id = v.Id ?? "?";
        if (v.Inspections is null)
        {
            errors.Add($"visitor '{id}' missing 'inspections' object");
            return;
        }
        // glass
        if (v.Inspections.Glass is null)
            errors.Add($"visitor '{id}' inspections missing 'glass'");
        else if (string.IsNullOrEmpty(v.Inspections.Glass.Reading))
            errors.Add($"visitor '{id}' glass reading is empty");
        // scale
        if (v.Inspections.Scale is null)
            errors.Add($"visitor '{id}' inspections missing 'scale'");
        else if (string.IsNullOrEmpty(v.Inspections.Scale.Reading))
            errors.Add($"visitor '{id}' scale reading is empty");
    }

    private static void CheckRate(List<string> errors, JsonElement? value, string label)
    {
        var v = Json.AsNumber(value);
        if (v is null || v < 0.0 || v > 1.0)
            errors.Add($"generation {label} not in [0,1]");
    }

    private static IEnumerable<string> Keys(Dictionary<string, JsonElement>? map)
    {
        foreach (var k in map?.Keys ?? Enumerable.Empty<string>())
            if (!k.StartsWith('_'))
                yield return k;
    }

    private static bool IsEmpty(GenerationConfig gen)
        => gen.TaskWeights is null && gen.PerTask is null && gen.SeasonSchedule is null
           && gen.InvalidRate is null && gen.InvalidRateByDay is null;
}

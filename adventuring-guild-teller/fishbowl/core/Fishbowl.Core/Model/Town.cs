namespace Fishbowl.Core.Model;

/// <summary>
/// The loaded, validated static town definition — the immutable input to a sim run.
/// Everything here comes straight from data/; nothing mutates during ticking (the mutable
/// per-townee state lives in the engine's World).
/// </summary>
public sealed class Town
{
    public required SimConfig Config { get; init; }
    public required IReadOnlyList<PlaceDto> Places { get; init; }
    public required IReadOnlyList<TowneeDto> Townees { get; init; }
    public required IReadOnlyDictionary<string, DayPlanDto> DayPlans { get; init; }
    public required IReadOnlyList<TraitDto> Traits { get; init; }
    public required IReadOnlyList<StoryletDto> Storylets { get; init; }
    public GoldenDayFile? Golden { get; init; }

    /// <summary>Authored posting templates (`data/postings.json`) — the seed bank the `post` effect
    /// files from. <b>Defaulted, not `required`, and that is deliberate:</b> a town with no postings
    /// is a legal town — it is exactly what the frozen golden fixture is (`PNO.D2`) — so an empty
    /// bank must be the natural state rather than a stub every caller has to remember to pass.</summary>
    public IReadOnlyList<PostingTemplateDto> Postings { get; init; } = Array.Empty<PostingTemplateDto>();

    // Lookups (built once). Keys are stable ids.
    public required IReadOnlyDictionary<string, PlaceDto> PlaceById { get; init; }
    public required IReadOnlyDictionary<string, TowneeDto> TowneeById { get; init; }
    public required IReadOnlyDictionary<string, TraitDto> TraitById { get; init; }
    public required IReadOnlyDictionary<string, StoryletDto> StoryletById { get; init; }

    /// <summary>The four L2 drives, in canonical (stable) order.</summary>
    public static readonly IReadOnlyList<string> Drives = new[] { "purse", "trade", "heart", "restlessness" };

    /// <summary>The drives a trait may aim with <c>pressure_targets</c> — i.e. the restoring drives whose
    /// rest point is a single authorable number.
    /// <para><b>`trade` is deliberately absent, though it is now restoring too.</b> Its rest point is not one
    /// number: <see cref="Engine.Pressures.BaseDaily"/> pulls it toward a <i>per-mode</i> target, so "where
    /// does trade rest" is answered by the townee's dayplan (the work/idle slot split), not by a scalar. A
    /// single <c>pressure_targets.trade</c> could not say which of the two it meant, so it is refused at load
    /// rather than resolved by a guess. Widening this needs a ruling on that question first.</para></summary>
    public static readonly IReadOnlyList<string> TargetedDrives = new[] { "heart" };

    /// <summary>A copy of this town with an added/replaced townee (creation menus' hot-add).
    /// Re-validates, so a bad reference throws before it reaches the sim.</summary>
    public Town WithTownee(TowneeDto townee)
    {
        var list = Townees.Where(t => t.Id != townee.Id).Append(townee)
                          .OrderBy(t => t.Id, StringComparer.Ordinal).ToList();
        return Data.TownLoader.Rebuild(this, townees: list);
    }

    /// <summary>A copy of this town with an added/replaced place.</summary>
    public Town WithPlace(PlaceDto place)
    {
        var list = Places.Where(p => p.Id != place.Id).Append(place)
                         .OrderBy(p => p.Id, StringComparer.Ordinal).ToList();
        return Data.TownLoader.Rebuild(this, places: list);
    }
}

namespace Fishbowl.Core.Model;

/// <summary>
/// An adventure <b>site</b> (`PNO.D7`): a fen, a mountain, a cellar. "All areas are dungeons" reads as
/// <i>dungeon is the game's word for an adventure site</i> — and a site is <b>not</b> a floor stack. It is
/// an ordered track of <see cref="Legs"/> (approach → search → the-thing → return), each a slot duration
/// and a hazard weight. A field has legs; a mountain has legs; a cellar has legs, and no more of them.
/// <para><b>A site is also a place.</b> At load a <see cref="PlaceDto"/> is synthesized for each site
/// (<c>board:false</c>, <c>offscreen:true</c>), so the party is co-present there and every existing
/// storylet mechanism — predicates, regard, the chronicle, because-lists — works off-screen with no new
/// code. `sites.json` is the single authoring surface; the place falls out of it.</para>
/// <para><b>Optional file</b>, exactly like `postings.json` (`PNO.D2`): absent means a site-free town,
/// which the frozen golden fixture is forever.</para>
/// </summary>
public sealed record SiteDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";

    /// <summary>A tag, not a structure (`PNO.D7`) — <c>fen</c>, <c>mountain</c>, <c>cellar</c>. Becomes the
    /// synthesized place's <see cref="PlaceDto.Kind"/>, so a storylet can predicate <c>place.kind</c> on it.</summary>
    public string Kind { get; init; } = "fen";

    /// <summary>Travel each way in slots — a floor on how long the trip runs regardless of the legs. Flavour
    /// and a length-floor at `PNO.M2`; the legs carry the real duration.</summary>
    public int DistanceSlots { get; init; }

    public List<LegDto> Legs { get; init; } = new();
}

/// <summary>One leg of a site's track: a stretch of the trip with a duration and a hazard weight.</summary>
public sealed record LegDto
{
    public string Id { get; init; } = "";

    /// <summary>Slots this leg takes, before <c>outing_pace_scale</c>. The sum across legs is the trip's
    /// base length.</summary>
    public int Slots { get; init; } = 1;

    /// <summary>Chance-weight this leg contributes to a rout, before <c>outing_hazard_scale</c>. Accumulated
    /// across the legs actually walked and rolled once against the <c>outings</c> sub-stream when the track
    /// completes — so a longer, nastier track is more dangerous, leg by leg.</summary>
    public double Hazard { get; init; }

    /// <summary>The activity label shown while the party is on this leg ("wading in", "casting about the
    /// reeds") — the site's equivalent of a day-plan block's activity.</summary>
    public string Activity { get; init; } = "";
}

/// <summary>`data/sites.json`. <b>Optional</b>, like `postings.json` — absent ⇒ a site-free town.</summary>
public sealed record SitesFile
{
    public int Version { get; init; } = 1;
    public List<SiteDto> Sites { get; init; } = new();
}

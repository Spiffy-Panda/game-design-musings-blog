using System.Text.Json.Serialization;

namespace Fishbowl.Core.Model;

/// <summary>One JSON-authored storylet rule (PLAN "Data", storylets/*.json).</summary>
public sealed record StoryletDto
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public StoryletPredicatesDto Predicates { get; init; } = new();
    public double Weight { get; init; } = 1.0;
    /// <summary>When true, the thinning gate (storylet_rate &lt; 1) never drops this event —
    /// the deterministic must-fire override (appendix MUA.J9).</summary>
    public bool MustFire { get; init; }
    public string Streams { get; init; } = "storylets";
    public List<StoryletEffectDto> Effects { get; init; } = new();
    public StoryletLinesDto Lines { get; init; } = new();

    /// <summary>Golden-day fixture hint: the intended role→townee binding. Used by the
    /// golden test and as a debug default for force-fire; the live engine still binds by
    /// predicate search (see StoryletEngine).</summary>
    [JsonPropertyName("_binding")]
    public Dictionary<string, string>? Binding { get; init; }
}

public sealed record StoryletPredicatesDto
{
    /// <summary>Bound role names that must share a place this slot (e.g. ["A","B"] or ["A"]).</summary>
    public List<string> Copresent { get; init; } = new();

    /// <summary>Keyed "A-&gt;B": directed-regard conditions.</summary>
    public Dictionary<string, RegardPredicateDto> Regard { get; init; } = new();

    /// <summary>Keyed "B.purse": pressure thresholds.</summary>
    public Dictionary<string, PressurePredicateDto> Pressure { get; init; } = new();

    /// <summary>Keyed role → required trait id.</summary>
    public Dictionary<string, string> Trait { get; init; } = new();

    /// <summary>Keyed "A.departing_today": boolean sim flags.</summary>
    public Dictionary<string, bool> Flag { get; init; } = new();

    public ChronicleSinceDto? ChronicleSince { get; init; }

    public int CooldownDays { get; init; }
}

public sealed record RegardPredicateDto
{
    public string? Tag { get; init; }
    /// <summary>When true, the tag must be on the <i>reverse</i> edge (B→A) — "B owes A".</summary>
    public bool Flip { get; init; }
    public double? ScoreBelow { get; init; }
    public double? ScoreAbove { get; init; }
}

public sealed record PressurePredicateDto
{
    public double? Below { get; init; }
    public double? Above { get; init; }
}

public sealed record ChronicleSinceDto
{
    public int Days { get; init; }
    public string? Kind { get; init; }
}

/// <summary>One effect. Exactly one of Regard / Pressure / Chronicle is meaningful per entry.</summary>
public sealed record StoryletEffectDto
{
    public string? Regard { get; init; }    // "A->B"
    public string? Pressure { get; init; }  // "B.heart"
    public double Delta { get; init; }
    public bool Chronicle { get; init; }
    public double Tellability { get; init; }
    /// <summary>Roles whose bios get a dated one-liner appended (FB.8, behind the toggle).</summary>
    public List<string> Mark { get; init; } = new();
}

public sealed record StoryletLinesDto
{
    public string Hearsay { get; init; } = "";
    public string Gossip { get; init; } = "";
    public string Report { get; init; } = "";
}

public sealed record GoldenDayFile
{
    public int Version { get; init; } = 1;
    public long Seed { get; init; }
    public int Day { get; init; }
    public List<GoldenBeat> ExpectedBeats { get; init; } = new();
}

public sealed record GoldenBeat
{
    public string Storylet { get; init; } = "";
    public List<string> Participants { get; init; } = new();
}

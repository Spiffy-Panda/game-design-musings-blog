using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>Runtime townee: authored identity (set once) + mutable sim state.</summary>
public sealed class Townee
{
    // Authored identity — never mutated during a run.
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public bool Adventurer { get; init; }
    public required IReadOnlyList<string> Traits { get; init; }
    public required string DayplanId { get; init; }
    public required string Home { get; init; }
    public string? Work { get; init; }
    public int? DepartsDay { get; init; }
    public required IReadOnlyList<string> Haunts { get; init; }
    public required string Bio { get; init; }

    // Mutable L2 state.
    public Dictionary<string, double> Pressures { get; } = new();
    public Dictionary<string, RegardEdge> Regard { get; } = new(); // target id -> directed edge
    public List<MarkDto> Marks { get; } = new();

    /// <summary>Adventurer away-flag knob — the expedition system's stand-in (AGT.11).</summary>
    public bool Away { get; set; }

    // Resolved once per day by the clockwork (length = slots_per_day).
    public string[] Itinerary { get; set; } = Array.Empty<string>();   // place id (or "away") per slot
    public string[] Activity { get; set; } = Array.Empty<string>();    // readout label per slot
    public string[] Mode { get; set; } = Array.Empty<string>();        // work|home|haunt|away per slot (drift context)
    public bool[] Asleep { get; set; } = Array.Empty<bool>();          // true where the townee is asleep (storylet awake-gate)

    /// <summary>Set on the day an adventurer is scheduled to leave (departure-farewell predicate).</summary>
    public bool DepartingToday { get; set; }

    public double Pressure(string drive) => Pressures.TryGetValue(drive, out var v) ? v : 0.0;
}

/// <summary>A directed regard edge (suggestee→target dyad): score + tags (AGT.8).</summary>
public sealed class RegardEdge
{
    public double Score { get; set; }
    public List<string> Tags { get; } = new();
    public bool HasTag(string tag) => Tags.Contains(tag);
}

/// <summary>One recorded event with the predicate snapshot that let it fire (AGR.2).</summary>
public sealed class ChronicleEntry
{
    public required int Day { get; init; }
    public required int Slot { get; init; }
    public required string StoryletId { get; init; }
    public required string Kind { get; init; }
    public required string PlaceId { get; init; }
    public required string PlaceName { get; init; }
    public required List<string> Participants { get; init; }   // townee ids, bound order
    public double Tellability { get; init; }

    /// <summary>The because-list: the predicate facts, human-readable (AGR.2 explainability).</summary>
    public List<BecauseFact> Because { get; init; } = new();

    /// <summary>The three actionability variants, pre-rendered (PLAN summarizer dial).</summary>
    public string Hearsay { get; set; } = "";
    public string Gossip { get; set; } = "";
    public string Report { get; set; } = "";

    /// <summary>Did a gossip-carrier witness or later share a room with a witness? (hearsay-lite gate).</summary>
    public bool CarriedByGossip { get; set; }
}

public sealed record BecauseFact(string Label, string Value);

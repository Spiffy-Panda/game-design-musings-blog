using System.Text.Json.Serialization;

namespace Fishbowl.Core.Model;

// Parse targets for the authored data/ files. Field lists are the contract (PLAN "Data");
// snake_case ↔ PascalCase handled by the shared naming policy in DataJson.

public sealed record TowneesFile
{
    public int Version { get; init; } = 1;
    public List<TowneeDto> Townees { get; init; } = new();
}

public sealed record TowneeDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public bool Adventurer { get; init; }
    public List<string> Traits { get; init; } = new();
    public string Dayplan { get; init; } = "";
    public string Home { get; init; } = "";
    public string? Work { get; init; }
    /// <summary>Adventurer only: the day this townee leaves on expedition (sets departing_today
    /// that day, then Away after). Null = stays. The away-flag knob overrides this at runtime.</summary>
    public int? DepartsDay { get; init; }
    public List<string> Haunts { get; init; } = new();
    public Dictionary<string, double> Pressures { get; init; } = new();
    public Dictionary<string, RegardDto> Regard { get; init; } = new();
    public double TellerRegard { get; init; } = 0.5;
    public string Bio { get; init; } = "";
    public List<MarkDto> Marks { get; init; } = new();
}

public sealed record RegardDto
{
    public double Score { get; init; }
    public List<string> Tags { get; init; } = new();
}

public sealed record MarkDto
{
    public int Day { get; init; }
    public string Line { get; init; } = "";
}

public sealed record PlacesFile
{
    public int Version { get; init; } = 1;
    public List<PlaceDto> Places { get; init; } = new();
}

public sealed record PlaceDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public HoursDto Hours { get; init; } = new();
    public int Capacity { get; init; }
    public bool Board { get; init; }
    public bool Shut { get; init; }
}

public sealed record HoursDto
{
    public int Open { get; init; }
    public int Close { get; init; }
}

public sealed record DayPlansFile
{
    public int Version { get; init; } = 1;
    public Dictionary<string, DayPlanDto> Dayplans { get; init; } = new();
}

public sealed record DayPlanDto
{
    public List<DayBlockDto> Weekday { get; init; } = new();
    public List<DayBlockDto>? Away { get; init; }
}

public sealed record DayBlockDto
{
    public int Start { get; init; }
    public int End { get; init; }
    public string Place { get; init; } = "";
    public string Activity { get; init; } = "";
    /// <summary>Optional roam set (courier): the townee is co-present with all listed
    /// places' occupants during this block. First entry is the anchor place shown in readouts.</summary>
    public List<string>? Roams { get; init; }
}

public sealed record TraitsFile
{
    public int Version { get; init; } = 1;
    public List<TraitDto> Traits { get; init; } = new();
}

public sealed record TraitDto
{
    public string Id { get; init; } = "";
    public Dictionary<string, double> PressureRateMods { get; init; } = new();
    public Dictionary<string, double> StoryletWeightMods { get; init; } = new();
    public bool HearsayCarrier { get; init; }
}

public sealed record SimConfig
{
    public int Version { get; init; } = 1;
    public int SlotsPerDay { get; init; } = 48;
    public long Seed { get; init; } = 1123;
    public Dictionary<string, double> PressureRates { get; init; } = new();
    public double StoryletRate { get; init; } = 1.0;
    public double StoryletCooldownScale { get; init; } = 1.0;
    public double CopresenceBonus { get; init; } = 1.0;
    public bool HearsayRequired { get; init; } = true;
    public double Actionability { get; init; } = 0.5;
    public int SummaryLines { get; init; } = 5;
    public bool BioMarksEnabled { get; init; } = true;
}

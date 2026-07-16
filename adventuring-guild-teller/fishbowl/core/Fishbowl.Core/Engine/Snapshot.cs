using Fishbowl.Core.Json;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// Save/load of the mutable world state to a JSON snapshot (bridge SaveSnapshot/LoadSnapshot;
/// also the backing for the observatory's scrub-backward = "restore the dawn snapshot + re-sim
/// forward"). RNG streams are NOT stored — they are re-derived from (seed, day, stream) each
/// day, so a snapshot taken at a day boundary resumes bit-identically. Ints are tolerant-parsed
/// on load, so a snapshot round-tripped through Godot's stringify (4 → 4.0) still loads.
/// </summary>
public static class Snapshot
{
    public static string Save(World world)
    {
        var file = new SnapshotFile
        {
            Seed = world.Seed, Day = world.Day, Slot = world.Slot, Config = world.Config,
            Townees = world.Townees.Select(t => new SnapTownee
            {
                Id = t.Id, Away = t.Away,
                Pressures = new Dictionary<string, double>(t.Pressures),
                Regard = t.Regard.ToDictionary(kv => kv.Key,
                    kv => new SnapRegard { Score = kv.Value.Score, Tags = new List<string>(kv.Value.Tags) }),
                Marks = new List<MarkDto>(t.Marks),
            }).ToList(),
            Cooldowns = new Dictionary<string, int>(world.Cooldowns),
            Chronicle = world.Chronicle.Select(SnapOf).ToList(),
            DayHashes = world.DayHashes.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
        };
        return DataJson.Serialize(file);
    }

    public static World Load(Town town, string json)
    {
        var file = DataJson.Deserialize<SnapshotFile>(json);
        var world = World.Build(town);
        world.Seed = file.Seed;
        world.Config = file.Config ?? town.Config;
        world.Day = file.Day;

        var byId = world.TowneeById;
        foreach (var st in file.Townees)
        {
            if (!byId.TryGetValue(st.Id, out var t)) continue;
            t.Away = st.Away;
            t.Pressures.Clear();
            foreach (var (d, v) in st.Pressures) t.Pressures[d] = v;
            t.Regard.Clear();
            foreach (var (target, r) in st.Regard)
            {
                var edge = new RegardEdge { Score = r.Score };
                edge.Tags.AddRange(r.Tags);
                t.Regard[target] = edge;
            }
            t.Marks.Clear();
            t.Marks.AddRange(st.Marks);
        }

        world.Cooldowns.Clear();
        foreach (var (k, v) in file.Cooldowns) world.Cooldowns[k] = v;
        world.Chronicle.Clear();
        world.Chronicle.AddRange(file.Chronicle.Select(RuntimeOf));
        foreach (var (k, v) in file.DayHashes) world.DayHashes[int.Parse(k)] = v;

        Clockwork.ResolveDay(world);   // rebuild the current day's itineraries + occupancy
        world.Slot = file.Slot;
        return world;
    }

    private static SnapChronicle SnapOf(ChronicleEntry e) => new()
    {
        Day = e.Day, Slot = e.Slot, StoryletId = e.StoryletId, Kind = e.Kind,
        PlaceId = e.PlaceId, PlaceName = e.PlaceName, Participants = new List<string>(e.Participants),
        Tellability = e.Tellability, Hearsay = e.Hearsay, Gossip = e.Gossip, Report = e.Report,
        Because = e.Because.Select(b => new BecauseFact(b.Label, b.Value)).ToList(),
        CarriedByGossip = e.CarriedByGossip,
    };

    private static ChronicleEntry RuntimeOf(SnapChronicle s) => new()
    {
        Day = s.Day, Slot = s.Slot, StoryletId = s.StoryletId, Kind = s.Kind,
        PlaceId = s.PlaceId, PlaceName = s.PlaceName, Participants = new List<string>(s.Participants),
        Tellability = s.Tellability, Because = s.Because, Hearsay = s.Hearsay, Gossip = s.Gossip,
        Report = s.Report, CarriedByGossip = s.CarriedByGossip,
    };
}

// --- snapshot DTOs -----------------------------------------------------------------------

public sealed record SnapshotFile
{
    public int Version { get; init; } = 1;
    public long Seed { get; init; }
    public int Day { get; init; }
    public int Slot { get; init; }
    public SimConfig? Config { get; init; }
    public List<SnapTownee> Townees { get; init; } = new();
    public Dictionary<string, int> Cooldowns { get; init; } = new();
    public List<SnapChronicle> Chronicle { get; init; } = new();
    public Dictionary<string, string> DayHashes { get; init; } = new();
}

public sealed record SnapTownee
{
    public string Id { get; init; } = "";
    public bool Away { get; init; }
    public Dictionary<string, double> Pressures { get; init; } = new();
    public Dictionary<string, SnapRegard> Regard { get; init; } = new();
    public List<MarkDto> Marks { get; init; } = new();
}

public sealed record SnapRegard
{
    public double Score { get; init; }
    public List<string> Tags { get; init; } = new();
}

public sealed record SnapChronicle
{
    public int Day { get; init; }
    public int Slot { get; init; }
    public string StoryletId { get; init; } = "";
    public string Kind { get; init; } = "";
    public string PlaceId { get; init; } = "";
    public string PlaceName { get; init; } = "";
    public List<string> Participants { get; init; } = new();
    public double Tellability { get; init; }
    public string Hearsay { get; init; } = "";
    public string Gossip { get; init; } = "";
    public string Report { get; init; } = "";
    public List<BecauseFact> Because { get; init; } = new();
    public bool CarriedByGossip { get; init; }
}

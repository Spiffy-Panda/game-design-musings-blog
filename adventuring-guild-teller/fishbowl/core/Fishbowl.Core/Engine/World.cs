using System.Text.Json.Nodes;
using Fishbowl.Core.Determinism;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// The single mutable world state. Ticks single-threaded; townees iterate in stable id
/// order, places in stable id order (PLAN "Determinism"). All randomness comes from named
/// streams derived from (seed, day, stream) — never from wall-clock or process state.
/// </summary>
public sealed class World
{
    public required Town Town { get; init; }
    public required long Seed { get; set; }

    /// <summary>Live, knob-able config (a mutable copy of the loaded defaults). The debug knobs
    /// bind here and take effect without a restart; slots_per_day and seed are structural and
    /// changed via re-seed/regen, not the live dial.</summary>
    public SimConfig Config { get; set; } = new();

    public int Day { get; set; } = 1;
    public int Slot { get; set; }
    public int SlotsPerDay => Config.SlotsPerDay;
    public int MinutesPerSlot => 1440 / Config.SlotsPerDay;

    public required IReadOnlyList<Townee> Townees { get; init; }         // stable id order
    public required IReadOnlyDictionary<string, Townee> TowneeById { get; init; }

    /// <summary>storylet id → last day it fired (for cooldown checks).</summary>
    public Dictionary<string, int> Cooldowns { get; } = new();

    public List<ChronicleEntry> Chronicle { get; } = new();
    public Dictionary<int, string> DayHashes { get; } = new();
    public Dictionary<int, List<SummaryLine>> Summaries { get; } = new();

    /// <summary>Per-slot pressure samples for the inspector sparklines, keyed "id.drive".</summary>
    public Dictionary<string, List<double>> PressureLog { get; } = new();

    /// <summary>Sample every townee's drives (called once per slot by the tick driver).</summary>
    public void RecordPressures()
    {
        foreach (var t in Townees)
            foreach (var d in Town.Drives)
            {
                var key = $"{t.Id}.{d}";
                if (!PressureLog.TryGetValue(key, out var list)) PressureLog[key] = list = new List<double>();
                list.Add(t.Pressure(d));
            }
    }

    /// <summary>Per-slot occupancy for the current day: slot → place id → townee ids present.
    /// A roaming townee (courier) is present at every place in their roam set.</summary>
    private Dictionary<string, List<string>>[] _occupants = Array.Empty<Dictionary<string, List<string>>>();

    public static World Build(Town town)
    {
        var townees = new List<Townee>();
        foreach (var dto in town.Townees.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            var n = new Townee
            {
                Id = dto.Id, Name = dto.Name, Role = dto.Role, Adventurer = dto.Adventurer,
                Traits = dto.Traits.ToArray(), DayplanId = dto.Dayplan, Home = dto.Home,
                Work = dto.Work, Haunts = dto.Haunts.ToArray(), Bio = dto.Bio,
                DepartsDay = dto.DepartsDay, Away = false,
            };
            foreach (var drive in Town.Drives)
                n.Pressures[drive] = dto.Pressures.TryGetValue(drive, out var v) ? v : 0.5;
            foreach (var (target, r) in dto.Regard)
            {
                var edge = new RegardEdge { Score = r.Score };
                edge.Tags.AddRange(r.Tags);
                n.Regard[target] = edge;
            }
            foreach (var m in dto.Marks) n.Marks.Add(m);
            townees.Add(n);
        }

        var world = new World
        {
            Town = town,
            Seed = town.Config.Seed,
            Config = town.Config,   // records are immutable; SetKnob replaces this reference
            Townees = townees,
            TowneeById = townees.ToDictionary(t => t.Id, StringComparer.Ordinal),
        };
        Clockwork.ResolveDay(world);
        return world;
    }

    // --- clockwork occupancy (set by Clockwork.ResolveDay) ---
    internal void SetOccupants(Dictionary<string, List<string>>[] occ) => _occupants = occ;

    /// <summary>place id → townee ids present at the given slot of the current day.</summary>
    public IReadOnlyDictionary<string, List<string>> OccupantsAt(int slot) => _occupants[slot];

    /// <summary>Anchor place shown in readouts for a townee at a slot ("away" if on expedition).</summary>
    public string PlaceOf(Townee t, int slot) => t.Itinerary.Length > slot ? t.Itinerary[slot] : "away";

    // Per-day RNG streams: created once per day and reused across that day's slots so
    // draws advance through the day rather than resetting each slot. Cleared at day setup.
    private readonly Dictionary<string, Rng> _streams = new();
    public Rng RngFor(string stream)
    {
        if (!_streams.TryGetValue(stream, out var r)) _streams[stream] = r = Rng.Stream(Seed, Day, stream);
        return r;
    }
    public void ResetDayStreams() => _streams.Clear();
    public Rng SubRngFor(string stream, string key) => Rng.SubStream(Seed, Day, stream, key);

    /// <summary>Set a live debug knob (PLAN "Debug knobs"). Bools use value != 0.</summary>
    public void SetKnob(string name, double value)
    {
        if (name.StartsWith("pressure_rates.", StringComparison.Ordinal))
        {
            var drive = name["pressure_rates.".Length..];
            var rates = new Dictionary<string, double>(Config.PressureRates) { [drive] = value };
            Config = Config with { PressureRates = rates };
            return;
        }
        Config = name switch
        {
            "storylet_rate" => Config with { StoryletRate = value },
            "storylet_cooldown_scale" => Config with { StoryletCooldownScale = value },
            "copresence_bonus" => Config with { CopresenceBonus = value },
            "actionability" => Config with { Actionability = value },
            "summary_lines" => Config with { SummaryLines = (int)Math.Round(value) },
            "hearsay_required" => Config with { HearsayRequired = value != 0 },
            "bio_marks_enabled" => Config with { BioMarksEnabled = value != 0 },
            _ => Config,
        };
        if (name == "seed") Seed = (long)value;
    }

    /// <summary>The away-flag knob — send or return an adventurer (expedition stand-in, AGT.11).</summary>
    public void SetAway(string towneeId, bool away)
    {
        if (TowneeById.TryGetValue(towneeId, out var t)) { t.Away = away; Clockwork.ResolveDay(this); }
    }

    /// <summary>
    /// Canonical hash-input for the dawn day-hash: day + per-townee mutable state +
    /// cooldowns + the day's chronicle digest. Resolves appendix MUA.Q5 (exactly what
    /// enters the hash). Object key ordering and float precision are handled by
    /// <see cref="Json.CanonicalJson"/>; arrays that need stable order are pre-sorted here.
    /// </summary>
    public JsonObject ToHashNode()
    {
        var townees = new JsonArray();
        foreach (var t in Townees) // already id-ordered
        {
            var pressures = new JsonObject();
            foreach (var d in Town.Drives) pressures[d] = t.Pressure(d);

            var regard = new JsonObject();
            foreach (var (target, edge) in t.Regard.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var tags = new JsonArray();
                foreach (var tag in edge.Tags.OrderBy(x => x, StringComparer.Ordinal)) tags.Add(tag);
                regard[target] = new JsonObject { ["score"] = edge.Score, ["tags"] = tags };
            }

            var marks = new JsonArray();
            foreach (var m in t.Marks) marks.Add(new JsonObject { ["day"] = m.Day, ["line"] = m.Line });

            townees.Add(new JsonObject
            {
                ["id"] = t.Id,
                ["away"] = t.Away,
                ["teller_regard"] = TellerRegardOf(t),
                ["pressures"] = pressures,
                ["regard"] = regard,
                ["marks"] = marks,
            });
        }

        var cooldowns = new JsonObject();
        foreach (var (id, last) in Cooldowns.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            cooldowns[id] = last;

        var chron = new JsonArray();
        foreach (var e in Chronicle.Where(e => e.Day == Day))
        {
            var who = new JsonArray();
            foreach (var p in e.Participants) who.Add(p);
            chron.Add(new JsonObject { ["slot"] = e.Slot, ["id"] = e.StoryletId, ["who"] = who });
        }

        return new JsonObject
        {
            ["day"] = Day,
            ["townees"] = townees,
            ["cooldowns"] = cooldowns,
            ["chronicle"] = chron,
        };
    }

    public string ComputeDayHash() => FnvHash.Hex(FnvHash.Hash64(Json.CanonicalJson.Canonicalize(ToHashNode())));

    // teller_regard is authored-static in v0 (reserved for the floor, AGT.8); keep it in the
    // hash for forward-compat by reading it from the source DTO.
    private double TellerRegardOf(Townee t) =>
        Town.TowneeById.TryGetValue(t.Id, out var dto) ? dto.TellerRegard : 0.5;
}

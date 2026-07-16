using System.Text.Json;
using Godot;
using Fishbowl.Core.Api;
using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Json;
using Fishbowl.Core.Model;

namespace Fishbowl;

/// <summary>
/// The single autoload bridge — the ONLY C# the engine touches directly. GDScript calls these
/// methods; JSON strings cross the boundary in both directions (never typed C# objects), and
/// state flows out through pull-based getters + four signals. This is the deliberate correction
/// of the mined Autonome bridge (appendix MUA.N3): one autoload, JSON strings, single-threaded.
/// </summary>
public partial class FishbowlBridge : Node
{
    private Simulation _sim = null!;
    private World World => _sim.World;

    [Signal] public delegate void SlotTickedEventHandler(int day, int slot);
    [Signal] public delegate void EventLoggedEventHandler(string eventJson);
    [Signal] public delegate void DawnReadyEventHandler(int day, string summaryJson);
    [Signal] public delegate void HashReadyEventHandler(int day, string hash);

    public override void _Ready() => LoadTown(GlobalizePath("res://data"));

    // --- lifecycle ---------------------------------------------------------------------

    public string LoadTown(string path)
    {
        try
        {
            var town = TownLoader.Load(path);
            SetSim(new Simulation(town));
            return Ok();
        }
        catch (System.Exception e) { return Err(e); }
    }

    public string GenerateTown(string configJson)
    {
        try
        {
            var cfg = DataJson.Deserialize<GenConfig>(configJson);
            var townees = TownGenerator.Generate(World.Town, cfg).Townees;
            // A fresh cast carries no golden-anchored storylets; observe clockwork + pressures.
            var generated = TownLoader.Rebuild(World.Town, townees: townees);
            var storyletless = new Town
            {
                Config = generated.Config, Places = generated.Places, Townees = generated.Townees,
                DayPlans = generated.DayPlans, Traits = generated.Traits,
                Storylets = System.Array.Empty<StoryletDto>(), Golden = null,
                PlaceById = generated.PlaceById, TowneeById = generated.TowneeById,
                TraitById = generated.TraitById, StoryletById = new System.Collections.Generic.Dictionary<string, StoryletDto>(),
            };
            SetSim(new Simulation(storyletless));
            return Ok();
        }
        catch (System.Exception e) { return Err(e); }
    }

    // --- ticking -----------------------------------------------------------------------

    public void StepSlot() => _sim.StepSlot();
    public void RunToDawn() => _sim.RunToDawn();
    public void RunDays(int n) => _sim.RunDays(n);

    // --- readouts (JSON out) -----------------------------------------------------------

    public string GetClock() => WorldView.ClockJson(World);
    public string GetRoster() => WorldView.RosterJson(World);
    public string GetTownee(string id) => WorldView.TowneeJson(World, id);
    public string GetPlaces() => WorldView.PlacesJson(World);
    public string GetChronicle(int day) => WorldView.ChronicleJson(World, day);
    public string GetSummary(int day) => WorldView.SummaryJson(World, day);
    public string GetStats(int day) => WorldView.StatsJson(World, day);
    public string GetKnobs() => WorldView.KnobsJson(World);
    public string GetPressureSeries(string id, string drive) => WorldView.PressureSeriesJson(World, id, drive);
    public string GetStorylets() => StoryletsJson();
    public int CurrentDay() => World.Day;

    // --- knobs -------------------------------------------------------------------------

    public void SetKnob(string name, double value) => World.SetKnob(name, value);
    public void SetAway(string id, bool away) => World.SetAway(id, away);

    public string Reseed(long seed)
    {
        World.Seed = seed;
        World.ResetDayStreams();
        return Ok();
    }

    // --- creation menus ----------------------------------------------------------------

    public string CreateTownee(string json)
    {
        try
        {
            var dto = DataJson.Deserialize<TowneeDto>(json);
            var town = World.Town.WithTownee(dto);          // re-validates
            PersistUserTown(town);
            SetSim(new Simulation(town));                    // hot-add: appears next dawn
            return Ok();
        }
        catch (System.Exception e) { return Err(e); }
    }

    public string CreatePlace(string json)
    {
        try
        {
            var dto = DataJson.Deserialize<PlaceDto>(json);
            var town = World.Town.WithPlace(dto);
            SetSim(new Simulation(town));
            return Ok();
        }
        catch (System.Exception e) { return Err(e); }
    }

    public string InjectStorylet(string id, string participantsJson)
    {
        try
        {
            var participants = DataJson.Deserialize<System.Collections.Generic.List<string>>(participantsJson);
            var e = StoryletEngine.ForceFire(World, id, participants, Mathf.Clamp(World.Slot, 0, World.SlotsPerDay - 1),
                ev => EmitSignal(SignalName.EventLogged, WorldView.ChronicleJson(World, ev.Day)));
            return e is null ? Err("force-fire produced no event") : Ok();
        }
        catch (System.Exception e) { return Err(e); }
    }

    // --- snapshots ---------------------------------------------------------------------

    public string SaveSnapshot(string path)
    {
        try { DataJson.WriteText(GlobalizePath(path), Snapshot.Save(World)); return Ok(); }
        catch (System.Exception e) { return Err(e); }
    }

    public string LoadSnapshot(string path)
    {
        try
        {
            var world = Snapshot.Load(World.Town, DataJson.ReadText(GlobalizePath(path)));
            SetSim(new Simulation(world));
            return Ok();
        }
        catch (System.Exception e) { return Err(e); }
    }

    // --- wiring ------------------------------------------------------------------------

    private void SetSim(Simulation sim)
    {
        _sim = sim;
        _sim.SlotTicked += (d, s) => EmitSignal(SignalName.SlotTicked, d, s);
        _sim.HashReady += (d, h) => EmitSignal(SignalName.HashReady, d, h);
        _sim.DawnReady += (d, _) => EmitSignal(SignalName.DawnReady, d, WorldView.SummaryJson(World, d));
        _sim.EventLogged += e => EmitSignal(SignalName.EventLogged, WorldView.ChronicleJson(World, e.Day));
    }

    private string StoryletsJson()
    {
        var arr = new System.Text.Json.Nodes.JsonArray();
        foreach (var s in World.Town.Storylets)
            arr.Add(new System.Text.Json.Nodes.JsonObject
            {
                ["id"] = s.Id, ["kind"] = s.Kind, ["weight"] = s.Weight,
                ["cooldown"] = s.Predicates.CooldownDays, ["must_fire"] = s.MustFire,
            });
        return new System.Text.Json.Nodes.JsonObject { ["storylets"] = arr }.ToJsonString();
    }

    private static void PersistUserTown(Town town)
    {
        string dir = GlobalizePath("user://town");
        DirAccess.MakeDirRecursiveAbsolute(dir);
        var file = new TowneesFile { Version = 1, Townees = town.Townees.ToList() };
        DataJson.WriteText(System.IO.Path.Combine(dir, "townees.json"), DataJson.Serialize(file));
    }

    private static string GlobalizePath(string path) => ProjectSettings.GlobalizePath(path);
    private static string Ok() => "{\"ok\":true}";
    private static string Err(string message) =>
        new System.Text.Json.Nodes.JsonObject { ["ok"] = false, ["error"] = message }.ToJsonString();
    private static string Err(System.Exception e) => Err(e.Message);
}

using System.Globalization;
using System.Text.Json.Nodes;
using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Json;
using Fishbowl.Core.Model;

// Headless fish-bowl runner. The research instrument behind VFB.Q1/Q4:
//   Fishbowl.Cli --town data/ --seed 1123 --days 7 --report out.json [--chronicle] [--soak]

var opts = ParseArgs(args);
string townDir = opts.Town ?? ProjectPaths.DataDir();

if (opts.Soak) { RunSoak(townDir, opts); return 0; }

var sim = BuildSim(townDir, opts.Seed);
var town = sim.World.Town;

Console.WriteLine($"fish-bowl · town={townDir}");
Console.WriteLine($"seed={sim.World.Seed} slots/day={town.Config.SlotsPerDay} townees={town.Townees.Count} storylets={town.Storylets.Count}");
Console.WriteLine(new string('-', 68));

for (int d = 0; d < opts.Days; d++)
{
    int day = sim.World.Day;
    var summary = sim.RunToDawn();
    string hash = sim.World.DayHashes[day];
    var dayEvents = sim.World.Chronicle.Where(e => e.Day == day).ToList();
    Console.WriteLine($"day {day}  hash={hash}  events={dayEvents.Count}  summary={summary.Count}");

    if (opts.Chronicle)
        foreach (var e in dayEvents.OrderBy(e => e.Slot))
            Console.WriteLine($"    [{e.Slot,2}] {e.StoryletId,-20} {string.Join(", ", e.Participants)}  @{e.PlaceName}");

    foreach (var line in summary)
        Console.WriteLine($"    • {line.Text}");
    Console.WriteLine();
}

if (opts.Report is { } reportPath)
{
    WriteReport(sim, reportPath, opts.Days);
    Console.WriteLine($"report → {reportPath}");
}
return 0;

// ---------------------------------------------------------------------------------------

static Simulation BuildSim(string dir, long? seedOverride)
{
    var sim = new Simulation(TownLoader.Load(dir));
    if (seedOverride is long s) { sim.World.Seed = s; sim.World.ResetDayStreams(); }
    return sim;
}

static void RunSoak(string townDir, Options opts)
{
    // VFB.M4 soak: N seeds × M days → distinct-line + repeated-type instrumentation (VFB.Q1).
    long[] seeds = opts.Seeds ?? new long[] { 1123, 2027, 5501 };
    int days = opts.Days > 0 ? opts.Days : 7;
    Console.WriteLine($"soak · {seeds.Length} seeds × {days} days");
    var perNight = new List<(long seed, int day, int distinct, int topRepeat)>();

    foreach (var seed in seeds)
    {
        var sim = BuildSim(townDir, seed);
        for (int d = 0; d < days; d++)
        {
            int day = sim.World.Day;
            var summary = sim.RunToDawn();
            int distinct = summary.Select(l => l.Text).Distinct().Count();
            int topRepeat = summary.GroupBy(l => l.StoryletId).Select(g => g.Count()).DefaultIfEmpty(0).Max();
            perNight.Add((seed, day, distinct, topRepeat));
        }
    }

    double avgDistinct = perNight.Count > 0 ? perNight.Average(x => x.distinct) : 0;
    int starved = perNight.Count(x => x.distinct < 4);
    Console.WriteLine($"avg distinct summary lines/night = {avgDistinct:0.00}");
    Console.WriteLine($"nights below 4 distinct (starvation) = {starved}/{perNight.Count}");
    Console.WriteLine(avgDistinct >= 4 && starved == 0
        ? "VFB.Q1: PASS at these seeds (>=4 distinct, no starvation)."
        : "VFB.Q1: tune - starvation or low variety present.");
}

static void WriteReport(Simulation sim, string path, int days)
{
    var daysNode = new JsonArray();
    foreach (var (day, hash) in sim.World.DayHashes.OrderBy(kv => kv.Key))
    {
        var events = new JsonArray();
        foreach (var e in sim.World.Chronicle.Where(e => e.Day == day).OrderBy(e => e.Slot))
            events.Add(new JsonObject
            {
                ["slot"] = e.Slot, ["storylet"] = e.StoryletId,
                ["participants"] = new JsonArray(e.Participants.Select(p => (JsonNode)p!).ToArray()),
            });
        var summary = new JsonArray();
        foreach (var l in sim.World.Summaries[day]) summary.Add(l.Text);
        daysNode.Add(new JsonObject { ["day"] = day, ["hash"] = hash, ["events"] = events, ["summary"] = summary });
    }
    var root = new JsonObject { ["seed"] = sim.World.Seed, ["days"] = days, ["report"] = daysNode };
    DataJson.WriteText(path, root.ToJsonString(DataJson.Pretty));
}

static Options ParseArgs(string[] a)
{
    var o = new Options();
    for (int i = 0; i < a.Length; i++)
    {
        switch (a[i])
        {
            case "--town": o.Town = a[++i]; break;
            case "--seed": o.Seed = long.Parse(a[++i], CultureInfo.InvariantCulture); break;
            case "--days": o.Days = int.Parse(a[++i], CultureInfo.InvariantCulture); break;
            case "--report": o.Report = a[++i]; break;
            case "--chronicle": o.Chronicle = true; break;
            case "--soak": o.Soak = true; break;
            case "--seeds": o.Seeds = a[++i].Split(',').Select(s => long.Parse(s, CultureInfo.InvariantCulture)).ToArray(); break;
        }
    }
    if (o.Days <= 0) o.Days = 3;
    return o;
}

sealed class Options
{
    public string? Town;
    public long? Seed;
    public int Days;
    public string? Report;
    public bool Chronicle;
    public bool Soak;
    public long[]? Seeds;
}

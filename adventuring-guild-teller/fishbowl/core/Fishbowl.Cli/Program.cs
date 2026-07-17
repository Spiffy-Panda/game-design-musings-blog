using System.Globalization;
using Fishbowl.Core.Api;
using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Json;

// Headless fish-bowl runner. The research instrument behind VFB.Q1/Q4:
//   Fishbowl.Cli --town data/ --seed 1123 --days 7 --report out.json [--chronicle] [--soak]
//   Fishbowl.Cli --lint [--town <dir>] [--report lint.json] [--json]   content-health gate
//   ... --knob name=value        override a knob (repeatable); --knob novelty_decay=1.0 ablates
//                                the novelty term, restoring the fixed-leaderboard Summarizer.

var opts = ParseArgs(args);
string townDir = opts.Town ?? ProjectPaths.DataDir();

// --lint exits non-zero on any error-class finding, so CI and a content author can gate on it.
if (opts.Lint) return Fishbowl.Cli.Linter.Run(townDir, opts.Report, opts.Json);

if (opts.Soak)
{
    // --soak writes no report; say so rather than accepting the flag and dropping it. A tool that
    // silently ignores an argument teaches its caller that the argument worked.
    if (opts.Report is not null)
        Console.Error.WriteLine("note: --report is not written for --soak (a soak runs many seeds; "
                                + "the report projects one run). Drop --soak for a report.");
    RunSoak(townDir, opts);
    return 0;
}

var sim = BuildSim(townDir, opts.Seed, opts.Knobs);
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

/// <summary>
/// Builds the sim and applies any <c>--knob name=value</c> overrides before the first day runs.
/// <para>Applied up front rather than at read time so the flag means the same thing for both knob
/// families: a simulation knob has to be set before the day it should affect, and a rendering knob
/// is re-read on every render anyway, so setting it early is equivalent. The alternative — applying
/// them after the run — would silently no-op every simulation knob.</para>
/// </summary>
static Simulation BuildSim(string dir, long? seedOverride, IEnumerable<(string Name, double Value)>? knobs = null)
{
    var sim = new Simulation(TownLoader.Load(dir));
    if (seedOverride is long s) { sim.World.Seed = s; sim.World.ResetDayStreams(); }
    foreach (var (name, value) in knobs ?? Enumerable.Empty<(string, double)>())
        sim.World.SetKnob(name, value);
    return sim;
}

/// <summary>
/// VFB.M4 soak: N seeds × M days. Reports the across-night variety measure (<see cref="Variety"/>)
/// and, alongside it, the old saturated VFB.Q1 figure.
/// <para><b>Per seed, never pooled.</b> A player plays one run; the union of three runs' sentences
/// is a number nobody experiences. So each seed is measured on its own and the seeds are averaged,
/// which keeps "distinct sentences in a fortnight" a claim about a fortnight.</para>
/// </summary>
static void RunSoak(string townDir, Options opts)
{
    long[] seeds = opts.Seeds ?? new long[] { 1123, 2027, 5501 };
    int days = opts.Days > 0 ? opts.Days : 7;
    Console.WriteLine($"soak · {seeds.Length} seeds × {days} days");

    var runs = new List<(long Seed, Variety V, string Fingerprint)>();
    foreach (var seed in seeds)
    {
        var sim = BuildSim(townDir, seed, opts.Knobs);
        sim.RunDays(days);
        string fingerprint = string.Join(",", Enumerable.Range(1, days)
            .Select(d => sim.World.DayHashes.GetValueOrDefault(d, "?")));
        runs.Add((seed, Variety.Measure(sim.World), fingerprint));
    }

    Console.WriteLine(new string('-', 68));
    Console.WriteLine($"{"seed",-8} {"distinct",8} {"lines",6} {"novelty",8} {"rpt(N-1)",9} {"rpt(any)",9} {"told/fired",11} {"never told",11}");
    foreach (var (seed, v, _) in runs)
        Console.WriteLine($"{seed,-8} {v.DistinctTexts,8} {v.DeliveredLines,6} {v.NoveltyRate,8:0.00} "
                        + $"{v.RepeatRatePreviousNight,9:0.00} {v.RepeatRateAnyPriorNight,9:0.00} "
                        + $"{v.ToldPerFired,11:0.00} {v.RulesFiredButNeverTold,11}");
    Console.WriteLine(new string('-', 68));

    // If every seed produced the identical day-hash sequence, the sweep measured one run N times.
    // Say so: at storylet_rate 1.0 nothing draws RNG, so "3 seeds" is decoration and averaging over
    // it would dress a single sample up as three.
    bool seedsCollapse = runs.Select(r => r.Fingerprint).Distinct().Count() == 1 && runs.Count > 1;

    double avgDistinct = runs.Average(r => r.V.DistinctTexts);
    double avgNovelty = runs.Average(r => r.V.NoveltyRate);
    var first = runs[0].V;

    Console.WriteLine($"THE NUMBER — distinct sentences a player reads in {days} nights = {avgDistinct:0.0}"
                      + $"  (of {first.DeliveredLines} lines delivered; novelty {avgNovelty:0.00})");
    Console.WriteLine($"  repeats a line from the night before : {first.RepeatRatePreviousNight:0.00} of lines");
    Console.WriteLine($"  repeats a line from any prior night  : {first.RepeatRateAnyPriorNight:0.00} of lines");
    Console.WriteLine($"  beats told / beats fired             : {first.BeatsTold}/{first.BeatsFired} = {first.ToldPerFired:0.00}");
    Console.WriteLine($"  rules that fired but were never told : {first.RulesFiredButNeverTold} of {first.RulesFired}");

    if (seedsCollapse)
        Console.WriteLine($"NOTE: all {runs.Count} seeds produced an identical day-hash sequence — this town draws no "
                          + "RNG at its authored knobs (storylet_rate 1.0 never thins), so the seed sweep is "
                          + "measuring ONE run N times. Vary storylet_rate to make seeds mean something.");

    // The old figure, kept verbatim so the plan's number still resolves — and labelled, because it
    // cannot move. Deleting it would hide the comparison that justifies everything above.
    Console.WriteLine(new string('-', 68));
    int starved = runs.Sum(r => r.V.NightsBelowFourWithinNight);
    int nights = runs.Sum(r => r.V.Nights);
    double q1 = runs.Average(r => r.V.Vfb_Q1_AvgDistinctPerNight);
    Console.WriteLine($"VFB.Q1 (SATURATED — kept for continuity, do not tune against it):");
    Console.WriteLine($"  avg distinct summary lines/night = {q1:0.00}   [ceiling is summary_lines]");
    Console.WriteLine($"  nights below 4 distinct = {starved}/{nights}");
    Console.WriteLine("  " + (q1 >= 4 && starved == 0
        ? "VFB.Q1: PASS at these seeds (>=4 distinct, no starvation)."
        : "VFB.Q1: tune - starvation or low variety present."));
    Console.WriteLine("  ^ this counts distinct texts WITHIN a summary already cut to summary_lines, so it is "
                      + "min(pool, summary_lines) averaged. It reads 5.00 with 30 of 46 rules deleted, and it "
                      + "never compares night N to night N-1. THE NUMBER above is the one that moves.");
}

/// <summary>The report is built and serialized by <see cref="RunReport"/> in the core, not here.
/// It used to be a local function in this file, which is why nothing could test it and why it threw
/// on every invocation for its whole life without anyone noticing.</summary>
static void WriteReport(Simulation sim, string path, int days) =>
    DataJson.WriteText(path, RunReport.ToJson(sim.World, days));

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
            case "--lint": o.Lint = true; break;
            case "--json": o.Json = true; break;
            case "--seeds": o.Seeds = a[++i].Split(',').Select(s => long.Parse(s, CultureInfo.InvariantCulture)).ToArray(); break;
            // --knob name=value, repeatable. The A/B surface: `--knob novelty_decay=1.0` is the
            // ablation, and it is exact rather than approximate (decay 1.0 skips the ledger).
            case "--knob":
            {
                var parts = a[++i].Split('=', 2);
                if (parts.Length != 2)
                    throw new ArgumentException($"--knob wants name=value, got '{parts[0]}'");
                o.Knobs.Add((parts[0], double.Parse(parts[1], CultureInfo.InvariantCulture)));
                break;
            }
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
    public bool Lint;
    public bool Json;
    public long[]? Seeds;
    public List<(string Name, double Value)> Knobs = new();
}

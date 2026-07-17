using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// NTD.Q1 — <b>the guard whose absence let a drive count down for the project's whole life.</b>
///
/// <para><c>trade</c> was <c>-0.11/day</c> in every mode for every townee, with no positive path anywhere
/// in the engine, and <b>every test in the suite ran green over it</b> — including a pinned 3-day hash
/// sequence and a golden-day acceptance list, because a ratchet is perfectly deterministic and perfectly
/// reproducible. Determinism was never the property that was missing. <i>Two-wayness</i> was, and nothing
/// asserted it. (The golden day was worse than silent: two of its seven pinned beats only fired <i>because</i>
/// of the ratchet, so the acceptance list was preserving the defect. See DEV-LOG 2026-07-16.)</para>
///
/// <para><b>These assert the property, not the numbers.</b> Nothing here hard-codes <c>0.55</c>, <c>0.12</c>
/// or <c>0.20</c> — retuning those is expected and must stay cheap, so the rest points are recovered from
/// <see cref="Pressures.BaseDaily"/> itself rather than copied out of it. What must not change without an
/// argument is the <i>shape</i>: a restoring drive settles somewhere a storylet can still move it; a
/// mode-constant drive does not settle at all. If you are here because this went red, you have most likely
/// made a drive one-way — that is the bug this file exists to name, not a threshold to nudge.</para>
/// </summary>
public class M2_TradeEquilibriumTests
{
    /// <summary>Days of pure drift — past the ~5-day time constant, and the same horizon
    /// <c>--lint</c>'s latch/die prediction uses.</summary>
    private const int Horizon = 14;

    /// <summary>The live town, drifted <paramref name="days"/> days with <b>the storylet bank switched
    /// off</b>. Bank-silent is the case that matters: a drive the bank has to prop up is a drive that dies
    /// for every townee no rule ever pays, and this town has plenty of those.</summary>
    private static World DriftOnly(int days)
    {
        var w = World.Build(TownLoader.Load(ProjectPaths.DataDir()));
        for (int day = 1; day <= days; day++)
        {
            w.Day = day;
            Clockwork.ResolveDay(w);
            for (int slot = 0; slot < w.SlotsPerDay; slot++) Pressures.DriftSlot(w, slot);
        }
        return w;
    }

    /// <summary>Recovers a mode's trade rest point from <see cref="Pressures.BaseDaily"/> itself. The drift
    /// is <c>(rest - current) * k</c>, which is zero exactly at the rest point; two probes give both unknowns.
    /// So this test reads the engine's real intent instead of a copy of its constants, and a retune keeps it
    /// honest rather than red.</summary>
    private static double RestOf(string mode)
    {
        double d0 = Pressures.BaseDaily("trade", mode, 0.0);   // rest * k
        double d1 = Pressures.BaseDaily("trade", mode, 1.0);   // (rest - 1) * k
        double k = d0 - d1;                                    // k
        Assert.True(k > 1e-9, "trade is no longer a restoring force — BaseDaily ignores `current` for it, "
            + "which means it has no rest point and can only ever count toward a clamp. That is NTD.Q1 back.");
        return d0 / k;
    }

    [Fact]
    public void Trade_Equilibrates_Bank_Silent_For_Every_Townee()
    {
        var w = DriftOnly(Horizon);

        foreach (var t in w.Townees)
        {
            double trade = t.Pressure("trade");

            // The two ways a drive dies. Both are perfectly deterministic, and both are worthless.
            Assert.False(trade <= 0.02,
                $"{t.Id}'s trade fell to {trade:0.000} on drift alone in {Horizon} days — that is a ratchet, not "
                + "a pressure. A drive pinned at a clamp gates nothing: every predicate reading it is permanently "
                + "true or permanently false, and no bank can fix it (see Pressures.BaseDaily).");
            Assert.False(trade >= 0.98,
                $"{t.Id}'s trade climbed to {trade:0.000} on drift alone in {Horizon} days — a ratchet with the "
                + "opposite sign, which is exactly what a mode-constant 'work pays trade' fix produces, and which "
                + "--lint cannot see because it tests the per-mode sign, not the net drift.");
        }
    }

    [Fact]
    public void Trade_Rest_Point_Is_Set_By_The_Dayplans_Work_Slots()
    {
        // The closed form IS the design: rest is the time-weighted blend of the per-mode targets, so the same
        // lever that tunes purse and restlessness — how many work slots a dayplan authors — tunes trade too.
        var w = DriftOnly(60); // well past the time constant; the exponential is spent
        double atWork = RestOf("work"), idle = RestOf("home");

        foreach (var t in w.Townees)
        {
            int work = t.Mode.Count(m => m == "work");
            double predicted = (work * atWork + (w.SlotsPerDay - work) * idle) / w.SlotsPerDay;

            Assert.True(Math.Abs(t.Pressure("trade") - predicted) < 0.01,
                $"{t.Id} (work={work}) settled at {t.Pressure("trade"):0.000} but the per-mode targets predict "
                + $"{predicted:0.000}. The rest point must stay the work/idle blend — that is the whole of what "
                + "makes a dayplan's work slots mean anything for trade.");
        }
    }

    [Fact]
    public void A_Working_Townee_Rests_Higher_Than_An_Idle_One()
    {
        // The point of the fix, as one assertion: work has to be worth something. Petch keeps a shop; Brindle
        // has no `work` at all. If these ever converge, `trade` has stopped modelling a livelihood.
        var w = DriftOnly(Horizon);
        double petch = w.TowneeById["petch"].Pressure("trade");
        double brindle = w.TowneeById["brindle-ashe"].Pressure("trade");

        Assert.True(petch > brindle + 0.05,
            $"the shopkeeper (petch, {petch:0.000}) must rest meaningfully above the adventurer who never works "
            + $"(brindle-ashe, {brindle:0.000}) — work is supposed to pay subsistence on drift alone.");
    }

    [Fact]
    public void Trade_Leaves_The_Bank_Headroom_To_Push_It()
    {
        // The budget handed to the bank author (DEV-LOG 2026-07-16): a sustained B/day moves a townee's rest
        // point to rest + B/k, so there has to be real room between where they rest and the clamp. If this goes
        // red, the drive has drifted back toward "the bank must hold it up" — which is the old bug wearing a
        // friendlier number.
        var w = DriftOnly(Horizon);
        foreach (var t in w.Townees)
        {
            double trade = t.Pressure("trade");
            Assert.True(trade < 0.75,
                $"{t.Id} rests at {trade:0.000}, leaving the bank almost no headroom before trade pegs at 1.0. "
                + "Rest points are meant to sit low: work pays subsistence, the supply chain pays prosperity.");
        }
    }
}

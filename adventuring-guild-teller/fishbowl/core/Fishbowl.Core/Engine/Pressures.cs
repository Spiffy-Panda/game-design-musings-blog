using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// L2 Pressures (FBS.2/FBS.5, distilled): slow scalar drives drift by rule each slot.
/// Minutes-scaled so the drift is tick-granularity-independent (appendix MUA.M1). Drives
/// are fuel — they never pick actions; they cross thresholds that let storylets fire, and
/// the north-star is a fragile equilibrium (MUA.J10/Q7): motion enough to feed a chronicle,
/// not so much it fires constantly. Directed regard changes only through storylet effects in
/// v0 (a deliberate MUA.Q3 choice — it keeps authored tensions from washing out).
///
/// <para><b>"Fragile equilibrium" is load-bearing, and two of the four drives literally have one.</b>
/// <c>heart</c> and <c>trade</c> are restoring forces with a rest point; <c>purse</c> and
/// <c>restlessness</c> are mode-constants whose sign the dayplan sets and whose loop the <i>bank</i> is
/// expected to close (an adventurer's purse draining to 0 is the economy pushing them at the board —
/// that one is deliberate, see NTD). If you are tuning here, the first question to ask about a drive is
/// which of the two shapes it has, because they fail in different ways: a mode-constant can only be
/// balanced by authoring its daily sum to exactly zero, which no bank can hold, while a restoring drive
/// absorbs a sustained push by moving its rest point and only pegs past a stateable budget.
/// <c>M2_TradeEquilibriumTests</c> is the guard on that distinction — it exists because <c>trade</c> was
/// one-way for the project's whole life and every test in the suite ran green over it.</para>
/// </summary>
public static class Pressures
{
    // Per-day target deltas by context, before config + trait scaling. Tuned against the
    // golden day; every constant is here (no magic-number sprawl — appendix MUA.N6).
    private const double HeartPullToTarget = 0.20;  // heart regresses toward its target at this fraction/day
    private const double PurseAtWork = 0.05;
    private const double PurseIdle = -0.03;
    private const double RestAtRest = 0.06;        // restlessness accrues at home/asleep
    private const double RestEngaged = -0.10;      // and burns off at work or a haunt

    // --- trade (NTD.Q1 + FBT.Q1, both ruled 2026-07-16; see DEV-LOG) ---------------------------
    // A shop's stock/goodwill converges on what its hours are worth. `TradeRestAtWork` is where trade
    // would settle for someone who worked all 48 slots; `TradeRestIdle` where it settles for someone
    // who never works — low, but not nothing: even a loafer keeps a little standing. A townee's actual
    // rest point is the time-weighted blend, so it is set by their dayplan's work-slot count — the same
    // lever purse and restlessness already use. Closed form, and it matches the engine to 3dp:
    //     rest = [W·TradeRestAtWork + (48-W)·TradeRestIdle] / 48
    // These replaced a flat -0.11/day that could only ever count every townee down to 0. Retuning that
    // constant would not have helped and the reason is the whole lesson — see BaseDaily.
    private const double TradePullToRest = 0.20;   // fraction of the gap closed per day
    private const double TradeRestAtWork = 0.55;
    private const double TradeRestIdle = 0.12;

    /// <summary>Where <c>heart</c> rests for a townee no trait speaks for. 0.5 — so an unauthored trait
    /// leaves the drive exactly where it has always been.</summary>
    public const double HeartDefaultTarget = 0.5;

    public static void DriftSlot(World world, int slot)
    {
        double perSlot = world.MinutesPerSlot / 1440.0;
        foreach (var t in world.Townees)
        {
            if (t.Away) continue; // away townees are off-sim until they return
            string mode = t.Mode.Length > slot ? t.Mode[slot] : "home";
            double heartTarget = HeartTarget(world.Town, t);
            foreach (var drive in Town.Drives)
            {
                double perDay = BaseDaily(drive, mode, t.Pressure(drive), heartTarget);
                double rate = world.Config.PressureRates.TryGetValue(drive, out var r) ? r : 1.0;
                double traitMod = TraitRateMod(world.Town, t, drive, perDay);
                double next = t.Pressures[drive] + perDay * rate * traitMod * perSlot;
                t.Pressures[drive] = Math.Clamp(next, 0.0, 1.0);
            }
        }
    }

    /// <summary>
    /// The net drift <paramref name="drive"/> takes over <b>one whole day of this townee's resolved
    /// itinerary</b>, at its current value, with nothing but the drift touching it. In one sentence:
    /// <i>which way does the day this townee actually lives push this drive, and how hard?</i>
    ///
    /// <para><b>Why this is here and not in the linter.</b> <c>--lint</c>'s <c>ratchets</c> check used to
    /// ask a weaker question — "does <i>some</i> mode push this drive up and <i>some</i> mode push it
    /// down?" — and call anything that answered yes bidirectional. <c>restlessness</c> answers yes
    /// (<c>-0.10</c> at work, <c>+0.06</c> at home) and is still one-way for 16 of the live town's 18
    /// townees, because the sign of the <i>sum</i> is set by the slot counts and the break-even sits at
    /// 18 engaged slots of 48 that nobody is near. The fix is to weight by the day actually lived, and
    /// that sum is not something an instrument can assemble from <see cref="BaseDaily"/> alone:
    /// <see cref="TraitRateMod"/> picks <c>gain</c> or <c>decay</c> per slot off the sign of that slot's
    /// drift, so an asymmetric trait (<c>wanderlust</c> is <c>{gain 1.3, decay 0.7}</c> on
    /// <c>restlessness</c>) re-weights the two halves against each other and <b>moves the break-even</b>
    /// — 18 slots becomes ~25. An instrument that summed <see cref="BaseDaily"/> and called it the net
    /// would print a confident number with the trait silently missing, which is this repo's signature
    /// defect and the reason <see cref="DriftSlot"/>'s arithmetic gets asked rather than copied.
    ///
    /// <para><b>Read-only, and deliberately not wired into <see cref="DriftSlot"/>.</b> It re-walks the
    /// day rather than being factored out of the tick, because the tick's arithmetic is pinned by three
    /// day-hash literals and a shared helper is a behaviour change wearing a refactor's clothes. The
    /// cost is that the two loops must agree; the guard is that everything either one relies on —
    /// the constants, <see cref="BaseDaily"/>, <see cref="HeartTarget"/>, <see cref="TraitRateMod"/>,
    /// the <c>pressure_rates</c> lookup and the minutes scaling — is the same single copy, so only the
    /// summation is restated.</para>
    ///
    /// <para><b>Exact for a mode-constant drive, instantaneous for a restoring one.</b> <c>purse</c> and
    /// <c>restlessness</c> ignore <paramref name="current"/>, so this is the constant daily step and
    /// there is no interior fixed point at any value — the number this returns is the whole future.
    /// <c>heart</c> and <c>trade</c> read it, so this is the drift <i>at where the townee stands now</i>
    /// and it shrinks to zero as they converge; it is a tangent, not a destiny.</para>
    /// </summary>
    public static double NetDaily(World world, Townee t, string drive)
    {
        double perSlot = world.MinutesPerSlot / 1440.0;
        double heartTarget = HeartTarget(world.Town, t);
        double rate = world.Config.PressureRates.TryGetValue(drive, out var r) ? r : 1.0;
        double current = t.Pressure(drive);

        double total = 0.0;
        for (int s = 0; s < world.SlotsPerDay; s++)
        {
            string mode = t.Mode.Length > s ? t.Mode[s] : "home";
            double perDay = BaseDaily(drive, mode, current, heartTarget);
            total += perDay * rate * TraitRateMod(world.Town, t, drive, perDay) * perSlot;
        }
        return total;
    }

    /// <summary>
    /// The per-day drift target for a drive in a mode, before config + trait scaling. <b>Public so
    /// <c>--lint</c> can predict ratchets and latches against the real rule rather than a copy of
    /// it</b> — a linter that reimplements the thing it audits eventually audits its own fiction.
    ///
    /// <para><b>The two shapes, and the difference the linter depends on.</b> <c>purse</c> and
    /// <c>restlessness</c> are <i>constant per mode</i>: they ignore <paramref name="current"/>, so the
    /// dayplan's slot counts set a sign and the drive travels until it clamps. <c>heart</c> and
    /// <c>trade</c> are <i>restoring</i>: the drift is proportional to the gap between where the townee is
    /// and where the mode says they belong, so the sign flips around that rest point and the drive
    /// converges instead of arriving. Sampling this across a townee's actual modes is how the linter tells
    /// "can only ever go down" from "converges" — and the sign test at probes 0.0 and 1.0 is invariant to
    /// <i>which</i> rest point a restoring drive has, so the linter reads both correctly without knowing a
    /// townee's traits, and the default parameter costs it nothing.</para>
    ///
    /// <para><b>Why <c>trade</c> changed shape (NTD.Q1, ruled 2026-07-16).</b> It was
    /// <c>-0.11/day, every mode, every townee</c>, with no positive path anywhere in the engine — a
    /// guaranteed countdown to 0 for all 18, which no authoring could hold: the largest trade effect ever
    /// written is <c>+0.08</c>, and holding the old constant flat needed <c>+0.11</c> every single day.
    /// <b>Retuning that constant would not have rescued it, and that is the part to keep</b>: a
    /// mode-constant drift has <i>no interior fixed point at all</i>. Whatever the numbers, the daily sum
    /// is some constant D, so the drive pegs at 1.0 if D&gt;0 and 0.0 if D&lt;0, and "balanced" would mean
    /// authoring D to exactly zero and then never letting a storylet touch it. Adding a mode-constant
    /// <i>work</i> gain — the obvious fix — would only have <b>relocated</b> the ratchet (high-work townees
    /// to 1.0, low-work to 0.0) while <b>silencing <c>--lint</c></b>, which tested the per-mode sign rather
    /// than the net drift: a green gate over a live countdown, which is strictly worse than the bug.
    ///
    /// <para><b>That paragraph described a bug that was already shipped, and did not notice</b> (corrected
    /// 2026-07-16). <c>restlessness</c> is exactly the relocated-ratchet shape it rejects — <c>-0.10</c>
    /// engaged, <c>+0.06</c> at rest, break-even at 18 engaged slots of 48 — and the live town's 18 townees
    /// sit at 8 or 22–36, so 16 of them ride a clamp. <c>--lint</c> was indeed silent, for precisely the
    /// stated reason. The check has since been rewritten to weight by the day actually lived
    /// (<see cref="NetDaily"/>) and to read the outcome from <c>World.PressureLog</c>; it now reports the
    /// 16, so the sentence above is history rather than a live warning. <b>The shape argument stands and
    /// the drive still has it</b> — whether <c>restlessness</c> is directional-by-intent or wants a rest
    /// point like <c>trade</c> is an open ruling, not an oversight.</para>
    ///
    /// <para><c>purse</c> keeps that shape on purpose (the adventurers' purse <i>should</i> drain — it is what
    /// walks them to the board). <c>trade</c> cannot, because it is the drive the supply chain exists to
    /// trade against, so it needs somewhere to stand. A restoring force gives it one, and gives the bank a
    /// real budget: a sustained <c>B</c>/day of authored trade effects moves the rest point to
    /// <c>rest + B/TradePullToRest</c> and pegs only past <c>B = TradePullToRest × (1 - rest)</c>
    /// (≈ <c>+0.115/day</c>), where the old shape pegged at <i>any</i> sustained surplus at all.</para>
    /// </summary>
    public static double BaseDaily(string drive, string mode, double current,
                                   double heartTarget = HeartDefaultTarget) => drive switch
    {
        "purse" => mode == "work" ? PurseAtWork : PurseIdle,
        "trade" => (TradeRestFor(mode) - current) * TradePullToRest,
        "heart" => (heartTarget - current) * HeartPullToTarget,
        "restlessness" => mode is "work" or "haunt" ? RestEngaged : RestAtRest,
        _ => 0.0,
    };

    /// <summary>Where <c>trade</c> is pulled while a townee is in this mode. Working pulls toward
    /// <see cref="TradeRestAtWork"/>, everything else toward <see cref="TradeRestIdle"/>; a day mixing
    /// both rests at the time-weighted blend, which is what makes a dayplan's work slots mean something.</summary>
    private static double TradeRestFor(string mode) => mode == "work" ? TradeRestAtWork : TradeRestIdle;

    /// <summary>
    /// Where this townee's <c>heart</c> rests: the mean of every <c>pressure_targets.heart</c> their traits
    /// author, or <see cref="HeartDefaultTarget"/> if none does.
    ///
    /// <para><b>Why a target and not a rate.</b> <c>heart</c> is a restoring force, so scaling its rate only
    /// changes how fast it converges on the target — never where. <c>cheerful ×1.1</c> and <c>gruff ×0.85</c>
    /// were therefore <i>indistinguishable in the limit</i>: both landed on 0.5, one slightly sooner. But
    /// "cheerful" is a claim about where a person rests, not how briskly they get there, so no rate could
    /// ever have expressed it. This is the mechanism that can.</para>
    ///
    /// <para><b>The mean, on purpose.</b> Two traits that both name a rest point are two claims about the
    /// same thing, and averaging is the only combining rule here that is order-independent — "last wins"
    /// would make the day-hash depend on the order traits happen to appear in a townee's JSON array, which
    /// is a determinism hazard wearing a shrug.</para>
    /// </summary>
    public static double HeartTarget(Town town, Townee t)
    {
        double sum = 0.0;
        int n = 0;
        foreach (var traitId in t.Traits)
            if (town.TraitById.TryGetValue(traitId, out var trait)
                && trait.PressureTargets.TryGetValue("heart", out var target))
            {
                sum += target;
                n++;
            }
        return n == 0 ? HeartDefaultTarget : sum / n;
    }

    /// <summary>
    /// The trait scalar for this drive <b>in the direction it is currently moving</b>.
    ///
    /// <para><paramref name="perDay"/> is the already-signed base drift, and that sign is the whole point:
    /// multiplication preserves it, so the old single-scalar form could only scale magnitude. Picking
    /// <c>gain</c> vs <c>decay</c> off the sign is what lets <c>wanderlust</c> mean "gets restless faster
    /// <i>and</i> settles slower" instead of "moves 30% more, whichever way they happened to be going".</para>
    ///
    /// <para>Still a product across traits, so mods stack exactly as before; a bare authored number sets both
    /// halves to the same value and is therefore bit-identical to the old behaviour.</para>
    /// </summary>
    private static double TraitRateMod(Town town, Townee t, string drive, double perDay)
    {
        double mod = 1.0;
        foreach (var traitId in t.Traits)
            if (town.TraitById.TryGetValue(traitId, out var trait)
                && trait.PressureRateMods.TryGetValue(drive, out var m))
                mod *= perDay >= 0 ? m.Gain : m.Decay;
        return mod;
    }
}

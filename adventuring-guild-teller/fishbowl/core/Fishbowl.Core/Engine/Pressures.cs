using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// L2 Pressures (FBS.2/FBS.5, distilled): slow scalar drives drift by rule each slot.
/// Minutes-scaled so the drift is tick-granularity-independent (appendix MUA.M1). Drives
/// are fuel — they never pick actions; they cross thresholds that let storylets fire, and
/// the north-star is a fragile equilibrium (MUA.J10/Q7): motion enough to feed a chronicle,
/// not so much it fires constantly. Directed regard changes only through storylet effects in
/// v0 (a deliberate MUA.Q3 choice — it keeps authored tensions from washing out).
/// </summary>
public static class Pressures
{
    // Per-day target deltas by context, before config + trait scaling. Tuned against the
    // golden day; every constant is here (no magic-number sprawl — appendix MUA.N6).
    private const double HeartPullToMid = 0.20;    // heart regresses toward 0.5 at this fraction/day
    private const double PurseAtWork = 0.05;
    private const double PurseIdle = -0.03;
    private const double TradeDepletion = -0.11;   // stock/goodwill depletes daily; events replenish
    private const double RestAtRest = 0.06;        // restlessness accrues at home/asleep
    private const double RestEngaged = -0.10;      // and burns off at work or a haunt

    public static void DriftSlot(World world, int slot)
    {
        double perSlot = world.MinutesPerSlot / 1440.0;
        foreach (var t in world.Townees)
        {
            if (t.Away) continue; // away townees are off-sim until they return
            string mode = t.Mode.Length > slot ? t.Mode[slot] : "home";
            foreach (var drive in Town.Drives)
            {
                double perDay = BaseDaily(drive, mode, t.Pressure(drive));
                double rate = world.Config.PressureRates.TryGetValue(drive, out var r) ? r : 1.0;
                double traitMod = TraitRateMod(world.Town, t, drive);
                double next = t.Pressures[drive] + perDay * rate * traitMod * perSlot;
                t.Pressures[drive] = Math.Clamp(next, 0.0, 1.0);
            }
        }
    }

    private static double BaseDaily(string drive, string mode, double current) => drive switch
    {
        "purse" => mode == "work" ? PurseAtWork : PurseIdle,
        "trade" => TradeDepletion,
        "heart" => (0.5 - current) * HeartPullToMid,
        "restlessness" => mode is "work" or "haunt" ? RestEngaged : RestAtRest,
        _ => 0.0,
    };

    private static double TraitRateMod(Town town, Townee t, string drive)
    {
        double mod = 1.0;
        foreach (var traitId in t.Traits)
            if (town.TraitById.TryGetValue(traitId, out var trait)
                && trait.PressureRateMods.TryGetValue(drive, out var m))
                mod *= m;
        return mod;
    }
}

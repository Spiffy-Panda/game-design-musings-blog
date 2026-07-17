namespace Fishbowl.Core.Engine;

/// <summary>
/// The outing phase machine (`PNO.M2`): <c>Daily ──take──▶ Outing ──resolve──▶ Cooldown ──restored──▶
/// Daily</c>. It takes a body out of town against a standing posting, walks it leg by leg through an
/// offscreen site, rolls the outcome when the track runs out, and cools the adventurer down before daily
/// life resumes. <b>This is the return path `Away` never had</b> — the one-way trapdoor is gone by
/// construction, because every take now has a resolve and a cooldown after it.
/// <para><b>Two entry points, at two moments, and the order is load-bearing:</b>
/// <list type="bullet">
/// <item><see cref="StepSlot"/> runs each slot, AFTER <c>Pressures.DriftSlot</c> and BEFORE
/// <c>StoryletEngine.RunSlot</c>, so site storylets evaluate against the current leg.</item>
/// <item><see cref="ResolveDay"/> runs at the day boundary, immediately BEFORE <c>Clockwork.ResolveDay</c>,
/// because clockwork picks a townee's block list from their phase — so the phase must already be settled.
/// It shares the exact slot `Board.ResolveDay` proved at M1 with zero RNG.</item>
/// </list></para>
/// <para><b>Determinism:</b> the one draw is <see cref="World.SubRngFor"/><c>("outings", posting.Id)</c> —
/// per-posting, cache-immune, so bank growth never re-rolls an unrelated outing and the reset that
/// <c>Clockwork.ResolveDay</c> does to the named streams cannot rewind it. No <c>DateTime</c>, no
/// <c>Guid</c>, no <c>HashCode.Combine</c>.</para>
/// </summary>
public static class Outings
{
    // Restlessness discharge (PNO.M2 ruling: BOTH bursts AND a tick-while-out). The tick-while-out lives
    // in Pressures (the "outing" mode burns restlessness); these are the two authored-moment bursts.
    // Accepting a job relieves the itch; finishing it relieves more. Both discharge (negative delta), and
    // together with the tick they legitimately empty the 14 restlessness ratchets the lint ledger cites —
    // legitimately, because authored effects do the work, not a bare mode-label side effect (the trap the
    // spec's open question named).
    private const double RestlessnessTakeBurst = -0.15;
    private const double RestlessnessResolveBurst = -0.25;

    /// <summary>
    /// Take a standing posting off the board and leave town on it. Called by the <c>take</c> storylet
    /// effect. Returns the new outing, or null if it cannot start — the taker is not living a daily day,
    /// the posting is gone or already taken, or it is an <c>errand</c> with no site (an errand is handled
    /// in town by a neighbour; it is not an outing).
    /// </summary>
    public static Outing? Take(World world, string takerId, string postingId, int day)
    {
        if (!world.TowneeById.TryGetValue(takerId, out var taker)) return null;
        if (taker.Phase != Phase.Daily) return null;
        var posting = world.PostingById(postingId);
        if (posting is null || !posting.IsStanding) return null;
        if (string.IsNullOrWhiteSpace(posting.SiteId)) return null;

        var outing = new Outing
        {
            TakerId = takerId, PostingId = postingId, SiteId = posting.SiteId!, StartedDay = day,
        };
        taker.Phase = Phase.Outing;
        taker.Outing = outing;
        posting.State = PostingState.Taken;
        posting.TakerId = takerId;

        Discharge(taker, RestlessnessTakeBurst);
        return outing;
    }

    /// <summary>Per-slot leg advance. Each on-outing townee moves one slot deeper into their current leg;
    /// when a leg's (pace-scaled) slots are spent, they step to the next; when the last leg is spent, the
    /// track is complete and the outcome is rolled.</summary>
    public static void StepSlot(World world, int slot)
    {
        foreach (var t in world.Townees)
        {
            if (t.Phase != Phase.Outing || t.Outing is not { Complete: false } o) continue;
            var site = world.Town.SiteById(o.SiteId);
            if (site is null || site.Legs.Count == 0) { CompleteTrack(world, t, o, site); continue; }

            o.SlotsIntoLeg++;
            int legSlots = Math.Max(1, (int)Math.Round(site.Legs[o.LegIndex].Slots * world.Config.OutingPaceScale));
            if (o.SlotsIntoLeg >= legSlots)
            {
                o.LegIndex++;
                o.SlotsIntoLeg = 0;
                if (o.LegIndex >= site.Legs.Count) CompleteTrack(world, t, o, site);
            }
        }
    }

    /// <summary>The day boundary: settle phase transitions BEFORE clockwork reads the phase. A completed
    /// outing rolls into cooldown; a finished cooldown rolls back to daily life. Runs on the INCOMING day
    /// (<paramref name="day"/> is passed, not read — <c>World.Day</c> is already day+1 here), the same trap
    /// <c>Board.ResolveDay</c> documents.</summary>
    public static void ResolveDay(World world, int day)
    {
        foreach (var t in world.Townees)
        {
            if (t.Phase == Phase.Outing && t.Outing is { Complete: true })
            {
                t.Phase = Phase.Cooldown;
                t.CooldownUntilDay = day + Math.Max(0, world.Config.CooldownDays);
            }
            else if (t.Phase == Phase.Cooldown && day >= t.CooldownUntilDay)
            {
                t.Phase = Phase.Daily;
                t.Outing = null;
                t.CooldownUntilDay = 0;
            }
        }
    }

    /// <summary>The track ran out: roll the one hazard draw and settle the outcome. The reward on a
    /// <c>Carried</c> and the retrieval posting on a <c>Rout</c> are `PNO.M3`; M2 decides the outcome and
    /// records it so the loop closes.</summary>
    private static void CompleteTrack(World world, Townee t, Outing o, Model.SiteDto? site)
    {
        o.Complete = true;
        if (site is not null && o.LegIndex >= site.Legs.Count) o.LegIndex = site.Legs.Count - 1;

        double hazard = (site?.Legs.Sum(l => l.Hazard) ?? 0.0) * world.Config.OutingHazardScale;
        double roll = world.SubRngFor("outings", o.PostingId).NextDouble();
        o.Outcome = roll < hazard ? OutingOutcome.Rout : OutingOutcome.Carried;

        var posting = world.PostingById(o.PostingId);
        if (posting is not null) { posting.State = PostingState.Resolved; posting.ResolvedDay = world.Day; }

        Discharge(t, RestlessnessResolveBurst);
    }

    private static void Discharge(Townee t, double delta) =>
        t.Pressures["restlessness"] = Math.Clamp(t.Pressure("restlessness") + delta, 0.0, 1.0);
}

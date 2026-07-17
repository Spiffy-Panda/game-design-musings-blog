using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// The board outside the shut door. Files postings, ages them, expires them.
/// <para>
/// The board itself is not stored — it is <see cref="World.Board"/>, the standing subset of
/// <see cref="World.Postings"/> (<c>PNO.D1</c>: "a new data type for standing" reads as *the board is
/// data, not gameplay*). This class owns the transitions between states, and every one of them
/// appends a chronicle entry carrying its because-list, so the board is explainable on exactly the
/// same terms as everything else (<c>AGR.2</c>).
/// </para>
/// <para>
/// <b>Draws no RNG at <c>PNO.M1</c>.</b> Filing is storylet-gated and expiry is arithmetic, so the
/// board is deterministic without consuming a stream. That is deliberate: it lets M1 prove the
/// day-boundary insertion point before <c>PNO.M2</c> needs that slot to carry draws.
/// </para>
/// </summary>
public static class Board
{
    /// <summary>Synthetic storylet id for an expiry. Expiry is a board mechanism, not a rule, so it
    /// has no entry in the bank — safe because nothing ever resolves a <see cref="ChronicleEntry"/>'s
    /// <c>StoryletId</c> back against <c>Town.StoryletById</c>; it is only sorted, grouped and shown.</summary>
    public const string ExpiredStoryletId = "posting-expired";

    /// <summary>Where the paper hangs. The desk is shut on purpose in v0 — the board is outside it.</summary>
    public const string BoardPlaceId = "guildhall-steps";

    /// <summary>
    /// File a posting from an authored template. Called by the <c>post</c> storylet effect, so the
    /// predicate that decided the need is already the because-list of the filing event — this method
    /// only records the paper.
    /// </summary>
    /// <returns>The filed posting, or null if the template is unknown or this paper already exists.</returns>
    public static Posting? File(World world, string templateId, string requesterId, int day, int slot)
    {
        var tpl = world.Town.Postings.FirstOrDefault(p => p.Id == templateId);
        if (tpl is null) return null;

        // Content-derived, never a counter and never a Guid. Two reasons, both load-bearing:
        // Guid.NewGuid() is banned outright in Fishbowl.Core (it is process state wearing a hat), and
        // an index-derived id would renumber every later posting when the bank grows — which would
        // shift `SubRngFor("outings", posting.Id)` at PNO.M2 and silently re-roll unrelated outings.
        // (requester, template, day) is stable under bank growth by construction.
        string id = $"{templateId}#{requesterId}#d{day}";
        if (world.Postings.Any(p => p.Id == id)) return null;

        int span = Math.Max(1, (int)Math.Round(tpl.ExpiresDays * world.Config.PostingExpiryScale));

        var posting = new Posting
        {
            Id = id,
            TemplateId = tpl.Id,
            RequesterId = requesterId,
            Reach = tpl.Reach,
            SiteId = string.IsNullOrWhiteSpace(tpl.Site) ? null : tpl.Site,
            Tags = tpl.Tags.ToArray(),
            Reward = tpl.Reward,
            FiledDay = day,
            ExpiresDay = day + span,   // stands through (day + span - 1); gone at the dawn of day+span
        };
        world.Postings.Add(posting);
        return posting;
    }

    /// <summary>
    /// The day-boundary pass: age the board, expire what nobody took.
    /// <para>
    /// <b>Runs immediately before <see cref="Clockwork.ResolveDay"/>, and <paramref name="day"/> is a
    /// parameter on purpose.</b> By the time this is called, <c>Simulation.FinalizeDay</c> has already
    /// done <c>World.Day = day + 1</c> — so reading <c>world.Day</c> here would silently mean "the
    /// incoming day", which is right by accident rather than by intent. The caller pins it. The same
    /// trap has teeth at <c>PNO.M2</c>, where <c>Clockwork.ResolveDay</c>'s first act is
    /// <c>ResetDayStreams()</c>: anything drawn here from a cached <c>RngFor</c> stream would come
    /// from the finished day and then be thrown away by the reset. Use <c>SubRngFor</c>, which is
    /// cache-immune.
    /// </para>
    /// </summary>
    public static void ResolveDay(World world, int day)
    {
        // ToList: Expire mutates state that Board's own standing filter reads.
        foreach (var p in world.Postings.Where(p => p.IsStanding).ToList())
            if (day >= p.ExpiresDay)
                Expire(world, p, day);
    }

    private static void Expire(World world, Posting p, int day)
    {
        p.State = PostingState.Expired;

        string placeName = world.Town.PlaceById.TryGetValue(BoardPlaceId, out var pl)
            ? pl.Name : BoardPlaceId;
        string who = world.TowneeById.TryGetValue(p.RequesterId, out var t) ? t.Name : p.RequesterId;
        int stood = day - p.FiledDay;

        var entry = new ChronicleEntry
        {
            Day = day,
            Slot = 0,                       // dawn: the paper is gone when the town wakes up
            StoryletId = ExpiredStoryletId,
            Kind = "posting",
            PlaceId = BoardPlaceId,
            PlaceName = placeName,
            Participants = new List<string> { p.RequesterId },
            PostingIds = { p.Id },          // same field the `post` effect writes: one way to ask "which paper?"
            Tellability = 0.3,              // a notice coming down unanswered is small news, but it is news
        };

        // The because-list. `post` inherits one free from the storylet that fired it; expiry has no
        // storylet, so this is the first chronicle entry in the project built outside
        // StoryletEngine.BuildEntry. Same contract (AGR.2): the facts that made it happen, readable.
        entry.Because.Add(new BecauseFact("posting", p.Id));
        entry.Because.Add(new BecauseFact("filed", $"day {p.FiledDay}"));
        entry.Because.Add(new BecauseFact("stood", $"{stood} day{(stood == 1 ? "" : "s")}, untaken"));
        entry.Because.Add(new BecauseFact("expired", $"day {day} >= expires day {p.ExpiresDay}"));
        entry.Because.Add(new BecauseFact("posting_expiry_scale", $"{world.Config.PostingExpiryScale:0.##}×"));

        // The three registers. Note what is NOT here: `p.Id`. It is the composite
        // "template#requester#dN" — a debug handle, and it used to render into the report line, so a
        // player read "Petch's posting sedgewort-short#petch#d3 expired day 7". That breaks `AGT.10`
        // (the summary is gossip, not telemetry), which is the musing's central claim. The id is
        // still on this entry twice, in both the places a machine looks: the because-list above and
        // `PostingIds` below. The report register is the most factual of the three, not the most
        // internal — requester + filed-day identify the paper to a person without a slug.
        entry.Hearsay = $"A notice came down off the board at {placeName}, nobody having taken it.";
        entry.Gossip = $"{who}'s posting came down unanswered — {stood} day{(stood == 1 ? "" : "s")} on the board at {placeName} and not one taker.";
        entry.Report = $"{who}'s posting expired day {day} (filed day {p.FiledDay}, stood {stood}d, untaken) at {placeName}.";

        world.Chronicle.Add(entry);
    }
}

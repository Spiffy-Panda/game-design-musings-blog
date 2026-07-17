namespace Fishbowl.Core.Model;

/// <summary>
/// An authored posting <b>template</b> — the seed bank, not the board. Runtime postings are sim
/// state (<c>Engine.Posting</c>); this is what one gets filed <i>from</i>. Named "Template" rather
/// than <c>PostingDto</c> on purpose: the mapping is one-to-many and dating it as a plain DTO
/// invites someone to assume the file is the board.
/// <para>
/// Per <c>PNO.D1</c> (ruled 2026-07-16): a posting is a piece of paper on a board that somebody has
/// to take down. Never "quest" — that word is reserved for characters to say out loud.
/// </para>
/// </summary>
public sealed record PostingTemplateDto
{
    public string Id { get; init; } = "";

    /// <summary><c>posting</c> = the board, an adventurer, a site · <c>errand</c> = in-town, a
    /// neighbour handles it (the existing stock-runs-low → fetch-arranged path). Keeping both is
    /// deliberate: a town where every need becomes guild paperwork loses its texture, so the
    /// threshold between them is authored per rule rather than global.</summary>
    public string Reach { get; init; } = "posting";

    public string Requester { get; init; } = "";

    /// <summary>Where an outing against this posting goes. <b>Authored but unresolved at
    /// <c>PNO.M1</c></b> — sites land with the phase machine at <c>PNO.M2</c>, so this is validated
    /// for shape, not for existence, until then. Null/empty for an <c>errand</c>.</summary>
    public string? Site { get; init; }

    public List<string> Tags { get; init; } = new();

    /// <summary>Paid into the taker's purse on a <c>carried</c> outcome (PNO.M3).</summary>
    public double Reward { get; init; }

    /// <summary>Days the paper stands before it expires, scaled live by the
    /// <c>posting_expiry_scale</c> knob. Drives <c>PNO.Q2</c> — does paper move?</summary>
    public int ExpiresDays { get; init; } = 4;

    /// <summary>The three actionability registers, same contract as a storylet's — reused rather
    /// than re-declared so the dial renders a posting exactly as it renders anything else.</summary>
    public StoryletLinesDto Lines { get; init; } = new();
}

/// <summary>
/// <c>data/postings.json</c>. <b>Optional</b>, like <c>storylets/</c> and <c>golden/day1.json</c> —
/// absent means a posting-free town, which is precisely what the frozen golden fixture is
/// (<c>PNO.D2</c>). Making it required would force that fixture to carry an empty stub it has no
/// business knowing about, and would put a posting-shaped hole in the town that pins determinism.
/// </summary>
public sealed record PostingsFile
{
    public int Version { get; init; } = 1;
    public List<PostingTemplateDto> Postings { get; init; } = new();
}

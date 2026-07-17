using System.Text.Json.Serialization;

namespace Fishbowl.Core.Model;

/// <summary>One JSON-authored storylet rule (PLAN "Data", storylets/*.json).</summary>
public sealed record StoryletDto
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public StoryletPredicatesDto Predicates { get; init; } = new();
    public double Weight { get; init; } = 1.0;
    /// <summary>When true, the thinning gate (storylet_rate &lt; 1) never drops this event —
    /// the deterministic must-fire override (appendix MUA.J9).</summary>
    public bool MustFire { get; init; }
    public string Streams { get; init; } = "storylets";
    public List<StoryletEffectDto> Effects { get; init; } = new();
    public StoryletLinesDto Lines { get; init; } = new();

    /// <summary>
    /// The role→townee anchor. <b>Authored, not a hint:</b> a storylet with a <c>_binding</c> is
    /// anchored to exactly that cast and no other — <c>StoryletEngine.CandidateBindings</c>
    /// <c>yield break</c>s straight after it, so the predicate search is never reached. Every
    /// predicate still gates the firing each slot; only <i>who</i> is fixed.
    /// <para>Omitting it opts into the search path, which binds by co-presence over <b>every</b>
    /// townee — that is a different rule, not a fallback, and v0's whole bank is anchored (which is
    /// what makes the golden day reproduce exactly). Drop this key only if you mean "anyone".</para>
    /// <para><b>Cover every role in <c>copresent</c>.</b> A partial binding matches no candidate and
    /// the storylet <i>silently never fires</i>; <c>--lint</c>'s <c>partial-binding</c> check exists
    /// because <c>SchemaValidator</c> only proves the ids are real townees, not that they are all
    /// there.</para>
    /// <para><b>And you may only drop it from a 1- or 2-role rule</b>, because that is all the search
    /// path enumerates — see <see cref="StoryletPredicatesDto.Copresent"/>. Going unbound at 3 roles
    /// yields no candidate and kills the rule exactly as silently as a partial binding does; an
    /// anchored rule has no such limit, because it binds whatever it names. <c>--lint</c> reports both
    /// halves under <c>partial-binding</c> (<c>kind: partial</c> and <c>kind: unsearchable</c>), and
    /// its remediation text used to recommend this very mistake — "drop <c>_binding</c> entirely to opt
    /// into the search path", said to a 3-role rule — which is how the limit came to be written down
    /// here.</para>
    /// <para>(This comment used to say the opposite — "fixture hint… the live engine still binds by
    /// predicate search". It was the lone wrong description in the codebase, and it was in the file
    /// an author reads first.)</para>
    /// </summary>
    [JsonPropertyName("_binding")]
    public Dictionary<string, string>? Binding { get; init; }
}

public sealed record StoryletPredicatesDto
{
    /// <summary>Role names that must share a place this slot (e.g. ["A","B"] or ["A"]).
    /// <para><b>An <i>unbound</i> rule may have 1 or 2 of them, and no other number.</b>
    /// <c>StoryletEngine.CandidateBindings</c>' search path has an arm for <c>roles.Count == 1</c> and
    /// one for <c>== 2</c>, and no <c>else</c> — so an unbound rule at 0 or 3+ roles falls off the end,
    /// yields no candidate binding, and <b>silently never fires</b>. Nothing throws and nothing logs;
    /// the rule is simply never a candidate. This is a limit of the engine's search, not of the data
    /// model: an <i>anchored</i> rule (one with a <c>_binding</c>) may name as many roles as it likes,
    /// which is what both of the live town's 3-role rules do. Widening the search means touching
    /// <c>CandidateBindings</c>, and that changes what fires bank-wide.</para>
    /// <para><c>--lint</c>'s <c>partial-binding</c> check reports it as <c>kind: unsearchable</c>.</para></summary>
    public List<string> Copresent { get; init; } = new();

    /// <summary>
    /// Constrains <i>where</i> the rule may fire. <b>Absent means unconstrained</b> — which is why
    /// every pre-existing storylet is unaffected and the pinned golden hashes do not move.
    /// <para>The co-presence intersection already decides where a rule fires; this only filters that
    /// candidate set (see <c>StoryletEngine.CheckPredicates</c>). It exists because all 13 storylets
    /// render <c>{place}</c> in their prose and, until now, none could say anything about it — so
    /// <c>a-good-catch</c> ("the nets came up heavy off {place}") could fire at the Long Table, and
    /// <c>the-daily-grind</c> ("the stones at {place} turned all day") at Market Row. Solo storylets
    /// lied about where they were. A rule that names its scenery should be able to require it.</para>
    /// </summary>
    public PlacePredicateDto? Place { get; init; }

    /// <summary>Keyed "A-&gt;B": directed-regard conditions.</summary>
    public Dictionary<string, RegardPredicateDto> Regard { get; init; } = new();

    /// <summary>Keyed "B.purse": pressure thresholds.</summary>
    public Dictionary<string, PressurePredicateDto> Pressure { get; init; } = new();

    /// <summary>Keyed role → required trait id.</summary>
    public Dictionary<string, string> Trait { get; init; } = new();

    /// <summary>Keyed "A.departing_today": boolean sim flags.</summary>
    public Dictionary<string, bool> Flag { get; init; } = new();

    public ChronicleSinceDto? ChronicleSince { get; init; }

    public int CooldownDays { get; init; }
}

/// <summary>
/// Where a rule may fire. Both lists are whitelists and both are ANDed when both are authored
/// (<c>{"any":["market-row"],"kind":["market"]}</c> means "at Market Row, and it had better still be
/// a market"). An empty predicate (<c>{}</c>) constrains nothing — author no <c>place</c> key at all
/// rather than an empty one; the linter cannot tell the difference and neither can a reader.
/// <para><b>Validated at load</b> (<c>Data/SchemaValidator.cs</c>): an unknown place id or a kind no
/// place in this town has is a load-time error, not a rule that silently never fires. That is the
/// file's stated discipline — validate then run.</para>
/// </summary>
public sealed record PlacePredicateDto
{
    /// <summary>Place ids. The rule fires only at one of these.</summary>
    public List<string> Any { get; init; } = new();

    /// <summary>Place kinds (<c>inn</c>/<c>shop</c>/<c>workshop</c>/<c>market</c>/<c>work</c>/
    /// <c>landmark</c>/<c>home</c> in the towns that exist today). The vocabulary is not hardcoded —
    /// it is whatever <c>places.json</c> uses, so a town may invent a kind without a code change,
    /// and a typo is still caught because it matches nothing the town has.</summary>
    public List<string> Kind { get; init; } = new();
}

public sealed record RegardPredicateDto
{
    public string? Tag { get; init; }
    /// <summary>When true, the tag must be on the <i>reverse</i> edge (B→A) — "B owes A".</summary>
    public bool Flip { get; init; }
    public double? ScoreBelow { get; init; }
    public double? ScoreAbove { get; init; }
}

public sealed record PressurePredicateDto
{
    public double? Below { get; init; }
    public double? Above { get; init; }
}

public sealed record ChronicleSinceDto
{
    public int Days { get; init; }
    public string? Kind { get; init; }
}

/// <summary>
/// One effect. <b>Exactly one of Regard / Pressure / Post / Chronicle is meaningful per entry</b>, and
/// that is a contract, not a style note: <c>StoryletEngine.Apply</c> dispatches on an if/else-if chain,
/// so an entry setting two of them applies the first and silently drops the rest. Author one per entry.
/// <para><b>Enforced at load</b> (<c>Data/SchemaValidator.cs</c>): an entry setting two members is a
/// load-time error naming the storylet, the effect index, and the keys that collided. It used to be a
/// contract this comment merely asserted, which meant
/// <c>{"regard": "A-&gt;B", "delta": 0.1, "chronicle": true}</c> quietly bought a regard change and no
/// chronicle entry — a beat that happens and is never told. The union is enforced rather than the chain
/// widened into independent <c>if</c>s, because widening it would redefine this record as a
/// bag-of-effects and <c>StoryletEngine.ApplyMarks</c> contradicts that: it reads the single
/// <c>Effects.FirstOrDefault(e =&gt; e.Chronicle)</c>.</para>
/// <para><b>Delta / Tellability / Mark are modifiers, not union members</b> — they legitimately
/// co-occur with the member they qualify (<c>delta</c> with regard/pressure; <c>tellability</c> and
/// <c>mark</c> with chronicle).</para>
/// </summary>
public sealed record StoryletEffectDto
{
    public string? Regard { get; init; }    // "A->B"
    public string? Pressure { get; init; }  // "B.heart"
    public double Delta { get; init; }

    /// <summary>File a posting from an authored template (`PNO.M1`). The need that crossed a
    /// threshold is already this storylet's because-list, so the board inherits the explanation.</summary>
    public PostEffectDto? Post { get; init; }

    public bool Chronicle { get; init; }
    public double Tellability { get; init; }
    /// <summary>Roles whose bios get a dated one-liner appended (FB.8, behind the toggle).</summary>
    public List<string> Mark { get; init; } = new();
}

/// <summary>
/// The <c>post</c> effect's payload: <c>{"post": {"requester": "A", "template": "sedgewort-short"}}</c>.
/// <para><b>Both fields are validated at load</b> — <c>requester</c> must be one of the storylet's own
/// copresent roles (it is a role name, resolved through the binding, never a townee id) and
/// <c>template</c> must name a real entry in <c>postings.json</c>. Neither check is optional: an
/// unresolvable template makes <c>Board.File</c> return null and the filing evaporate, which is
/// precisely the failure this effect was shipped with.</para>
/// </summary>
public sealed record PostEffectDto
{
    /// <summary>A <i>role</i> from this storylet's <c>copresent</c> list — the townee who files.</summary>
    public string Requester { get; init; } = "";

    /// <summary>A posting template id from <c>postings.json</c>.</summary>
    public string Template { get; init; } = "";
}

public sealed record StoryletLinesDto
{
    public string Hearsay { get; init; } = "";
    public string Gossip { get; init; } = "";
    public string Report { get; init; } = "";
}

public sealed record GoldenDayFile
{
    public int Version { get; init; } = 1;
    public long Seed { get; init; }
    public int Day { get; init; }
    public List<GoldenBeat> ExpectedBeats { get; init; } = new();
}

public sealed record GoldenBeat
{
    public string Storylet { get; init; } = "";
    public List<string> Participants { get; init; } = new();
}

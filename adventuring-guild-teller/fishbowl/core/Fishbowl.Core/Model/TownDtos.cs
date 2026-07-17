using System.Text.Json.Serialization;

namespace Fishbowl.Core.Model;

// Parse targets for the authored data/ files. Field lists are the contract (PLAN "Data");
// snake_case ↔ PascalCase handled by the shared naming policy in DataJson.

public sealed record TowneesFile
{
    public int Version { get; init; } = 1;
    public List<TowneeDto> Townees { get; init; } = new();
}

public sealed record TowneeDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public bool Adventurer { get; init; }
    public List<string> Traits { get; init; } = new();
    public string Dayplan { get; init; } = "";
    public string Home { get; init; } = "";
    public string? Work { get; init; }
    /// <summary>Adventurer only: the day this townee leaves on expedition (sets departing_today
    /// that day, then Away after). Null = stays. The away-flag knob overrides this at runtime.</summary>
    public int? DepartsDay { get; init; }
    public List<string> Haunts { get; init; } = new();
    public Dictionary<string, double> Pressures { get; init; } = new();
    public Dictionary<string, RegardDto> Regard { get; init; } = new();
    public double TellerRegard { get; init; } = 0.5;
    public string Bio { get; init; } = "";
    public List<MarkDto> Marks { get; init; } = new();
}

public sealed record RegardDto
{
    public double Score { get; init; }
    public List<string> Tags { get; init; } = new();
}

public sealed record MarkDto
{
    public int Day { get; init; }
    public string Line { get; init; } = "";
}

public sealed record PlacesFile
{
    public int Version { get; init; } = 1;
    public List<PlaceDto> Places { get; init; } = new();
}

public sealed record PlaceDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public HoursDto Hours { get; init; } = new();
    public int Capacity { get; init; }
    public bool Board { get; init; }
    public bool Shut { get; init; }

    /// <summary>An off-screen place — an adventure site (`PNO.M2`), reached only by an outing, never part
    /// of daily-life routing. Defaulted false, so every authored place is unaffected and this is
    /// hash-neutral (PlaceDto never enters the day-hash). <b>Load-bearing in one spot:</b>
    /// <c>TownGenerator</c> must not house a generated townee here — a site is somewhere you go, not
    /// somewhere you live.</summary>
    public bool Offscreen { get; init; }
}

public sealed record HoursDto
{
    public int Open { get; init; }
    public int Close { get; init; }
}

public sealed record DayPlansFile
{
    public int Version { get; init; } = 1;
    public Dictionary<string, DayPlanDto> Dayplans { get; init; } = new();
}

public sealed record DayPlanDto
{
    public List<DayBlockDto> Weekday { get; init; } = new();

    /// <summary>The legacy bare-departure block list (`departs_day` / the `SetAway` knob) — place tokens
    /// resolve to "away", off-screen. Kept as a read alias for <see cref="Outing"/> (`PNO.D6`): a plan with
    /// only an <c>away</c> list still routes an outing-phase townee off-screen, which is exactly the
    /// bare-departure behaviour.</summary>
    public List<DayBlockDto>? Away { get; init; }

    /// <summary>The block list for <see cref="Engine.Phase.Outing"/> — a real trip. Its <c>site</c> place
    /// token resolves to the townee's active outing site, so the party is co-present there. Falls back to
    /// <see cref="Away"/> then <see cref="Weekday"/> when unauthored.</summary>
    public List<DayBlockDto>? Outing { get; init; }

    /// <summary>The block list for <see cref="Engine.Phase.Cooldown"/> — resting in town after a trip. Falls
    /// back to a shared <c>cooldown-default</c> plan, then <see cref="Weekday"/>.</summary>
    public List<DayBlockDto>? Cooldown { get; init; }
}

public sealed record DayBlockDto
{
    public int Start { get; init; }
    public int End { get; init; }
    public string Place { get; init; } = "";
    public string Activity { get; init; } = "";
    /// <summary>Optional roam set (courier): the townee is co-present with all listed
    /// places' occupants during this block. First entry is the anchor place shown in readouts.</summary>
    public List<string>? Roams { get; init; }
}

public sealed record TraitsFile
{
    public int Version { get; init; } = 1;
    public List<TraitDto> Traits { get; init; } = new();
}

public sealed record TraitDto
{
    public string Id { get; init; } = "";

    /// <summary>How fast a drive moves for this townee, <b>per direction</b>. See <see cref="RateModDto"/>:
    /// a bare number is still legal and still means <c>{gain: n, decay: n}</c> — today's meaning, exactly.</summary>
    public Dictionary<string, RateModDto> PressureRateMods { get; init; } = new();

    /// <summary>Where a restoring drive <b>rests</b> for this townee — a different claim from how fast it
    /// gets there, and the reason this field exists at all (see <see cref="Engine.Pressures.BaseDaily"/>).
    /// <para>Only drives in <see cref="Town.TargetedDrives"/> read a target; <see cref="Data.SchemaValidator"/>
    /// rejects the rest rather than letting an authored number sit here doing nothing.</para></summary>
    public Dictionary<string, double> PressureTargets { get; init; } = new();

    public Dictionary<string, double> StoryletWeightMods { get; init; } = new();
    public bool HearsayCarrier { get; init; }
}

/// <summary>
/// A per-direction rate scalar for one drive. <c>gain</c> scales the delta when the base drift is
/// <b>positive</b>, <c>decay</c> when it is <b>negative</b>.
///
/// <para><b>Why this is not one number.</b> <see cref="Engine.Pressures.BaseDaily"/> is already signed, and
/// multiplication preserves sign — so a single scalar can only ever scale a drift's <i>magnitude</i>,
/// never its direction. That made every trait direction-blind: <c>wanderlust ×1.3</c> scaled an engaged
/// townee's <c>restlessness</c> drift of <c>-0.10</c> to <c>-0.13</c>, i.e. it made a restless man
/// <b>settle faster</b>. Splitting the scalar in two is the smallest change that lets a trait mean what
/// its word means, and it stays a pure multiplier: still minutes-scaled, still stacking.</para>
///
/// <para><b>A bare number keeps today's meaning.</b> <c>"restlessness": 1.3</c> parses to
/// <c>{gain: 1.3, decay: 1.3}</c> — arithmetically identical to what the single scalar did — and is
/// flagged by <c>--lint</c>'s <c>legacy-rate-mods</c> as un-migrated. That is deliberate: migration is a
/// judgement about what a word means, so it must be made per trait by a human, never inferred. The
/// frozen golden fixture (PNO.D2) relies on this and keeps its bare numbers forever.</para>
/// </summary>
public sealed record RateModDto
{
    /// <summary>Scales the drift where the base rule pushes the drive <b>up</b>.</summary>
    public double Gain { get; init; } = 1.0;

    /// <summary>Scales the drift where the base rule pushes the drive <b>down</b>.</summary>
    public double Decay { get; init; } = 1.0;

    /// <summary>True when authored as a bare number rather than a <c>{gain, decay}</c> pair. Carried so
    /// <c>--lint</c> can name the un-migrated traits; never read by the engine, which sees only the two
    /// numbers, and identical either way.</summary>
    [JsonIgnore]
    public bool Legacy { get; init; }
}

public sealed record SimConfig
{
    public int Version { get; init; } = 1;
    public int SlotsPerDay { get; init; } = 48;
    public long Seed { get; init; } = 1123;
    public Dictionary<string, double> PressureRates { get; init; } = new();
    public double StoryletRate { get; init; } = 1.0;
    public double StoryletCooldownScale { get; init; } = 1.0;
    public double CopresenceBonus { get; init; } = 1.0;
    public bool HearsayRequired { get; init; } = true;
    public double Actionability { get; init; } = 0.5;
    public int SummaryLines { get; init; } = 5;

    /// <summary>
    /// <b>Rendering knob.</b> The multiplier a rule's score takes for each night inside
    /// <see cref="Engine.Summarizer.NoveltyWindow"/> on which it was already told. <c>1.0</c> disables
    /// fatigue entirely (and is exactly the pre-novelty fixed leaderboard); <c>0.0</c> makes one
    /// telling silence a rule for the whole window.
    /// <para>Defaults ON per the 2026-07-16 ruling. Off, the town says 29 distinct sentences in a
    /// fortnight and 23 rules that fire are never once told.</para>
    /// <para><b>Why 0.5 and not lower, when lower scores better.</b> Each telling halves a rule's
    /// claim on the summary — chosen so that <i>two</i> tellings drop the juiciest beat in the bank
    /// (0.90 with the carrier bump → 0.225) below the most mundane fresh one (0.25), which is the
    /// reach the term needs, stated as an inequality. Pushing further keeps buying variety, and the
    /// variety metric would applaud all the way to 0.0 — but tellability is an authored dial, and at
    /// 0.0 a rule told once is silenced outright, so the town narrates its dullest rule as readily as
    /// its best. Measured over 14 nights, rank correlation between authored tellability and times
    /// told falls 0.76 (off) → 0.50 (here) → 0.33 (at 0.0), while distinct sentences saturate at 51
    /// from 0.3 down: the last stretch spends authorial intent and buys nothing. 0.5 is the far side
    /// of the stated inequality, not the far side of the metric.</para>
    /// </summary>
    public double NoveltyDecay { get; init; } = 0.5;

    public bool BioMarksEnabled { get; init; } = true;

    // --- PNO.M1: the board. ---

    /// <summary>How readily needs become paper. <b>Read by no engine code — a dead knob</b> (verified
    /// 2026-07-17); volume lives in the authored bank, not here (`PNO.T1`). Kept so the report/projection
    /// keep serializing it, and flagged pending a ruling.</summary>
    public double PostingRate { get; init; } = 1.0;

    /// <summary>Scales each posting's authored `expires_days`, baked into <c>ExpiresDay</c> at filing time.
    /// Drives `PNO.Q2` (does paper move?): if postings rot on the board, self-selection is too shy; if
    /// they're gone within a slot, the board isn't a board.</summary>
    public double PostingExpiryScale { get; init; } = 1.0;

    // --- PNO.M2: outings. ---

    /// <summary>Scales every leg's hazard weight. <c>0</c> = nobody is ever routed; <c>3</c> = the fen eats
    /// everyone. The A/B lever for `PNO.Q4` (does the rout loop land?) and the Corvo fixture's second half.</summary>
    public double OutingHazardScale { get; init; } = 1.0;

    /// <summary>Scales every leg's slot duration — how long a trip takes. Drives `PNO.Q5` (cooldown a beat
    /// or a lull?) alongside <see cref="CooldownDays"/>.</summary>
    public double OutingPaceScale { get; init; } = 1.0;

    /// <summary>Days a returned adventurer rests before re-entering daily life.</summary>
    public int CooldownDays { get; init; } = 2;

    /// <summary>The `AGT.12` loop, toggleable for A/B: a rout files a retrieval posting for the lost gear
    /// (`PNO.M3`). Read at resolve time.</summary>
    public bool RoutSeedsRetrieval { get; init; } = true;

    /// <summary>Trait/pressure weighting on who self-selects a posting (`PNO.D4`), applied inside `PNO`'s own
    /// take path — never by retrofitting the dead `storylet_weight_mods` hook.</summary>
    public double SelfSelectBias { get; init; } = 1.0;
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorningQueue.Core;

// The typed domain model for The Morning Queue's data banks and visits.
//
// Design notes:
//  * Every type tolerates unknown JSON fields (System.Text.Json ignores members it does
//    not recognise by default) — the banks are hand-authored and grow over time, and an
//    unrecognised field must never throw.
//  * Nullable reference types are on; an absent field deserialises to null, which the
//    Validator reads as "missing" exactly like the GDScript `dict.has(key)` checks did.
//  * `owed` is a JsonElement (not a number) so the "owed is not a number" validator check
//    can inspect the JSON value kind, mirroring GDScript's `is int or is float` guard.

// ---- references.json: the Book ---------------------------------------------------

public sealed class BookItem
{
    public string? Category { get; set; }
    public string[]? Tells { get; set; }
    public string? Glass { get; set; }
    public string? ForgeryGlass { get; set; }
    public string[]? ForgeryTells { get; set; }
    public string? SureTell { get; set; }
    public string[]? ConfusableWith { get; set; }
    public string? Unit { get; set; }
    public bool Hazard { get; set; }
}

// ---- references.json: Postings (gates + standing orders) --------------------------

public sealed class Accept
{
    public int Min { get; set; }
    public int Max { get; set; }
    public string? Unit { get; set; }
}

public sealed class Total
{
    public int Needed { get; set; }
    public string? Unit { get; set; }
}

public sealed class Posting
{
    public string? Type { get; set; }
    public string? Item { get; set; }

    // The standing-order limit is an honest accept|total union: exactly one is present on a
    // standing_order posting, and neither on a plain gate/quest posting.
    public Accept? Accept { get; set; }
    public Total? Total { get; set; }

    public string? RankMin { get; set; }
    public string[]? WardRequired { get; set; }
    public string[]? Requires { get; set; }
    public string? Target { get; set; }
    public string? ProofItem { get; set; }
    public string? Location { get; set; }
    public string? AssignedTo { get; set; }
    public string? Status { get; set; }
    public string? Token { get; set; }

    [JsonIgnore] public bool HasLimit => Accept != null || Total != null;
}

// ---- references.json: ciphers, archive, drops, roster, misc -----------------------

public sealed class Cipher
{
    public string? Mark { get; set; }
    public string? Seal { get; set; }
    public string? Glass { get; set; }
}

// The Completion Archive holds two shapes under one map: completion tokens
// ({seal, posting, assigned_to}) and logbooks ({entries, distinct_seals, all_owner}).
// Modelled as a union with every field optional so both parse without throwing.
public sealed class ArchiveEntry
{
    public string? Seal { get; set; }
    public string? Posting { get; set; }
    public string? AssignedTo { get; set; }
    public int? Entries { get; set; }
    public int? DistinctSeals { get; set; }
    public string? AllOwner { get; set; }
}

public sealed class DropEntry
{
    public bool IsDrop { get; set; }
    public int Floor { get; set; }
    public string? Season { get; set; }
    public int BaseBounty { get; set; }
}

public sealed class RankCard
{
    public string? Rank { get; set; }
    public string? Dues { get; set; }
}

public sealed class RosterParty
{
    public string? Id { get; set; }
    public string? Lead { get; set; }
    public string? Rank { get; set; }
    public int ReachFloor { get; set; }
    public string[]? Wards { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
}

public sealed class Roster
{
    public List<RosterParty> Parties { get; set; } = new();
}

public sealed class Season
{
    public string? Current { get; set; }
    public string[]? Wheel { get; set; }
}

public sealed class Payout
{
    public double DepthRate { get; set; }
    public int InSeasonPremium { get; set; }
}

// ---- directories -----------------------------------------------------------------

public sealed class Townee
{
    public string? Name { get; set; }
    public string? Profession { get; set; }
    public string? Dues { get; set; }
    public JsonElement? Owed { get; set; }
    public string[]? Owns { get; set; }
    public string? Blurb { get; set; }
}

public sealed class Logbook
{
    public string? ArchiveId { get; set; }
    public int Entries { get; set; }
    public int DistinctSeals { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}

public sealed class Adventurer
{
    public string? Name { get; set; }
    public string? Profession { get; set; }
    public string? Rank { get; set; }
    public string? Dues { get; set; }
    public JsonElement? Owed { get; set; }
    public string? Chapter { get; set; }
    public string[]? Wards { get; set; }
    public Logbook? Logbook { get; set; }
    public string? Blurb { get; set; }
}

// ---- generation.json -------------------------------------------------------------

public sealed class PerTaskSpec
{
    public string? ActorPool { get; set; }
    public string[]? FailureAxes { get; set; }
}

public sealed class SeasonSchedule
{
    public string[]? Wheel { get; set; }
    // by_day may carry a "_note" string alongside numeric-keyed seasons; kept as JsonElement
    // values and filtered/read by the validator, mirroring GDScript's underscore skip.
    public Dictionary<string, JsonElement>? ByDay { get; set; }
}

public sealed class GenerationConfig
{
    public Dictionary<string, JsonElement>? TaskWeights { get; set; }
    public JsonElement? InvalidRate { get; set; }
    public Dictionary<string, JsonElement>? InvalidRateByDay { get; set; }
    public Dictionary<string, PerTaskSpec>? PerTask { get; set; }
    public SeasonSchedule? SeasonSchedule { get; set; }
}

// ---- visitors.json: a Visit ------------------------------------------------------

public sealed class Claim
{
    public string? Summary { get; set; }
    public Dictionary<string, JsonElement>? Asserts { get; set; }
}

public sealed class Failure
{
    public string? Axis { get; set; }
    public string? Reason { get; set; }
}

public sealed class Truth
{
    public bool Valid { get; set; }
    public string? Stamp { get; set; }
    public string? Binary { get; set; }
    public Failure? Failure { get; set; }

    // Optionals a visit may carry (honestly modelled — most visits carry none):
    public JsonElement? RosterWrite { get; set; }
    public JsonElement? Quote { get; set; }
    public JsonElement? FlagFloor { get; set; }
}

public sealed class Check
{
    public string? Consult { get; set; }
    public string? Entry { get; set; }
    public string[]? Compare { get; set; }
    public string? Against { get; set; }
    public string? Result { get; set; }
}

public sealed class GlassReading
{
    public string? Reading { get; set; }
    public bool Relevant { get; set; }
}

public sealed class ScaleReading
{
    public string? Reading { get; set; }
    public double? Amount { get; set; }
    public string? Unit { get; set; }
    public bool Relevant { get; set; }

    // Derived at compose time (see Deriver): within|over|under|meets|no_order. Never
    // authored in the source data; the derive pass annotates it.
    public string? Verdict { get; set; }
}

public sealed class Inspections
{
    public GlassReading? Glass { get; set; }
    public ScaleReading? Scale { get; set; }
}

public sealed class Visit
{
    public string? Id { get; set; }
    public int Order { get; set; }
    public string? Name { get; set; }
    public string? Affiliation { get; set; }
    public string? Profession { get; set; }
    public string? TaskType { get; set; }
    public Claim? Claim { get; set; }
    public Truth? Truth { get; set; }
    public List<Check>? Checks { get; set; }
    public Inspections? Inspections { get; set; }
    public string? PlayerStory { get; set; }
    public string? Notes { get; set; }
    public JsonElement? Portrait { get; set; }

    // Preserve any unrecognised fields on a faithful parse->serialize->parse round trip.
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

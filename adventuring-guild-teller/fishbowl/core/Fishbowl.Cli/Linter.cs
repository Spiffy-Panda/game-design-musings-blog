using System.Text.Json;
using System.Text.Json.Nodes;
using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Json;
using Fishbowl.Core.Model;

namespace Fishbowl.Cli;

/// <summary>
/// <c>--lint</c> — the content-health instrument. Every check here corresponds to a defect the first
/// town actually shipped, which is the only reason each one exists: this is a list of mistakes that
/// were made, not a list of mistakes that could be imagined.
///
/// <para><b>It runs the real engine.</b> Itineraries and co-presence come from
/// <see cref="Clockwork.ResolveDay"/>, drift comes from <see cref="Pressures.BaseDaily"/> /
/// <see cref="Pressures.NetDaily"/>, hearsay carriage comes from
/// <see cref="Summarizer.WouldBeCarried"/>, the place and pressure predicates from
/// <see cref="StoryletEngine.PlaceMatches"/> / <see cref="StoryletEngine.PressureMatches"/>,
/// cooldown eligibility from <see cref="StoryletEngine.OnCooldown"/>, and <c>latch-die</c> and
/// <c>ratchets</c> read a whole <see cref="Simulation"/> — never a local copy. A linter that
/// reimplements the system it audits will, given time, audit its own fiction instead. <b>Three
/// checks have now proved it, and the third is the one worth remembering:</b>
/// <list type="bullet">
/// <item><c>stranded-beats</c> hand-modelled one of hearsay-lite's three carriage clauses and
/// reported — at error class — that beats were dead while the summary was printing them.</item>
/// <item><c>latch-die</c> predicted from base drift with the storylet effects switched off, and got
/// 7 of 7 of its "can never fire again" verdicts backwards, because effects outweigh drift by
/// 1–15× here.</item>
/// <item><c>ratchets</c> asked whether <i>some mode</i> pushes a drive up and <i>some mode</i> pushes
/// it down, and called anything that answered yes bidirectional. That is a sound question; it is not
/// the question. <c>restlessness</c> answers yes (<c>-0.10</c> engaged, <c>+0.06</c> at rest) and is
/// nonetheless one-way for 16 of the live town's 18 townees, because the sign that matters belongs to
/// the <i>sum over a real day</i> and the break-even sits at 18 engaged slots of 48 that nobody in
/// the town is within four slots of. So the check reported 3 findings — the 3 adventurer purses,
/// which are by design — and was blind to 24 real ones. It now weights by the day actually lived
/// (<see cref="Pressures.NetDaily"/>) and, like the two above it, reads the outcome out of the
/// engine's own <see cref="World.PressureLog"/> instead of predicting one.</item>
/// </list>
///
/// <para><b>The part that should sting.</b> <c>Pressures.cs</c> predicted this bug in writing. When it
/// rejected a mode-constant shape for <c>trade</c> it said such a shape "would only have relocated the
/// ratchet… while silencing <c>--lint</c>, which tests the per-mode sign rather than the net drift: a
/// green gate over a live countdown, which is strictly worse than the bug." <c>restlessness</c> is that
/// exact shape, and it was already shipped when the sentence was written. The argument that stopped the
/// bug in one drive did not think to look at the next one. Two checks were fixed by calling the engine;
/// the third needed someone to notice that a green light was the finding.</para>
///
/// <para><b>It degrades.</b> A town that fails <see cref="SchemaValidator"/> is reported as findings
/// rather than a stack trace, and the checks that need a resolved world are skipped with a note. A
/// linter that only reads towns which already load is useless exactly when an author needs it —
/// mid-rebuild, with the data half-swapped.</para>
///
/// <para><b>And it can be told "yes, we know" — once, in writing.</b> See
/// <see cref="LoadAcceptances"/>. A ruled defect that ships unfixed would otherwise pin the gate red
/// forever, and <b>a permanently-red gate is a disabled gate</b>: it teaches its reader to ignore the
/// exit code, which is the same defect class as every check above that had to be repaired. The ledger
/// separates <i>"is this a defect?"</i> (still yes — nothing is reclassified, the proof still prints in
/// full every run) from <i>"is this news?"</i> (no — a human ruled on it, and said why, and said
/// where).</para>
/// </summary>
public static class Linter
{
    /// <summary>Checks whose findings mean "this content is provably dead or provably lying". These
    /// set the exit code, so a content author can gate on them.
    /// <para><c>load</c> and <c>lint-aborted</c> are here because the gate must never pass a town it
    /// could not read. Both were warn-class, and both broke the contract in the same direction: a
    /// malformed town emitted <c>"ok": true, "errors": 0</c> while <c>Run</c> hand-returned 1, and a
    /// town the engine refused to resolve exited <b>0</b> with half the checks silently skipped —
    /// a green gate over content nobody had audited.</para>
    /// <para>Membership sets a check's <i>default</i> class, not its verdict on every finding: a
    /// check that can tell a proof from a by-design case may downgrade an individual finding via
    /// <see cref="Finding.Class"/>. <c>ghost-cast</c> and <c>ratchets</c> are the two that do.</para></summary>
    private static readonly HashSet<string> ErrorChecks = new(StringComparer.Ordinal)
    {
        "schema", "ghost-cast", "beats-over-sleepers", "unreachable-haunts",
        "unheld-trait", "unreachable-posting", "line-tokens", "partial-binding",
        "load", "lint-aborted",
        // A beat whose every fire opportunity is uncarriable is provably dead content: it burns a
        // slot and can never reach a summary. That is the same class as an unreachable posting, not
        // a style note — so it sets the exit code. Split out of `hearsay-dead-zones` (which stays a
        // warning) because that check's other findings are soft measures, not proofs.
        "stranded-beats",
        // Only the `ratchet` kind gates — a drive observed against a clamp that NO authored effect
        // pushes back on, on a shape that has no interior fixed point to rest at. That conjunction is
        // a proof (see Ratchets), and it is the one this check spent its whole life unable to see. The
        // other four kinds downgrade themselves to warn via Finding.Class: `by-design` because the
        // engine documents the drain as intended, `outgunned`/`quiet` because the bank does reach the
        // drive and a retune could win, and `unwritten` because an empty bank is not a broken drive.
        "ratchets",
        // The acceptance ledger auditing itself. Default error is the fail-safe default — a kind added
        // later and left unclassified gates rather than whispers — and two of the three kinds keep it:
        // `unreadable` (the `load` argument exactly: the gate must never pass on a ledger it could not
        // read, because it cannot know what the ledger would have forgiven) and `malformed` (an entry
        // is missing a field, so it forgives nothing and someone believes it does). `unmatched`
        // downgrades to warn via Finding.Class — see UnmatchedAcceptances for why that one must not
        // gate.
        "lint-accepted",
    };

    /// <summary>Checks no ledger may ever accept, at any subject.
    /// <para>Both are the <c>ErrorChecks</c> note's own argument, one step on: a gate must never pass a
    /// town it could not read, and it must equally never pass one <i>on the strength of an acceptance
    /// it cannot verify</i>. These two findings mean the report itself is untrustworthy —
    /// <c>load</c> says there are no findings to accept, and <c>lint-aborted</c> says an unknown subset
    /// of the checks never ran, including the very ones whose findings a ledger entry might name. An
    /// acceptance of either is self-undermining: it would forgive a defect on the evidence of a run
    /// that did not happen.</para>
    /// <para><c>lint-accepted</c> closes the loop — the ledger cannot forgive its own defects. If the
    /// file will not parse there is no ledger to forgive with, and an entry that could excuse its own
    /// malformation would excuse anything.</para></summary>
    private static readonly HashSet<string> UnacceptableChecks = new(StringComparer.Ordinal)
    {
        "load", "lint-aborted", "lint-accepted",
    };

    /// <param name="Class">Per-finding severity override, or null — the normal case — to take the
    /// class from <see cref="ErrorChecks"/>. This exists because a check's subject matter can be
    /// mixed: <c>ghost-cast</c> spans "nobody wrote for this townee" and "this townee is authored to
    /// have left", and those are not the same finding at two volumes. Set it only where the check
    /// can <i>prove</i> the softer reading from authored data; a guess belongs in the detail text,
    /// not in the gate.</param>
    /// <param name="Accepted">The ruling that accepted this finding, or null — the normal case.
    /// <para><b>This is a second axis, not a quieter class.</b> <see cref="Severity"/> deliberately
    /// does not read it: an accepted <c>ratchet</c> is still error-class, still a proof, still printed
    /// in full. Class answers <i>"is this a defect?"</i>; <see cref="Gates"/> answers <i>"is this
    /// news?"</i>. Collapsing the two — letting an acceptance rewrite the class — would be the
    /// reclassification this file has twice paid to undo, and it would lose the distinction that makes
    /// the ledger honest: an accepted finding is one a human looked at and ruled on, not one the
    /// linter decided was fine.</para></param>
    private sealed record Finding(string Check, string Subject, string Detail, JsonObject Data,
        string? Class = null, Acceptance? Accepted = null)
    {
        public string Severity => Class ?? (ErrorChecks.Contains(Check) ? "error" : "warn");

        /// <summary>The gate's whole question, and the only input to the exit code.</summary>
        public bool Gates => Severity == "error" && Accepted is null;
    }

    /// <summary>Days of real simulation <see cref="Observe"/> runs, and <see cref="LatchOrDie"/> and
    /// <see cref="Ratchets"/> read.
    /// <para>56, because 56 is the length that caught the check's predecessor lying: at 14 days —
    /// the old prediction horizon — <c>the-inn-is-well-found</c> has only had 7 chances to fire and a
    /// slow rule is indistinguishable from a dead one. Eight weeks is long enough for a 2-day
    /// cooldown to show a cap and for a genuinely dead rule to stay quiet through it.</para></summary>
    private const int ObservedDays = 56;

    /// <summary>The window at the end of the run that <see cref="Ratchets"/> judges a drive on.
    /// <para>A fortnight, and it is the whole reason the check can say anything: a drive that spends
    /// days 1–40 doing its job and days 42–56 pinned to a clamp is a ratchet, and a verdict taken over
    /// the whole run would average the two into "it moved". The tail asks the only question that
    /// matters — <i>where did it end up, and is it still moving there?</i> — and it is the same
    /// fortnight <see cref="DeadTailDays"/> gives a rule to prove it has not latched shut.</para></summary>
    private const int TailDays = 14;

    /// <summary>Eligible-but-silent days after a rule's last firing before <see cref="LatchOrDie"/>
    /// calls it dead. A fortnight of unused chances — short enough to catch a rule that latched shut
    /// on day 3, long enough that a rule pacing itself through a quiet stretch is not slandered.</summary>
    private const int DeadTailDays = 14;

    public static int Run(string townDir, string? reportPath, bool jsonToStdout)
    {
        var findings = new List<Finding>();

        // The ledger first, and never through TownLoader — see LoadAcceptances. Its own findings are
        // ordinary findings, so a broken ledger is reported in the same shape as a broken town.
        var (ledger, ledgerProblems) = LoadAcceptances(townDir);
        findings.AddRange(ledgerProblems);

        Town town;
        try
        {
            town = TownLoader.LoadUnvalidated(townDir);
        }
        catch (Exception ex)
        {
            // No town at all: malformed JSON, or a required file missing. Nothing to lint, but say so
            // in the same shape as everything else so a caller's parser never has two cases.
            findings.Add(new Finding("load", townDir, ex.Message, new JsonObject { ["error"] = ex.GetType().Name }));
            return Emit(findings, townDir, reportPath, jsonToStdout, worldResolved: false, ledger);
        }

        // Schema first, and non-fatally. These are the findings that matter most during a rebuild.
        foreach (var err in SchemaValidator.Collect(town))
            findings.Add(new Finding("schema", "town", err, new JsonObject()));

        // Checks that need nothing but the authored data. These stay honest even when the town is
        // mid-rebuild and its references dangle.
        findings.AddRange(LineTokens(town));
        findings.AddRange(PartialBindings(town));
        findings.AddRange(DeadTraits(town));
        findings.AddRange(UnreachablePostings(town));
        findings.AddRange(NonDrivingRegard(town));
        findings.AddRange(TellabilityHistogram(town));
        findings.AddRange(UnreachableHaunts(town));

        // Checks that need the clockwork to have resolved. If the town is broken badly enough that
        // the engine won't run, report that and keep the static findings rather than dying.
        bool worldResolved = false;
        try
        {
            var day1 = ResolveAt(town, 1);
            var later = ResolveAt(town, 3);
            var pairs = AwakeCopresentSlots(day1);

            findings.AddRange(GhostCast(town, later));
            findings.AddRange(BeatsOverSleepers(town, day1));
            findings.AddRange(UnconvenedBonds(town, day1, pairs));
            findings.AddRange(SilentPairs(town, day1, pairs));
            findings.AddRange(StrandedBeats(town, day1));
            findings.AddRange(HearsayDeadZones(town, day1));

            // One run, two readers. `latch-die` asks what fired and `ratchets` asks what the drives
            // did, and those are two questions about the same 56 days — so they are answered from one
            // Simulation rather than two. Not only for the seconds: two runs are two chances to
            // describe different worlds, and a report whose fire counts and whose pressure curves came
            // from different runs would be internally unfalsifiable at exactly the moment someone
            // needed to cross-reference them (which is the whole point of `outgunned`, below — it
            // divides one check's fire count into the other check's drift).
            var observed = Observe(town);
            findings.AddRange(LatchOrDie(town, observed));
            findings.AddRange(Ratchets(town, observed));
            worldResolved = true;
        }
        catch (Exception ex)
        {
            findings.Add(new Finding("lint-aborted", townDir,
                $"the engine could not resolve this town, so the occupancy- and drift-dependent checks "
                + $"were skipped: {ex.Message}",
                new JsonObject { ["error"] = ex.GetType().Name }));
        }

        return Emit(findings, townDir, reportPath, jsonToStdout, worldResolved, ledger);
    }

    // --- the acceptance ledger ----------------------------------------------------------

    /// <summary>The per-town ledger, in the town's own directory.</summary>
    private const string AcceptedFile = "lint-accepted.json";

    /// <param name="Entry">This entry's index in the file's <c>accepted</c> array — so a finding can
    /// point a reader at the line that forgave it, and so two entries that happen to carry identical
    /// prose still group separately in the output.</param>
    /// <param name="Kind">The finding's <c>data.kind</c>, or null for a check that emits none. Part of
    /// the key, and matched with <b>null-equals-null</b>: an entry that omits <c>kind</c> matches only
    /// findings that have none. See <see cref="MatchOf"/> for why that direction is the safe one.</param>
    private sealed record Acceptance(int Entry, string Check, string? Kind, List<string> Subjects,
        string Reason, string Ruling, string? Date);

    /// <summary>
    /// Reads <c>&lt;townDir&gt;/lint-accepted.json</c> — <b>the ledger of defects a human has ruled on
    /// and chosen to ship.</b>
    ///
    /// <para><b>Why this exists.</b> On 2026-07-16 Panda ruled that the live town's 14 <c>restlessness</c>
    /// ratchets ship unfixed: the buildup is directional <i>by design</i>, and <c>PNO.M2</c> (outings) is
    /// where it finally gets somewhere to discharge, so fixing the drive now would pre-empt the milestone
    /// that gives it meaning. The findings are still proofs — <see cref="Ratchets"/> is right and was
    /// cross-validated — so the gate was red, permanently, on a defect nobody intended to fix. <b>A tool
    /// that always exits 1 teaches its reader to ignore the exit code</b>, and that reader is the same one
    /// who has to notice the day a <i>real</i> ratchet appears. This file has killed that exact defect
    /// class four times already (a green test over a dead feature; a fixture that pinned a bug; three
    /// checks crying wolf, blind, and inverted). A permanently-red gate is the same animal facing the
    /// other way.
    ///
    /// <para><b>What was rejected, and why.</b>
    /// <list type="bullet">
    /// <item><b>A <c>--strict</c> / <c>--ignore-accepted</c> flag split.</b> A flag is not keyed to
    /// anything, so <c>--ignore-accepted</c> forgives the 15th ratchet and tomorrow's <c>purse</c> ratchet
    /// exactly as readily as the 14 that were ruled on — it is a bare suppression with a command-line
    /// interface. And whichever mode CI does not run is decoration.</item>
    /// <item><b>A distinct exit code alone</b> ("2 = only known findings"). It has nothing to be
    /// distinct <i>against</i>: "known" is a fact about a ledger, so the code needs this file first. Worth
    /// having on top one day; it is not the mechanism.</item>
    /// <item><b>Living in <c>simconfig.json</c>, or in <see cref="Town"/> via <see cref="TownLoader"/>.</b>
    /// The engine must not be able to read what the linter has been told to forgive — a <c>Town</c> that
    /// carries its own lint suppressions is one a simulation could, in principle, consult. Acceptance is
    /// the instrument's business, so it stays in the instrument.</item>
    /// </list>
    ///
    /// <para><b>Four properties, and each one is load-bearing:</b>
    /// <list type="number">
    /// <item><b>It never silences.</b> An accepted finding prints in full, every run, under its own
    /// <c>[ACCEPTED]</c> heading with the ruling above it. Silencing is not accepting — the reader must
    /// always see the defect, or the ledger becomes the place defects go to die quietly.</item>
    /// <item><b>It never reclassifies.</b> <see cref="Finding.Severity"/> stays <c>error</c>; only
    /// <see cref="Finding.Gates"/> moves. Error is reserved for proofs and an accepted ratchet is still
    /// a proof.</item>
    /// <item><b>Accepting costs a written reason and a resolving pointer to the ruling</b>, both
    /// enforced non-blank. A bare id list would be worse than the disease: it records that someone
    /// stopped caring, not that someone decided.</item>
    /// <item><b>The key is exact and narrow</b> — <c>(check, kind, subject)</c>, ordinal, no patterns,
    /// with subjects enumerated one by one. So accepting these 14 <c>restlessness</c> curves cannot
    /// absorb a 15th townee's (not in the list) or a <c>purse</c> ratchet on a townee who <i>is</i>
    /// (different subject). New defects still gate; that is the whole point of not silencing them.</item>
    /// </list>
    ///
    /// <para><b>One entry per ruling, not per finding.</b> The 14 are one judgement about one drive, and
    /// 14 copies of one sentence would misrepresent that as 14 independent ones — besides being the
    /// repetition that makes somebody reach for a wildcard. So an entry carries an explicit
    /// <c>subjects</c> array: one reason, many named subjects, still no pattern anywhere.</para>
    ///
    /// <para><b>Absence is the default and is not a finding.</b> A town with no ledger accepts nothing
    /// and every proof gates — which is what <c>tests/towns/golden-town</c> (frozen, <c>PNO.D2</c>) must
    /// keep doing. It stays red by construction rather than by a rule, and that is correct: the old town
    /// really is defective and nothing should hide it.</para>
    /// </summary>
    private static (List<Acceptance> Ledger, List<Finding> Problems) LoadAcceptances(string townDir)
    {
        var ledger = new List<Acceptance>();
        var problems = new List<Finding>();
        string path = Path.Combine(townDir, AcceptedFile);
        if (!File.Exists(path)) return (ledger, problems);

        JsonNode? root;
        try
        {
            // The same tolerances the rest of data/ is authored under (DataJson) — a ledger is authored
            // prose like everything else here, and a trailing comma must not be able to un-accept 14
            // findings.
            root = JsonNode.Parse(DataJson.ReadText(path), documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (Exception ex)
        {
            problems.Add(Unreadable(path, $"it does not parse: {ex.Message}"));
            return (ledger, problems);
        }

        if (root?["accepted"] is not JsonArray arr)
        {
            problems.Add(Unreadable(path, "it has no `accepted` array. The shape is "
                + "{\"version\": 1, \"accepted\": [{\"check\", \"kind\"?, \"subjects\": [], \"reason\", "
                + "\"ruling\", \"date\"?}]}."));
            return (ledger, problems);
        }

        for (int i = 0; i < arr.Count; i++)
        {
            var e = arr[i] as JsonObject;
            string? check = Str(e, "check");
            string? reason = Str(e, "reason");
            string? ruling = Str(e, "ruling");
            var subjects = (e?["subjects"] as JsonArray)?
                .Select(n => (n as JsonValue)?.TryGetValue<string>(out var s) == true ? s : null)
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList() ?? new List<string>();

            // Every one of these is "the ledger is written wrong", and none of them can be caused by the
            // town getting better — so they gate. Contrast UnmatchedAcceptances, which can be, and does not.
            var missing = new List<string>();
            if (check is null) missing.Add("check");
            if (subjects.Count == 0) missing.Add("subjects");
            if (reason is null) missing.Add("reason");
            if (ruling is null) missing.Add("ruling");
            if (missing.Count > 0)
            {
                problems.Add(Malformed(i, check, $"it is missing [{string.Join(", ", missing)}]. Every "
                    + "field but `kind` and `date` is required, and `reason`/`ruling` may not be blank: an "
                    + "acceptance without a written reason and a pointer to where it was ruled is a "
                    + "suppression, and a suppression list nobody can audit is worse than the defect it "
                    + "hides. This entry accepts nothing until it is fixed."));
                continue;
            }
            if (UnacceptableChecks.Contains(check!))
            {
                problems.Add(Malformed(i, check, $"'{check}' can never be accepted, at any subject. "
                    + $"[{string.Join(", ", UnacceptableChecks.OrderBy(x => x, StringComparer.Ordinal))}] "
                    + "each mean the report itself is untrustworthy — there were no findings to accept, or "
                    + "an unknown subset of the checks never ran, or the ledger would be excusing its own "
                    + "malformation. Accepting one would forgive a defect on the evidence of a run that did "
                    + "not happen. This entry accepts nothing."));
                continue;
            }

            ledger.Add(new Acceptance(i, check!, Str(e, "kind"), subjects, reason!, ruling!, Str(e, "date")));
        }
        return (ledger, problems);
    }

    /// <summary>The gate must never pass on a ledger it could not read — <see cref="ErrorChecks"/>'s own
    /// argument about <c>load</c>, one step on. An unreadable ledger is not "nothing is accepted": it is
    /// "nobody knows what would have been", and those exit differently.</summary>
    private static Finding Unreadable(string path, string why) =>
        new("lint-accepted", AcceptedFile,
            $"the acceptance ledger at {path} exists but cannot be read — {why} This is NOT the same as "
            + "having no ledger (which is fine, and means nothing is accepted): the file is there, so it "
            + "claims to forgive something, and the gate cannot know what. Fix the file or delete it.",
            new JsonObject { ["kind"] = "unreadable", ["path"] = path });

    private static Finding Malformed(int entry, string? check, string why) =>
        new("lint-accepted", $"entry[{entry}]{(check is null ? "" : $":{check}")}",
            $"{AcceptedFile} entry [{entry}] is malformed — {why}",
            new JsonObject { ["kind"] = "malformed", ["entry"] = entry, ["check"] = check });

    /// <summary>A non-blank string at <paramref name="key"/>, trimmed, or null. A blank reason is a
    /// missing reason.</summary>
    private static string? Str(JsonObject? o, string key) =>
        o?[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s)
            ? s.Trim() : null;

    /// <summary>A finding's <c>data.kind</c> discriminator, or null for a check that emits none.</summary>
    private static string? KindOf(Finding f) =>
        f.Data["kind"] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    /// <summary>
    /// The ledger entry that accepts this finding, or null.
    ///
    /// <para><b>The key is <c>(check, kind, subject)</c>, all ordinal, all exact.</b> Subject alone would
    /// already stop the two cases that matter — a 15th townee is a subject nobody listed, and a
    /// <c>purse</c> ratchet on a listed townee is <c>x.purse</c>, not <c>x.restlessness</c>. <c>kind</c>
    /// is in the key because subject alone is <i>not</i> enough in general, and this codebase already has
    /// the counterexample: <c>ghost-cast</c> emits <c>thin</c> (a cameo) and <c>unreachable</c> (nobody
    /// wrote for them) at the same subject and the same class, so an acceptance written for a cameo would
    /// silently swallow the day somebody deleted that townee's last rule.</para>
    ///
    /// <para><b>null-equals-null, so an omitted <c>kind</c> matches only a finding that has none.</b> The
    /// tempting alternative — "omitted means any kind" — is a wildcard wearing a default, and it fails
    /// exactly where it matters: an author who never noticed the discriminator gets the broadest possible
    /// acceptance. This way the mistake costs a red gate and a message naming the kind to add, which is
    /// the direction a gate should fail in.</para>
    /// </summary>
    private static Acceptance? MatchOf(Finding f, List<Acceptance> ledger)
    {
        if (f.Severity != "error" || UnacceptableChecks.Contains(f.Check)) return null;
        string? kind = KindOf(f);
        return ledger.FirstOrDefault(a =>
            string.Equals(a.Check, f.Check, StringComparison.Ordinal)
            && string.Equals(a.Kind, kind, StringComparison.Ordinal)
            && a.Subjects.Contains(f.Subject, StringComparer.Ordinal));
    }

    /// <summary>
    /// Ledger lines that forgave nothing this run — <b>warn-class, and the asymmetry is deliberate.</b>
    ///
    /// <para>Every other <c>lint-accepted</c> kind gates, because every other one means somebody wrote the
    /// ledger wrong. This one is different: <b>its commonest cause is the town getting better.</b> Fix a
    /// ratchet and its acceptance goes stale — and a gate that turns red when you repair a defect teaches
    /// exactly the lesson this whole mechanism exists to unteach. So it is news, loudly, and it is not a
    /// gate. (It is also the one kind that would fire in a heap for a reason that is not about the ledger
    /// at all: if the world stops resolving, every drift-check acceptance goes unmatched at once.)</para>
    ///
    /// <para><b>Only when every check ran.</b> With <c>world_resolved</c> false an unknown subset was
    /// skipped, so an unmatched acceptance is evidence about the skip, not about the ledger. Suppressed
    /// with a note instead of guessed at — reporting it would be the linter auditing its own fiction,
    /// which is the mistake three checks in this file were rewritten for.</para>
    /// </summary>
    private static IEnumerable<Finding> UnmatchedAcceptances(
        List<Acceptance> ledger, HashSet<(int, string)> matched)
    {
        foreach (var a in ledger)
            foreach (var subject in a.Subjects.Where(s => !matched.Contains((a.Entry, s))))
                yield return new Finding("lint-accepted", $"{a.Check}:{subject}",
                    $"{AcceptedFile} entry [{a.Entry}] accepts '{a.Check}'"
                    + (a.Kind is null ? "" : $" (kind {a.Kind})") + $" at '{subject}' — and no such "
                    + "error-class finding was reported this run, so the line forgives nothing"
                    + (ErrorChecks.Contains(a.Check)
                        ? ". Most likely the defect was fixed, in which case delete the subject: an "
                          + "acceptance outliving its finding is a standing pre-authorisation for that "
                          + "exact defect to come back unnoticed."
                        : $". Note '{a.Check}' is not an error-class check at all, so it could never have "
                          + "gated and this entry could never have done anything — check the name.")
                    + " NOT gated: the usual cause of this finding is the town getting better, and a gate "
                    + "that reddens when you fix something is the disease.",
                    new JsonObject
                    {
                        ["kind"] = "unmatched", ["entry"] = a.Entry,
                        ["accepts_check"] = a.Check, ["accepts_subject"] = subject,
                    },
                    Class: "warn");
    }

    // --- engine reuse -------------------------------------------------------------------

    /// <summary>A world with the real clockwork resolved for the given day. Not a simulation: no
    /// pressures drift and no storylets fire, so occupancy is the authored day-plan's own truth.
    /// The day matters — <c>Clockwork</c> applies the departure schedule, so a day-3 world has the
    /// adventurers who left already gone.</summary>
    private static World ResolveAt(Town town, int day)
    {
        var w = World.Build(town);
        w.Day = day;
        Clockwork.ResolveDay(w);
        return w;
    }

    private static bool Asleep(World w, string id, int slot) =>
        w.TowneeById.TryGetValue(id, out var t) && t.Asleep.Length > slot && t.Asleep[slot];

    /// <summary>Unordered pair (ordinal-sorted) → slots per day where both are awake in the same
    /// room. Counted once per slot, not once per shared room: a roaming courier is co-present with
    /// the same person at several places in one slot, and that is still one slot of company.</summary>
    private static Dictionary<(string A, string B), int> AwakeCopresentSlots(World w)
    {
        var counts = new Dictionary<(string, string), int>();
        for (int s = 0; s < w.SlotsPerDay; s++)
        {
            var seen = new HashSet<(string, string)>();
            foreach (var (_, occ) in w.OccupantsAt(s))
            {
                var awake = occ.Where(id => !Asleep(w, id, s)).OrderBy(x => x, StringComparer.Ordinal).ToList();
                for (int i = 0; i < awake.Count; i++)
                    for (int j = i + 1; j < awake.Count; j++)
                        seen.Add((awake[i], awake[j]));
            }
            foreach (var p in seen) counts[p] = counts.GetValueOrDefault(p) + 1;
        }
        return counts;
    }

    private static (string, string) Key(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

    /// <summary>The cast a storylet is anchored to (its <c>_binding</c>, in copresent-role order).
    /// Empty for an unbound rule, which binds by search and therefore has no fixed cast.</summary>
    private static List<string> Cast(StoryletDto s) =>
        s.Binding is { Count: > 0 }
            ? s.Predicates.Copresent.Where(r => s.Binding.ContainsKey(r)).Select(r => s.Binding[r]).ToList()
            : new List<string>();

    /// <summary>Every <c>(place, slot)</c> at which a storylet's whole bound cast could fire: they
    /// share the room, they pass the awake gate, and the room satisfies the rule's own <c>place</c>
    /// predicate (checked with <see cref="StoryletEngine.PlaceMatches"/> — the engine's matcher, not
    /// a copy).
    /// <para><b>The pair, not just the place.</b> Carriage is a fact about a <i>firing</i>: hearsay-lite
    /// clause (c) asks who the cast meets <i>after</i> slot N, so the same room can be tellable at
    /// noon and a dead drop at midnight. A place-only enumeration cannot express that, which is how
    /// <c>stranded-beats</c> came to call a carried beat dead.</para>
    /// <paramref name="requireAwake"/> mirrors the engine's awake gate, which <c>must_fire</c>
    /// storylets are exempt from — that exemption is the whole of the sleeping-farewell defect.</summary>
    private static IEnumerable<(string Place, int Slot)> FireOpportunities(World w, StoryletDto s, bool requireAwake)
    {
        var cast = Cast(s);
        if (cast.Count == 0) yield break;
        for (int slot = 0; slot < w.SlotsPerDay; slot++)
        {
            foreach (var (place, occ) in w.OccupantsAt(slot).OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (!cast.All(occ.Contains)) continue;
                if (requireAwake && cast.Any(id => Asleep(w, id, slot))) continue;
                if (!StoryletEngine.PlaceMatches(w, s.Predicates.Place, place)) continue;
                yield return (place, slot);
            }
        }
    }

    /// <summary>Places where a storylet's whole bound cast could fire, over the whole day.</summary>
    private static HashSet<string> FirePlaces(World w, StoryletDto s, bool requireAwake) =>
        FireOpportunities(w, s, requireAwake).Select(o => o.Place).ToHashSet(StringComparer.Ordinal);

    /// <summary>Drives that some storylet's pressure predicate actually reads. A trait that scales a
    /// drive nothing gates on is scaling a number nobody looks at.</summary>
    private static HashSet<string> DrivesRead(Town t) =>
        t.Storylets.SelectMany(s => s.Predicates.Pressure.Keys)
                   .Select(k => k.Contains('.') ? k[(k.IndexOf('.') + 1)..] : "")
                   .Where(d => d.Length > 0).ToHashSet(StringComparer.Ordinal);

    // --- 1. ghost cast ------------------------------------------------------------------

    /// <summary>Townees fewer than two live storylets can reach after day 2. The cast floor — every
    /// cast member has ≥2 reachable rules past the opening — is an accepted project bar, so falling
    /// under it gates. One rule is a cameo; none at all is a hole in the writing.
    ///
    /// <para><b>The threshold gates. The departure does not.</b> Three findings live under this name
    /// and only two of them are defects:</para>
    /// <list type="bullet">
    /// <item><c>unreachable</c> — 0 rules, still in town. Nobody wrote for them. <b>Errors.</b></item>
    /// <item><c>thin</c> — 1 rule, still in town. Under the bar, so it <b>errors</b> too — but "you
    /// gave them a cameo" is a different sentence to an author than "you gave them nothing", and the
    /// message now says which rather than making them count the ids themselves.</item>
    /// <item><c>by-design</c> — authored to leave, and gone by day 3. <b>Warns.</b></item>
    /// </list>
    ///
    /// <para>The last case is why this was rewritten. The check already knew: it appended "which is
    /// by design" to the detail and failed the build on it anyway. That is the habit this linter has
    /// twice paid to unlearn — <c>stranded-beats</c> cried wolf at error class over content the
    /// summarizer was printing, and <c>latch-die</c> got 7 of 7 of its condemnations backwards. Error
    /// is reserved for proofs, and a townee with <c>departs_day</c> ≤ 2 having nothing left to do is
    /// not a hole in the writing — it <i>is</i> the writing. A check that annotates a finding as
    /// intentional and gates on it regardless teaches its readers to scroll past it, and that
    /// attention is spent out of the same budget as every check here that was right.</para>
    ///
    /// <para><b>Away comes from the engine.</b> <see cref="Clockwork.ResolveDay"/> derives it from
    /// the authored <c>departs_day</c>; this check does not re-decide whether day 3 is past someone's
    /// departure. Cheap to hand-model, and hand-modelling is exactly how the two checks above came to
    /// audit their own fiction — so the exemption is only ever as true as the engine's own flag, which
    /// is the strongest thing it could be.</para></summary>
    private static IEnumerable<Finding> GhostCast(Town t, World later)
    {
        foreach (var n in t.Townees.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var reachable = t.Storylets.Where(s => LiveAfterDay2(t, s) && CanReach(t, s, n.Id))
                                       .Select(s => s.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (reachable.Count >= 2) continue;

            bool away = later.TowneeById.TryGetValue(n.Id, out var rt) && rt.Away;
            string kind = away ? "by-design" : reachable.Count == 0 ? "unreachable" : "thin";

            string detail = kind switch
            {
                "by-design" =>
                    $"{n.Name} is reachable by {reachable.Count} storylet(s) after day 2"
                    + (reachable.Count > 0 ? $" ({string.Join(", ", reachable)})" : "")
                    + " — but they are away from day 3, by design"
                    + (n.DepartsDay is int d ? $" (departs_day {d})" : "")
                    + ". NOT gated: the cast floor is a bar for townees who are still here. If they "
                    + "are meant to stay, the defect is the departure, not the missing rules.",

                "unreachable" =>
                    $"{n.Name} is reachable by 0 storylet(s) after day 2 — they never appear again, "
                    + "and they never leave either. Nothing authored can still reach them past the "
                    + "opening: this is unwritten content, not a departure. Either write them a rule "
                    + "that outlives day 2, or give them a departs_day and let them go.",

                // The common case — keep it short. This is the one that repeats across a whole cast,
                // and a paragraph restated verbatim under six names is read once and skipped after.
                _ =>
                    $"{n.Name} is reachable by only 1 storylet after day 2 ({reachable[0]}) — a cameo, "
                    + "not a part. Thin rather than absent, but still under the cast floor of 2, so it "
                    + "gates: write them a second rule, or add them to a role in one that already exists.",
            };

            yield return new Finding("ghost-cast", n.Id, detail,
                new JsonObject
                {
                    ["reachable"] = new JsonArray(reachable.Select(x => (JsonNode)x!).ToArray()),
                    ["count"] = reachable.Count,
                    // The machine-readable half of the split. `kind` is the finding; `away_by_day_3`
                    // is kept under its old name because it is what a consumer already greps for.
                    ["kind"] = kind,
                    ["away_by_day_3"] = away,
                    ["departs_day"] = n.DepartsDay is int dd ? JsonValue.Create(dd) : null,
                },
                // Only the by-design case is exempt. A cameo still fails the bar Panda ruled.
                Class: away ? "warn" : null);
        }
    }

    /// <summary>Can this storylet still fire after day 2? A one-shot calendar flag is the case that
    /// matters: <c>departing_today</c> is true on exactly one authored day, so a rule gated on it is
    /// spent the moment that day passes — and any townee whose only rule it was is now a ghost.</summary>
    private static bool LiveAfterDay2(Town t, StoryletDto s)
    {
        foreach (var (key, want) in s.Predicates.Flag)
        {
            if (!want) continue;
            int dot = key.IndexOf('.');
            if (dot < 0) continue;
            string role = key[..dot], flag = key[(dot + 1)..];
            if (flag != "departing_today") continue;
            if (s.Binding is null || !s.Binding.TryGetValue(role, out var id)) continue;
            if (!t.TowneeById.TryGetValue(id, out var n)) continue;
            if (n.DepartsDay is not int dd || dd <= 2) return false;
        }
        return true;
    }

    /// <summary>Anchored rules reach exactly their cast. An unbound rule binds by search, so it
    /// reaches anyone who could satisfy a role's trait requirement.</summary>
    private static bool CanReach(Town t, StoryletDto s, string towneeId)
    {
        if (s.Binding is { Count: > 0 }) return s.Binding.ContainsValue(towneeId);
        if (!t.TowneeById.TryGetValue(towneeId, out var n)) return false;
        return s.Predicates.Copresent.Any(role =>
            !s.Predicates.Trait.TryGetValue(role, out var need) || n.Traits.Contains(need));
    }

    // --- 2. beats over sleepers ---------------------------------------------------------

    /// <summary>A storylet whose cast is never all awake in one room. It can only ever fire over a
    /// sleeping participant — which the engine permits for <c>must_fire</c> rules, and those are
    /// exactly the rules authored at the highest tellability.</summary>
    private static IEnumerable<Finding> BeatsOverSleepers(Town t, World day1)
    {
        foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var cast = Cast(s);
            if (cast.Count == 0) continue;                       // unbound: no fixed cast to judge
            if (FirePlaces(day1, s, requireAwake: true).Count > 0) continue;

            var asleepPlaces = FirePlaces(day1, s, requireAwake: false);
            yield return new Finding("beats-over-sleepers", s.Id,
                $"'{s.Id}' (tellability {Tellability(s):0.00}) has no slot where its cast "
                + $"({string.Join(", ", cast)}) is awake together"
                + (asleepPlaces.Count > 0
                    ? $"; they only share a room at [{string.Join(", ", asleepPlaces.OrderBy(x => x, StringComparer.Ordinal))}] with someone asleep"
                    : "; they never share a room at all")
                + (s.MustFire ? " — and must_fire exempts it from the awake gate, so it fires anyway" : ""),
                new JsonObject
                {
                    ["cast"] = new JsonArray(cast.Select(x => (JsonNode)x!).ToArray()),
                    ["must_fire"] = s.MustFire,
                    ["tellability"] = Tellability(s),
                    ["shared_rooms_while_asleep"] =
                        new JsonArray(asleepPlaces.OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                });
        }
    }

    private static double Tellability(StoryletDto s) =>
        s.Effects.FirstOrDefault(e => e.Chronicle)?.Tellability ?? 0.0;

    // --- 3. unconvened bonds ------------------------------------------------------------

    /// <summary>An authored regard edge between two townees who are barely ever awake together. The
    /// tension is written down; the clockwork never convenes it.</summary>
    private static IEnumerable<Finding> UnconvenedBonds(Town t, World w, Dictionary<(string, string), int> pairs)
    {
        const int Need = 8;
        var seen = new HashSet<(string, string)>();
        foreach (var n in t.Townees.OrderBy(x => x.Id, StringComparer.Ordinal))
            foreach (var (target, edge) in n.Regard.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (!t.TowneeById.ContainsKey(target)) continue;    // schema already reported it
                var key = Key(n.Id, target);
                if (!seen.Add(key)) continue;                        // one physical fact, not two
                int slots = pairs.GetValueOrDefault(key);
                if (slots >= Need) continue;
                var tags = n.Regard.GetValueOrDefault(target)?.Tags ?? new List<string>();
                yield return new Finding("unconvened-bonds", $"{key.Item1}+{key.Item2}",
                    $"authored regard ({string.Join("/", tags.DefaultIfEmpty("no tag"))}) but only {slots} "
                    + $"awake-co-present slot(s)/day — under the {Need}-slot floor for a bond to play out",
                    new JsonObject
                    {
                        ["slots"] = slots, ["floor"] = Need,
                        ["tags"] = new JsonArray(tags.Select(x => (JsonNode)x!).ToArray()),
                    });
            }
    }

    // --- 4. silent pairs ----------------------------------------------------------------

    /// <summary>Two townees who spend a great deal of the day awake in the same room with nothing
    /// authored between them — no storylet, no regard. The town's wasted content, ranked.</summary>
    private static IEnumerable<Finding> SilentPairs(Town t, World w, Dictionary<(string, string), int> pairs)
    {
        const int Loud = 20;
        var bonded = new HashSet<(string, string)>();
        foreach (var n in t.Townees)
            foreach (var target in n.Regard.Keys)
                bonded.Add(Key(n.Id, target));

        var storyleted = new HashSet<(string, string)>();
        foreach (var s in t.Storylets)
        {
            var cast = Cast(s);
            for (int i = 0; i < cast.Count; i++)
                for (int j = i + 1; j < cast.Count; j++)
                    storyleted.Add(Key(cast[i], cast[j]));
        }

        foreach (var (pair, slots) in pairs.Where(kv => kv.Value >= Loud)
                     .OrderByDescending(kv => kv.Value)
                     .ThenBy(kv => kv.Key.Item1, StringComparer.Ordinal)
                     .ThenBy(kv => kv.Key.Item2, StringComparer.Ordinal))
        {
            if (bonded.Contains(pair) || storyleted.Contains(pair)) continue;
            yield return new Finding("silent-pairs", $"{pair.Item1}+{pair.Item2}",
                $"{slots} awake-co-present slots/day and nothing authored between them — "
                + "no storylet, no regard edge",
                new JsonObject { ["slots"] = slots, ["threshold"] = Loud });
        }
    }

    // --- 5. hearsay dead zones ----------------------------------------------------------

    /// <summary>
    /// <b>Error-class, and therefore a proof.</b> A storylet that can fire somewhere, but whose every
    /// single fire opportunity is uncarriable — so it burns a slot and can never reach a summary.
    ///
    /// <para><b>What this check got wrong, and why the shape changed.</b> It used to reason per-room:
    /// "no carrier enters room R; beats can fire at R; therefore those beats are dead." Both steps are
    /// unsound. Carriage is decided by <see cref="Summarizer.WouldBeCarried"/>, which has three
    /// clauses — the carrier is in the cast (a), the carrier is in the room (b), or <b>a participant
    /// later shares a room with a carrier that same day</b> (c). The old check modelled only (b), so a
    /// beat in a carrier-free room whose cast walks to the market an hour later was reported as
    /// provably dead while the sim was putting it in the summary. And "dead" is a claim about a
    /// <i>beat</i>, which can usually fire in several rooms; a carrier-free room strands a beat only
    /// if every other room it could fire in is dead too.</para>
    ///
    /// <para>So the unit is the beat, the test is the engine's own, and the quantifier is <i>every</i>
    /// opportunity. What survives is a real proof: <c>market-cheer</c> is no longer flagged (clause (c)
    /// carries it, and the day-2 summary says so), while a beat that genuinely cannot be told still is.
    /// The room-level observation is true and keeps its warning in <see cref="HearsayDeadZones"/>; it
    /// just never proved what it was being used to claim.</para>
    ///
    /// <para><b>Scoped to the town's own config.</b> <see cref="Summarizer.Candidates"/> drops
    /// non-<c>CarriedByGossip</c> entries only while <c>hearsay_required</c> is on; with it off, the
    /// beat reaches the summary and nothing is stranded. An error-class finding a knob can falsify is
    /// not a proof, so the error is gated on what the town actually authors.</para>
    /// </summary>
    private static IEnumerable<Finding> StrandedBeats(Town t, World w)
    {
        bool gated = t.Config.HearsayRequired;
        var carriers = Summarizer.CarriersOf(w);
        var carrierList = carriers.OrderBy(x => x, StringComparer.Ordinal).ToList();

        if (!gated) yield break;      // nothing can be stranded: the gate is off, every beat is told

        if (carriers.Count == 0)
        {
            yield return new Finding("stranded-beats", "town",
                "no townee has a hearsay_carrier trait and this town authors hearsay_required — so NO "
                + "beat can ever reach a summary and the entire storylet bank is wasted yield. Give "
                + "someone a hearsay_carrier trait.",
                new JsonObject { ["carriers"] = 0, ["hearsay_required"] = true });
            yield break;
        }

        foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var cast = Cast(s);
            if (cast.Count == 0) continue;      // unbound: binds by search, so it has no fixed cast

            var opps = FireOpportunities(w, s, requireAwake: !s.MustFire).ToList();
            if (opps.Count == 0) continue;      // it can't fire at all — beats-over-sleepers owns that

            if (opps.Any(o => Summarizer.WouldBeCarried(w, o.Slot, o.Place, cast, carriers))) continue;

            var rooms = opps.Select(o => o.Place).Distinct().OrderBy(x => x, StringComparer.Ordinal)
                            .Select(id => t.PlaceById.TryGetValue(id, out var p) ? p.Name : id).ToList();
            yield return new Finding("stranded-beats", s.Id,
                $"'{s.Id}' (tellability {Tellability(s):0.00}, cast {string.Join(", ", cast)}) can fire at "
                + $"{opps.Count} (place, slot) opportunit(y/ies) — across [{string.Join(", ", rooms)}] — and "
                + $"NOT ONE of them can ever be carried: at every one, no carrier is in the cast, no carrier "
                + $"is in the room, and no participant meets a carrier anywhere later that day (the three "
                + $"clauses of Summarizer.WouldBeCarried). hearsay_required is on in this town's simconfig, so "
                + $"Summarizer.Candidates drops every entry that is not CarriedByGossip. This beat fires, "
                + $"spends its slot, and is told to no one. The town's carriers are "
                + $"[{string.Join(", ", carrierList)}]. Fix: route a carrier into one of those rooms (a haunt: "
                + $"block or a roam set) while the cast is still there, have the cast cross a carrier's path "
                + $"later in the day, or give someone in the cast a hearsay_carrier trait.",
                new JsonObject
                {
                    ["cast"] = new JsonArray(cast.Select(x => (JsonNode)x!).ToArray()),
                    ["tellability"] = Tellability(s),
                    ["hearsay_required"] = true,
                    ["opportunities"] = opps.Count,
                    ["carriers"] = new JsonArray(carrierList.Select(x => (JsonNode)x!).ToArray()),
                    ["rooms"] = new JsonArray(opps.Select(o => o.Place).Distinct()
                        .OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                });
        }
    }

    /// <summary>Soft hearsay measures — <b>warnings, because none of them proves dead content</b>.
    /// Rooms no gossip-carrier ever enters; townees rarely awake near one; and beats that only
    /// *sometimes* land out of earshot. Each is a true statement about yield risk; the provable
    /// "this beat can never be told" case is <see cref="StrandedBeats"/>.</summary>
    private static IEnumerable<Finding> HearsayDeadZones(Town t, World w)
    {
        var carriers = Summarizer.CarriersOf(w);
        bool gated = t.Config.HearsayRequired;

        if (carriers.Count == 0)
        {
            // The gated case is StrandedBeats' town-level error; ungated, it strands nothing.
            if (!gated)
                yield return new Finding("hearsay-dead-zones", "town",
                    "no townee has a hearsay_carrier trait (hearsay_required is off in this town, so "
                    + "beats reach the summary anyway)",
                    new JsonObject { ["carriers"] = 0, ["hearsay_required"] = false });
            yield break;
        }

        // (a) Rooms. Visits are counted the way the engine's gate counts them (raw occupancy, no
        // awake test) so a finding here means the engine really cannot carry news out of this room.
        // It does NOT mean beats there are lost — a cast can carry the news out on its own feet
        // (clause (c)), which is exactly the inference this check used to get wrong.
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (int s = 0; s < w.SlotsPerDay; s++)
            foreach (var (place, occ) in w.OccupantsAt(s))
                if (occ.Any(carriers.Contains)) visited.Add(place);

        foreach (var p in t.Places.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (visited.Contains(p.Id)) continue;
            var fires = t.Storylets
                .Where(s => FirePlaces(w, s, requireAwake: !s.MustFire).Contains(p.Id))
                .Select(s => s.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();

            yield return new Finding("hearsay-dead-zones", p.Id,
                $"no gossip-carrier ever enters {p.Name}"
                + (fires.Count > 0
                    ? $" — {fires.Count} beat(s) can fire there ({string.Join(", ", fires)}). Not necessarily "
                      + "lost: a beat is still told if its cast meets a carrier later that day. See "
                      + "stranded-beats for the ones that provably are not."
                    : " (no bound cast can fire there either, so nothing is at risk)"),
                new JsonObject
                {
                    ["kind"] = p.Kind, ["board"] = p.Board,
                    ["hearsay_required"] = gated,
                    ["beats_that_can_fire_here"] = new JsonArray(fires.Select(x => (JsonNode)x!).ToArray()),
                });
        }

        // (b) Beats that land out of earshot only some of the time. The honest version of what the
        // old room-level error was reaching for: real lost yield, but a risk rather than a proof,
        // because whether it bites depends on which slot the rule actually fires in.
        if (gated)
            foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                var cast = Cast(s);
                if (cast.Count == 0) continue;
                var opps = FireOpportunities(w, s, requireAwake: !s.MustFire).ToList();
                if (opps.Count == 0) continue;
                var lost = opps.Where(o => !Summarizer.WouldBeCarried(w, o.Slot, o.Place, cast, carriers)).ToList();
                if (lost.Count == 0 || lost.Count == opps.Count) continue;   // all fine, or StrandedBeats' job

                var rooms = lost.Select(o => o.Place).Distinct().OrderBy(x => x, StringComparer.Ordinal)
                                .Select(id => t.PlaceById.TryGetValue(id, out var p) ? p.Name : id).ToList();
                yield return new Finding("hearsay-dead-zones", s.Id,
                    $"'{s.Id}' has {lost.Count} of {opps.Count} (place, slot) fire opportunit(y/ies) that "
                    + $"could never be carried — at [{string.Join(", ", rooms)}], slots "
                    + $"{lost.Min(o => o.Slot)}-{lost.Max(o => o.Slot)}. If it fires there it is told to no "
                    + "one; elsewhere it is fine. Lost yield, not dead content.",
                    new JsonObject
                    {
                        ["opportunities"] = opps.Count, ["uncarriable"] = lost.Count,
                        ["rooms"] = new JsonArray(lost.Select(o => o.Place).Distinct()
                            .OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                    });
            }

        // (c) Townees. The brief's measure: awake slots in a room with an awake carrier.
        const int Need = 6;
        foreach (var n in t.Townees.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (carriers.Contains(n.Id)) continue;
            int near = 0;
            for (int s = 0; s < w.SlotsPerDay; s++)
            {
                if (Asleep(w, n.Id, s)) continue;
                bool with = w.OccupantsAt(s).Any(kv => kv.Value.Contains(n.Id)
                    && kv.Value.Any(o => carriers.Contains(o) && !Asleep(w, o, s)));
                if (with) near++;
            }
            if (near >= Need) continue;
            yield return new Finding("hearsay-dead-zones", n.Id,
                $"{n.Name} spends {near} awake slot(s)/day with an awake gossip-carrier — under the "
                + $"{Need}-slot floor; what happens to them mostly cannot be told",
                new JsonObject { ["awake_slots_with_carrier"] = near, ["floor"] = Need });
        }
    }

    // --- 6. unreachable haunts ----------------------------------------------------------

    /// <summary>A haunt the townee's day-plan never deliberately routes to. A haunt is an authored
    /// statement that someone chooses to be somewhere; if the plan does not act on it, the statement
    /// is decoration. Home and work do <b>not</b> count as honouring a haunt — being somewhere
    /// because you live there is not the same claim.</summary>
    private static IEnumerable<Finding> UnreachableHaunts(Town t)
    {
        foreach (var n in t.Townees.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!t.DayPlans.TryGetValue(n.Dayplan, out var plan)) continue;   // schema reports it
            var blocks = plan.Weekday.Concat(plan.Away ?? Enumerable.Empty<DayBlockDto>()).ToList();

            var routed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var b in blocks)
            {
                if (b.Place.StartsWith("haunt:", StringComparison.Ordinal)) routed.Add(b.Place["haunt:".Length..]);
                foreach (var r in b.Roams ?? Enumerable.Empty<string>()) routed.Add(r);
            }

            foreach (var h in n.Haunts)
            {
                if (routed.Contains(h)) continue;
                string via = h == n.Home ? "home" : h == n.Work ? "work" : "never";
                yield return new Finding("unreachable-haunts", $"{n.Id}:{h}",
                    $"{n.Name} haunts '{h}' but dayplan '{n.Dayplan}' never routes there via a haunt: "
                    + $"block or a roam set"
                    + (via == "never"
                        ? " — they never go there at all"
                        : $" (they are there anyway, as their {via}, which is a different claim)"),
                    new JsonObject { ["dayplan"] = n.Dayplan, ["haunt"] = h, ["routed_as"] = via });
            }
        }
    }

    // --- 7. dead traits -----------------------------------------------------------------

    private static IEnumerable<Finding> DeadTraits(Town t)
    {
        var held = t.Townees.SelectMany(n => n.Traits).ToHashSet(StringComparer.Ordinal);
        var named = t.Storylets.SelectMany(s => s.Predicates.Trait.Values).ToHashSet(StringComparer.Ordinal);
        var drivesRead = DrivesRead(t);

        foreach (var tr in t.Traits.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!held.Contains(tr.Id))
            {
                yield return new Finding("unheld-trait", tr.Id,
                    $"trait '{tr.Id}' is held by no townee", new JsonObject());
                continue;
            }

            // storylet_weight_mods is inert engine-wide: nothing outside its own declaration in
            // Model/TownDtos.cs reads it, and PNO.D4 ruled against wiring it (it would change what
            // fires bank-wide and move VFB.Q1's numbers mid-measurement). Authored values here are
            // a promise the engine does not keep.
            if (tr.StoryletWeightMods.Count > 0)
                yield return new Finding("dead-trait-mods", tr.Id,
                    $"trait '{tr.Id}' authors storylet_weight_mods "
                    + $"({string.Join(", ", tr.StoryletWeightMods.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}×{kv.Value:0.##}"))}) "
                    + "but no engine code reads that field — the values are inert (PNO.D4 ruled against wiring it)",
                    new JsonObject { ["field"] = "storylet_weight_mods" });

            // Un-migrated direction-blind mods. Legal, and exactly-preserved by RateModConverter — a bare
            // number still means {gain: n, decay: n}, which is what the single scalar always did. But that
            // is precisely the thing that made `wanderlust` speed up settling, so a trait still authored
            // this way is one whose word has never been checked against its arithmetic. Warn-class: the
            // frozen fixture (PNO.D2) is authored this way on purpose and must stay that way forever.
            var legacy = tr.PressureRateMods.Where(kv => kv.Value.Legacy)
                           .Select(kv => kv.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (legacy.Count > 0)
                yield return new Finding("legacy-rate-mods", tr.Id,
                    $"trait '{tr.Id}' authors {legacy.Count} bare-number pressure_rate_mod(s) "
                    + $"({string.Join(", ", legacy.Select(d => $"{d}×{tr.PressureRateMods[d].Gain:0.##}"))}) — "
                    + "a bare number means {gain: n, decay: n}, i.e. the same scalar both ways, which can only "
                    + "scale how fast the drive moves and never which way. If the trait's name is a claim about "
                    + "direction (wanderlust, frugal, open-handed), it is not currently making it. Migrate to "
                    + "{\"gain\": n, \"decay\": m}, or leave it if volatility really is what the word means.",
                    new JsonObject
                    {
                        ["drives"] = new JsonArray(legacy.Select(x => (JsonNode)x!).ToArray()),
                        ["form"] = "bare-number",
                    });

            if (named.Contains(tr.Id)) continue;
            // A target is as live as a rate mod — `cheerful` authors only a heart rest point now, and a check
            // that counted rate mods alone would call it dead while the engine was reading it every slot.
            var liveMods = tr.PressureRateMods.Keys.Concat(tr.PressureTargets.Keys)
                                              .Distinct(StringComparer.Ordinal).Where(drivesRead.Contains)
                                              .OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (liveMods.Count > 0) continue;

            var authored = tr.PressureRateMods.Keys.Concat(tr.PressureTargets.Keys)
                             .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
            yield return new Finding("dead-traits", tr.Id,
                $"trait '{tr.Id}' is named by no storylet predicate, and its pressure_rate_mods/pressure_targets "
                + (authored.Count == 0
                    ? "are empty — it does nothing at all"
                    : $"({string.Join(", ", authored)}) "
                      + $"touch no drive any predicate reads (read: {string.Join(", ", drivesRead.OrderBy(x => x, StringComparer.Ordinal))})"),
                new JsonObject
                {
                    ["pressure_rate_mods"] =
                        new JsonArray(tr.PressureRateMods.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                    ["pressure_targets"] =
                        new JsonArray(tr.PressureTargets.Keys.OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                    ["drives_read_by_predicates"] =
                        new JsonArray(drivesRead.OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                });
        }
    }

    // --- 8. non-driving regard edges ----------------------------------------------------

    /// <summary>An authored edge no predicate ever reads. It can be written to (an effect decrements
    /// it) but it gates nothing — it is a number that only ever gets worse and never causes anything.
    /// <para>Resolving the read requires honouring <c>flip</c>: <c>{"A-&gt;B": {tag, flip:true}}</c>
    /// means "B owes A", so the edge actually read is B→A.</para></summary>
    private static IEnumerable<Finding> NonDrivingRegard(Town t)
    {
        var driven = new HashSet<(string, string)>();
        foreach (var s in t.Storylets)
        {
            if (s.Binding is not { Count: > 0 }) continue;
            foreach (var (key, rp) in s.Predicates.Regard)
            {
                int i = key.IndexOf("->", StringComparison.Ordinal);
                if (i < 0) continue;
                string r1 = key[..i], r2 = key[(i + 2)..];
                if (!s.Binding.TryGetValue(r1, out var a) || !s.Binding.TryGetValue(r2, out var b)) continue;
                driven.Add(rp.Flip ? (b, a) : (a, b));       // the edge the predicate reads
            }
        }

        foreach (var n in t.Townees.OrderBy(x => x.Id, StringComparer.Ordinal))
            foreach (var (target, edge) in n.Regard.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (driven.Contains((n.Id, target))) continue;
                yield return new Finding("non-driving-regard", $"{n.Id}->{target}",
                    $"authored regard {n.Id}→{target} ({string.Join("/", edge.Tags.DefaultIfEmpty("no tag"))}, "
                    + $"score {edge.Score:0.##}) is read by no storylet predicate — it can be written, "
                    + "but it drives nothing",
                    new JsonObject
                    {
                        ["tags"] = new JsonArray(edge.Tags.Select(x => (JsonNode)x!).ToArray()),
                        ["score"] = edge.Score,
                    });
            }
    }

    // --- 9. ratchets --------------------------------------------------------------------

    // A drive is judged over the last TailDays of the run. These say what "against the clamp" and
    // "not moving" mean, and they are the sweep's own numbers (scrap_scripts/csharp/01_drive_sweep),
    // kept identical so the instrument that found the 27 and the gate that reports them cannot
    // disagree about what they are looking at.
    private const double ClampBand = 0.10;    // how far off a clamp the tail may fall and still be riding it
    private const double FlatTravel = 0.005;  // total absolute travel over the tail below this = not a pressure

    /// <summary>
    /// A (townee, drive) that is <b>not a pressure</b> — the fuel is against a clamp, or it never
    /// moves. Read out of a real <see cref="ObservedDays"/>-day run's <see cref="World.PressureLog"/>,
    /// which is the engine's own per-slot sample of the drive, then classified by what the authored
    /// data can and cannot do about it.
    ///
    /// <para><b>What this check used to ask, and why it saw almost nothing.</b> It probed
    /// <see cref="Pressures.BaseDaily"/> <i>per mode</i> and called a drive bidirectional if any mode
    /// pushed it up and any mode pushed it down. <c>restlessness</c> is <c>-0.10</c> engaged and
    /// <c>+0.06</c> at rest, so it passed — while its <i>net over a real 48-slot day</i> is one-way for
    /// 16 of the town's 18 townees. The break-even is exactly 18 engaged slots of 48; the town's actual
    /// counts are 8 (the four adventurers) and 22–36 (everyone else), so nobody is within four slots of
    /// it and the question the check was asking could not have found the answer. It reported 3 findings
    /// for the whole town, and all 3 were the adventurer purses — the one case that is deliberate.
    /// <b>A check that reports only the false positives is worse than no check</b>: it spends an
    /// author's attention teaching them the finding is noise.</para>
    ///
    /// <para><b>So: observe the outcome, then ask the bank what it could have done about it.</b> The
    /// outcome is not predicted at all — <see cref="World.PressureLog"/> already holds every slot of the
    /// real curve, effects and clamps and trait mods and all, and the sweep that found these 27 proved
    /// the readout works. The arithmetic below only ever <i>explains</i> a verdict the engine already
    /// handed over. That ordering is the doctrine this file has paid for three times.</para>
    ///
    /// <para><b>The five kinds, and why only one of them gates.</b> Error is reserved for proofs:</para>
    /// <list type="bullet">
    /// <item><c>ratchet</c> — <b>errors.</b> The drive is against a clamp; its shape is mode-constant
    /// (<see cref="Pressures.BaseDaily"/> ignores the current value, so the daily step is a constant and
    /// there is <i>no interior fixed point at any value</i> — <c>Pressures</c>'s own remarks state the
    /// theorem); and <b>not one authored effect in the bank pushes the other way</b>. Observed against
    /// the clamp + provably nothing that can lift it off = the fuel is dead and will stay dead. That is
    /// the same class of claim as <c>stranded-beats</c>, and it is a proof.</item>
    /// <item><c>outgunned</c> — <b>warns.</b> Same clamp, same shape, but the bank <i>does</i> push back
    /// and lost anyway. Not a proof: what it costs to win is a fire-rate question, and fire rates are a
    /// simulation outcome rather than an authored fact. A retune could fix it, so it must not gate on a
    /// number that a retune would move. Real lost fuel all the same, and the finding says by how much.</item>
    /// <item><c>by-design</c> — <b>warns.</b> An adventurer's purse draining to 0 is the economy walking
    /// them to the board; <c>Pressures</c> says so in as many words. Proved from authored data
    /// (<c>adventurer: true</c>), never guessed — the same bar <c>ghost-cast</c>'s exemption has to
    /// clear, and for the same reason: a check that annotates a finding as intentional and fails the
    /// build on it anyway teaches its readers to scroll past it.</item>
    /// <item><c>unwritten</c> — <b>warns.</b> Dead flat, and no authored effect touches it at all. The
    /// restoring force simply parked it at its rest point, which is the drive <i>working</i>. "Nobody
    /// wrote content for this" is a real finding, but it is not a broken drive, and one word of the
    /// gate's vocabulary should not have to mean both.</item>
    /// <item><c>quiet</c> — <b>warns.</b> Dead flat, but effects <i>are</i> authored — they just fire too
    /// rarely to move it across the tail. Split from <c>unwritten</c> because the repair is opposite:
    /// there is nothing to write, only something to let fire.</item>
    /// </list>
    ///
    /// <para><b>What it cannot tell you.</b> The clamp is observed over <see cref="ObservedDays"/> days
    /// at the town's authored knobs. The <i>proof</i> attached to a <c>ratchet</c> is about the shape and
    /// the bank, both of which are authored and exhaustively checkable — but the observation that got us
    /// there is still an observation, and a drive that clamps on day 57 looks alive from here.</para>
    /// </summary>
    private static IEnumerable<Finding> Ratchets(Town t, Observation o)
    {
        var w = o.World;
        int spd = w.SlotsPerDay;
        int tailStart = Math.Max(0, (ObservedDays - Math.Min(TailDays, ObservedDays)) * spd);

        foreach (var n in w.Townees)
        {
            // An away body is skipped outright by Pressures.DriftSlot ("away townees are off-sim"), so
            // its drives freeze by construction. A flat reading there is the departure, not a defect —
            // and reading it as one is how ghost-cast came to fail the build over the writing.
            if (n.Away) continue;

            foreach (var drive in Town.Drives)
            {
                if (!w.PressureLog.TryGetValue($"{n.Id}.{drive}", out var log) || log.Count == 0) continue;

                var tail = log.Skip(tailStart).ToList();
                if (tail.Count < 2) continue;
                double tailMin = tail.Min(), tailMax = tail.Max();

                // Total absolute travel, not range: a drive can be pinned while its per-slot moves sum
                // to nothing, and range alone cannot tell a standstill from a slow crawl.
                double travel = 0;
                for (int i = 1; i < tail.Count; i++) travel += Math.Abs(tail[i] - tail[i - 1]);

                // SATURATION, not "never left the clamp". A drive that hits 1.0, is clipped, relaxes to
                // 0.99 overnight and climbs back is pegged: the clamp is silently discarding authored
                // effect every single day and the drive has no headroom left to mean anything. The
                // sweep learned this the expensive way — its first cut scored a known-pegged purse "ok"
                // because the curve dipped below the ceiling once a night.
                bool pegged = tailMax >= 1.0 - 1e-9 && tailMin >= 1.0 - ClampBand;
                bool floored = tailMin <= 1e-9 && tailMax <= ClampBand;
                bool flat = !pegged && !floored && travel < FlatTravel;
                if (!pegged && !floored && !flat) continue;              // alive: it is doing its job

                // --- what the authored bank can do about it ---------------------------------------
                var writers = Writers(t, o, n.Id, drive);
                double bankPerDay = writers.Sum(x => x.Delta * x.Fires) / ObservedDays;
                bool clampedUp = pegged;

                // A counter-push is any authored effect pointing away from the clamp the drive sits at.
                // An unbound rule counts here even though it has no fixed cast: it binds by search, so
                // it *could* reach this townee, and "could" is enough to cost the finding its proof.
                // Erring toward "the bank might still reach it" is the safe direction for a gate.
                var counters = writers.Where(x => (x.Delta > 0) != clampedUp).ToList();

                // The shape, asked of BaseDaily rather than assumed from the drive's name. If the drift
                // is the same at 0.0 and at 1.0 in every mode the townee's day visits, it ignores the
                // current value — so the day's drift is a constant and the drive has nowhere to rest.
                // This is the one prediction left in the check, and it is exact: it is a question about
                // BaseDaily alone, and sampling BaseDaily answers it.
                double heartTarget = Pressures.HeartTarget(t, n);
                bool modeConstant = n.Mode.Distinct(StringComparer.Ordinal).All(m =>
                    Math.Abs(Pressures.BaseDaily(drive, m, 0.0, heartTarget)
                           - Pressures.BaseDaily(drive, m, 1.0, heartTarget)) < 1e-12);

                // The net drift over the day this townee actually lives — the question the old check
                // was not asking. Pressures.NetDaily owns the arithmetic (slot counts, pressure_rates,
                // and the per-slot gain/decay pick that an asymmetric trait like `wanderlust` uses to
                // move the break-even); reproducing any of it here is how this check got blind.
                double netDaily = Pressures.NetDaily(w, n, drive);
                var modes = ModeCounts(n);
                string modeText = string.Join(", ", modes.Select(kv => $"{kv.Key} {kv.Value}"));

                string kind =
                    flat ? (writers.Count == 0 ? "unwritten" : "quiet")
                    : drive == "purse" && n.Adventurer && floored ? "by-design"
                    : modeConstant && counters.Count == 0 ? "ratchet"
                    : "outgunned";

                string where = pegged ? "1.0" : floored ? "0.0" : $"{tail[^1]:0.000}";
                string clamp = pegged ? "the ceiling" : "the floor";
                string writerText = writers.Count == 0
                    ? "no authored effect touches it"
                    : string.Join(", ", writers.Select(x => $"{x.Id} {x.Delta:+0.##;-0.##} ×{x.Fires}"));

                // The two ways a bank can fail to push back read very differently to an author — an empty
                // list is "you never wrote one", a list that all points the same way as the drift is "you
                // wrote some and they are all shoving it further in". Same verdict, opposite first move.
                string bankText = writers.Count == 0
                    ? $"NOT ONE authored effect in the bank touches {n.Id}.{drive} at all"
                    : $"the bank's only authored effects on {n.Id}.{drive} are [{writerText}] — and every one "
                      + $"of them points the same way as the drift, deeper into {clamp}";

                string detail = kind switch
                {
                    "ratchet" =>
                        $"{n.Name}'s {drive} spent the last {TailDays} days of a {ObservedDays}-day run against "
                        + $"{clamp} ({where}), and NOTHING AUTHORED CAN LIFT IT OFF. Its drift ignores the drive's "
                        + $"current value, so the day this townee lives ({modeText}) is worth a flat "
                        + $"{netDaily:+0.####;-0.####}/day forever — a shape with no interior fixed point at any "
                        + $"value, which is Pressures' own statement about a mode-constant drive. And "
                        + $"{bankText}. This is not a pressure; it is a finished countdown. Fix: author an effect "
                        + $"that pushes it back (it must beat {Math.Abs(netDaily):0.####}/day sustained), change the "
                        + $"dayplan's mode balance, or give the drive a rest point so it has somewhere to stand.",

                    "outgunned" =>
                        $"{n.Name}'s {drive} spent the last {TailDays} days against {clamp} ({where}) even though "
                        + $"the bank pushes back: [{string.Join(", ", counters.Select(c => $"{c.Id} {c.Delta:+0.##;-0.##} ×{c.Fires}"))}] "
                        + $"= {bankPerDay:+0.####;-0.####}/day against a drift of {netDaily:+0.####;-0.####}/day "
                        + $"over this townee's day ({modeText}). The authored content exists and loses. NOT gated: "
                        + $"what it would cost to win is a fire-rate question, not an authored fact, so a retune "
                        + $"moves this number and a gate must not rest on it. Fix: raise the delta, shorten the "
                        + $"cooldown, or widen the predicate so it fires more than {counters.Sum(c => c.Fires)} "
                        + $"time(s) in {ObservedDays} days.",

                    "by-design" =>
                        $"{n.Name}'s purse drains to 0.0 by day {ClampDay(log, spd, floored: true)} and stays "
                        + $"({netDaily:+0.####;-0.####}/day over {modeText}) — and they are an adventurer, so this "
                        + $"is the design, not a defect: Pressures' own remarks call an adventurer's purse draining "
                        + $"to 0 \"the economy pushing them at the board\". NOT gated. It is listed because a drive "
                        + $"pinned at a clamp still cannot gate a storylet, so if anything reads {n.Id}.purse as "
                        + $"fuel, that rule is reading a constant.",

                    "unwritten" =>
                        $"{n.Name}'s {drive} never moves — {travel:0.####} total travel across the last {TailDays} "
                        + $"days, parked at {tail[^1]:0.000} — because NO AUTHORED EFFECT TOUCHES IT and the drift "
                        + $"alone has already converged on its rest point. NOT gated: this is the drive working "
                        + $"exactly as designed with an empty bank behind it. It is \"nobody wrote content for "
                        + $"{n.Id}.{drive}\", not \"{drive} is broken\" — and the repair is to write some.",

                    _ =>
                        $"{n.Name}'s {drive} never moves — {travel:0.####} total travel across the last {TailDays} "
                        + $"days, parked at {tail[^1]:0.000} — though effects ARE authored for it: [{writerText}]. "
                        + $"They fire too rarely to show up at all ({bankPerDay:+0.####;-0.####}/day averaged). NOT "
                        + $"gated. Different repair from `unwritten`: there is nothing to write here, only something "
                        + $"to let fire — check latch-die for whether these rules are gated shut.",
                };

                yield return new Finding("ratchets", $"{n.Id}.{drive}", detail,
                    new JsonObject
                    {
                        ["drive"] = drive,
                        ["kind"] = kind,
                        ["observed"] = pegged ? "pegged" : floored ? "floored" : "flat",
                        ["shape"] = modeConstant ? "mode-constant" : "restoring",
                        ["adventurer"] = n.Adventurer,
                        ["net_drift_per_day"] = Math.Round(netDaily, 6),
                        ["bank_per_day"] = Math.Round(bankPerDay, 6),
                        ["counter_pushes"] =
                            new JsonArray(counters.Select(c => (JsonNode)c.Id!).ToArray()),
                        ["writers"] = new JsonArray(writers.Select(x => (JsonNode)new JsonObject
                        {
                            ["storylet"] = x.Id, ["delta"] = x.Delta, ["fires"] = x.Fires,
                            ["anchored"] = x.Anchored,
                        }).ToArray()),
                        ["modes"] = ModesNode(modes),
                        ["observed_days"] = ObservedDays,
                        ["tail_days"] = TailDays,
                        ["tail_travel"] = Math.Round(travel, 6),
                        ["tail_min"] = Math.Round(tailMin, 4),
                        ["tail_max"] = Math.Round(tailMax, 4),
                        ["last"] = Math.Round(log[^1], 4),
                        ["model"] = "observed: World.PressureLog from a real Simulation run",
                    },
                    // Only `ratchet` is a proof. The rest say so in their own words and must not gate.
                    Class: kind == "ratchet" ? null : "warn");
            }
        }
    }

    /// <summary>Mode → slots per day, from the townee's <b>resolved</b> itinerary (Clockwork's own
    /// array), which is the whole point: the day as lived, not the modes the dayplan mentions.</summary>
    private static List<KeyValuePair<string, int>> ModeCounts(Townee n) =>
        n.Mode.GroupBy(m => m, StringComparer.Ordinal)
              .OrderBy(g => g.Key, StringComparer.Ordinal)
              .Select(g => new KeyValuePair<string, int>(g.Key, g.Count())).ToList();

    private static JsonObject ModesNode(List<KeyValuePair<string, int>> modes)
    {
        var obj = new JsonObject();
        foreach (var (m, c) in modes) obj[m] = c;
        return obj;
    }

    /// <summary>The first day from which the curve never comes back off the clamp — the day the
    /// countdown finished. Read off the log, not inferred from the arithmetic.</summary>
    private static int ClampDay(List<double> log, int spd, bool floored)
    {
        for (int i = 0; i < log.Count; i++)
        {
            bool stays = true;
            for (int j = i; j < log.Count; j++)
                if (floored ? log[j] > ClampBand : log[j] < 1.0 - ClampBand) { stays = false; break; }
            if (stays) return i / spd + 1;
        }
        return log.Count / spd;
    }

    /// <summary>Every authored pressure effect on this (townee, drive), with the number of times the
    /// observed run actually fired the rule carrying it.
    /// <para><b>Unbound rules are included, and marked.</b> An unbound rule binds by search, so it has
    /// no fixed cast — but it can still land on this townee if they could satisfy the role its effect
    /// names, and its <c>Fires</c> count is then an upper bound rather than a fact about them. The old
    /// check dropped unbound rules entirely, which would have let a bank that <i>does</i> push back be
    /// reported as one that provably cannot.</para></summary>
    private static List<(string Id, double Delta, int Fires, bool Anchored)> Writers(
        Town t, Observation o, string towneeId, string drive)
    {
        var found = new List<(string Id, double Delta, int Fires, bool Anchored)>();
        foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            bool anchored = s.Binding is { Count: > 0 };
            double total = 0; bool any = false;
            foreach (var e in s.Effects)
            {
                if (e.Pressure is not { Length: > 0 } key) continue;
                int dot = key.IndexOf('.');
                if (dot < 0) continue;
                string role = key[..dot], d = key[(dot + 1)..];
                if (d != drive) continue;
                if (anchored)
                {
                    if (!s.Binding!.TryGetValue(role, out var id) || id != towneeId) continue;
                }
                else if (!RoleReachable(t, s, role, towneeId)) continue;
                total += e.Delta; any = true;
            }
            if (any && Math.Abs(total) > 1e-9)
                found.Add((s.Id, total, o.FiredOn.GetValueOrDefault(s.Id)?.Count ?? 0, anchored));
        }
        return found;
    }

    /// <summary>Could this townee bind to this role of an unbound storylet? The role has to exist and
    /// its trait requirement has to be one they hold.</summary>
    private static bool RoleReachable(Town t, StoryletDto s, string role, string towneeId)
    {
        if (!t.TowneeById.TryGetValue(towneeId, out var n)) return false;
        if (!s.Predicates.Copresent.Contains(role)) return false;
        return !s.Predicates.Trait.TryGetValue(role, out var need) || n.Traits.Contains(need);
    }

    // --- 10. latch / die observations ---------------------------------------------------

    /// <summary>
    /// <b>Runs the real <see cref="Simulation"/> for <see cref="ObservedDays"/> days and reports what
    /// actually happened.</b> Three verdicts, and keeping them apart is most of the value:
    /// <list type="bullet">
    ///   <item><c>never-fired</c> — it never fired once, though the cooldown left it eligible.</item>
    ///   <item><c>died</c> — it fired, then stopped, and has sat eligible ever since.</item>
    ///   <item><c>ungated</c> — it fired on <i>every</i> day the cooldown allowed and its pressure
    ///   predicate never once said no. Not a death: the rule works. The predicate is decoration and
    ///   the cooldown is the only thing pacing it, which is worth knowing and is a different repair.</item>
    /// </list>
    ///
    /// <para><b>Why this stopped being a prediction (measured 2026-07-16).</b> It used to run
    /// <see cref="Pressures.DriftSlot"/> forward 14 days with <i>the storylets switched off</i> and
    /// report which pressure predicates latched — and it called that honest because the model was
    /// stated. It was not honest, it was just documented. Against a 56-day run of the real engine it
    /// was right about 5 of 19 findings and <b>every single one of its 7 "can never fire again"
    /// predictions was false</b>: all 7 rules fire, and 5 of them fire at their cooldown cap.
    /// <c>market-cheer</c> was predicted dead from day 6 and fires 56/56. <c>the-inn-is-well-found</c>
    /// was predicted dead from day 8 and fires 28/28 — i.e. every day its 2-day cooldown permits.
    ///
    /// <para>It could not have been right, and the reason generalises: storylet effects dwarf base
    /// drift by 1–15× in this engine. A rule at <c>+0.05</c> on <c>cooldown_days: 1</c> moves its
    /// drive <c>+0.05/day</c> against a base drift of <c>-0.0033/day</c>. A check that models drift
    /// alone is reasoning about the weakest force in the system — and then telling an author to
    /// delete a rule that fires every single day. "The model is stated" does not rescue a model
    /// whose omitted term is fifteen times its included one.</para>
    ///
    /// <para><b>This was the last check still re-modelling the engine</b>, which is exactly why it
    /// was the wrong one. The same defect took <c>stranded-beats</c> down earlier the same day: it
    /// hand-modelled one of hearsay-lite's three carriage clauses and reported, at error class, that
    /// beats were dead while the summary was printing them. Both are the file's own doctrine paying
    /// out. Option (a) — model the effects too — is not a fix; simulating effects, cooldowns,
    /// bindings and co-presence forward *is* re-writing <see cref="Simulation"/>, and the copy would
    /// drift from the original the way every other copy here has.</para>
    ///
    /// <para><b>What it cannot tell you.</b> This is an observation over
    /// <see cref="ObservedDays"/> days at the town's authored config, not a proof. A rule that first
    /// fires on day 57 is reported as <c>never-fired</c> and the report says so. That is why all three
    /// verdicts are warn-class: this file reserves error class for findings that are provable, and
    /// "I watched for eight weeks" is not a proof. It is, however, never <i>false</i> — which the
    /// thing it replaced could not say.</para>
    /// </summary>
    /// <summary>Everything a <see cref="ObservedDays"/>-day run of the real <see cref="Simulation"/>
    /// tells us, gathered once. <see cref="LatchOrDie"/> reads the fire record; <see cref="Ratchets"/>
    /// reads the same run's <see cref="World.PressureLog"/> and its fire counts.
    /// <para><c>Read</c>/<c>Said</c> are keyed <c>"storyletId|role.drive"</c>: how often the engine
    /// consulted a pressure predicate, and how often it said yes.</para></summary>
    private sealed record Observation(
        World World,
        Dictionary<string, int> EligibleDays,
        Dictionary<string, List<int>> FiredOn,
        Dictionary<string, int> EligibleAfterLastFire,
        Dictionary<string, int> Read,
        Dictionary<string, int> Said);

    /// <summary>Run the real engine for <see cref="ObservedDays"/> days and write down what happened.
    /// Nothing here decides anything — the verdicts live in the checks that read this.</summary>
    private static Observation Observe(Town t)
    {
        var sim = new Simulation(t);
        var w = sim.World;

        var eligibleDays = new Dictionary<string, int>(StringComparer.Ordinal);
        var firedOn = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var eligibleAfterLastFire = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in t.Storylets) { eligibleDays[s.Id] = 0; firedOn[s.Id] = new List<int>(); eligibleAfterLastFire[s.Id] = 0; }

        // "storyletId|role.drive" -> how often the predicate was read, and how often it said yes.
        var read = new Dictionary<string, int>(StringComparer.Ordinal);
        var said = new Dictionary<string, int>(StringComparer.Ordinal);

        // Sample at the engine's own read moment: after the drift, before any effect lands. Anything
        // later is a different number on every slot where something fired. And only while the rule is
        // off-cooldown — RunSlot skips a cooled rule before TryBind, so counting those slots would be
        // counting answers to a question nobody asked.
        sim.SlotOpening += (_, _) =>
        {
            foreach (var s in t.Storylets)
            {
                if (s.Binding is not { Count: > 0 }) continue;      // unbound: binds by search, no fixed cast
                if (s.Predicates.Pressure.Count == 0) continue;
                if (StoryletEngine.OnCooldown(w, s)) continue;
                foreach (var (key, pp) in s.Predicates.Pressure)
                {
                    int dot = key.IndexOf('.');
                    if (dot < 0) continue;
                    string role = key[..dot], drive = key[(dot + 1)..];
                    if (!s.Binding.TryGetValue(role, out var id) || !w.TowneeById.TryGetValue(id, out var n)) continue;
                    string tk = $"{s.Id}|{key}";
                    read[tk] = read.GetValueOrDefault(tk) + 1;
                    if (StoryletEngine.PressureMatches(pp, n.Pressure(drive))) said[tk] = said.GetValueOrDefault(tk) + 1;
                }
            }
        };

        for (int i = 0; i < ObservedDays; i++)
        {
            int day = w.Day;
            var eligibleToday = t.Storylets.Where(s => !StoryletEngine.OnCooldown(w, s))
                                           .Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var id in eligibleToday) eligibleDays[id]++;

            sim.RunToDawn();

            // world.Cooldowns[id] == day is the engine's own record that the rule fired today —
            // read, not re-derived, and it catches rules with no chronicle effect too.
            foreach (var s in t.Storylets)
            {
                bool fired = w.Cooldowns.TryGetValue(s.Id, out int last) && last == day;
                if (fired) { firedOn[s.Id].Add(day); eligibleAfterLastFire[s.Id] = 0; }
                else if (eligibleToday.Contains(s.Id) && firedOn[s.Id].Count > 0) eligibleAfterLastFire[s.Id]++;
            }
        }

        return new Observation(w, eligibleDays, firedOn, eligibleAfterLastFire, read, said);
    }

    private static IEnumerable<Finding> LatchOrDie(Town t, Observation o)
    {
        var (eligibleDays, firedOn, eligibleAfterLastFire, read, said) =
            (o.EligibleDays, o.FiredOn, o.EligibleAfterLastFire, o.Read, o.Said);

        foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            int elig = eligibleDays[s.Id];
            var fired = firedOn[s.Id];
            var preds = s.Predicates.Pressure.Keys.OrderBy(x => x, StringComparer.Ordinal)
                         .Select(k => $"{s.Id}|{k}").Where(read.ContainsKey).ToList();
            var predData = PredicateData(s, preds, read, said);
            string cd = $"cooldown_days {s.Predicates.CooldownDays}";

            if (fired.Count == 0)
            {
                yield return new Finding("latch-die", s.Id,
                    $"'{s.Id}' never fired once in a {ObservedDays}-day run of the real engine, though its "
                    + $"cooldown left it eligible on {elig} of those {ObservedDays} days ({cd}) — so the "
                    + $"cooldown is not what is stopping it, a predicate is"
                    + PredicateBlame(preds, read, said)
                    + ". Observed, not predicted: this is what Simulation did, not what a model says it "
                    + "would do. It is not a proof — a rule whose day comes at day 57 looks identical "
                    + "from here.",
                    Data("never-fired", elig, fired, predData, s));
                continue;
            }

            if (eligibleAfterLastFire[s.Id] >= DeadTailDays)
            {
                yield return new Finding("latch-die", s.Id,
                    $"'{s.Id}' fired {fired.Count} time(s) in a {ObservedDays}-day run — last on day "
                    + $"{fired[^1]} — and has sat off-cooldown on {eligibleAfterLastFire[s.Id]} day(s) since "
                    + $"without firing again ({cd}). It worked and then stopped"
                    + PredicateBlame(preds, read, said)
                    + ". Observed over the run, so it is a fact about these "
                    + $"{ObservedDays} days rather than a claim about all of them.",
                    Data("died", elig, fired, predData, s));
                continue;
            }

            // Ungated: fired at every opportunity the cooldown gave it, and the predicate never
            // objected at any slot the engine actually consulted it.
            bool atCap = elig > 0 && fired.Count == elig;
            bool neverSaidNo = preds.Count > 0 && preds.All(k => said.GetValueOrDefault(k) == read[k]);
            if (!atCap || !neverSaidNo) continue;      // it is doing its job; say nothing

            string list = string.Join(", ", preds.Select(k =>
                $"{k.Split('|')[1]} {Threshold(s, k.Split('|')[1])} (true at {said.GetValueOrDefault(k)}/{read[k]} reads)"));
            yield return new Finding("latch-die", s.Id,
                $"'{s.Id}' fired on all {fired.Count} of the {elig} day(s) its cooldown left it eligible "
                + $"({cd}) across a {ObservedDays}-day run, and its pressure predicate(s) — {list} — never "
                + "once said no at any slot the engine read them. The rule is not dead; it is UNGATED. The "
                + "cooldown is the only thing pacing it and the pressure predicate is decoration: deleting "
                + "it would change nothing, which is the useful fact here. If it is meant to gate, the "
                + "threshold is on the wrong side of where this drive actually sits.",
                Data("ungated", elig, fired, predData, s));
        }
    }

    /// <summary>The predicate-by-predicate evidence, which is the part an author acts on: it names
    /// which threshold is the one never being met.</summary>
    private static JsonObject PredicateData(StoryletDto s, List<string> keys,
        Dictionary<string, int> read, Dictionary<string, int> said)
    {
        var obj = new JsonObject();
        foreach (var k in keys)
        {
            string key = k.Split('|')[1];
            obj[key] = new JsonObject
            {
                ["threshold"] = Threshold(s, key),
                ["reads"] = read[k],
                ["said_yes"] = said.GetValueOrDefault(k),
            };
        }
        return obj;
    }

    /// <summary>A pressure predicate a rule never once satisfied is the first thing to look at, so
    /// name it inline rather than leaving it in the JSON only.</summary>
    private static string PredicateBlame(List<string> keys, Dictionary<string, int> read, Dictionary<string, int> said)
    {
        var never = keys.Where(k => said.GetValueOrDefault(k) == 0).Select(k => k.Split('|')[1]).ToList();
        if (never.Count > 0)
            return $". Its pressure predicate(s) [{string.Join(", ", never)}] were true at 0 of the "
                 + $"{keys.Where(k => said.GetValueOrDefault(k) == 0).Sum(k => read[k])} slots the engine read them, "
                 + "so at least one of them is the reason";
        return keys.Count > 0
            ? $". Its pressure predicate(s) [{string.Join(", ", keys.Select(k => $"{k.Split('|')[1]} true at {said.GetValueOrDefault(k)}/{read[k]}"))}] "
              + "did sometimes hold, so look at co-presence, the awake gate, regard or traits too"
            : ". It authors no pressure predicate, so look at co-presence, the awake gate, regard or traits";
    }

    private static string Threshold(StoryletDto s, string key)
    {
        if (!s.Predicates.Pressure.TryGetValue(key, out var pp)) return "";
        var parts = new List<string>();
        if (pp.Above is double a) parts.Add($"> {a:0.###}");
        if (pp.Below is double b) parts.Add($"< {b:0.###}");
        return parts.Count > 0 ? string.Join(" and ", parts) : "(unconstrained)";
    }

    private static JsonObject Data(string verdict, int elig, List<int> fired, JsonObject preds, StoryletDto s) => new()
    {
        ["storylet"] = s.Id,
        ["verdict"] = verdict,
        ["observed_days"] = ObservedDays,
        ["eligible_days"] = elig,
        ["fired_days"] = fired.Count,
        ["at_cooldown_cap"] = elig > 0 && fired.Count == elig,
        ["cooldown_days"] = s.Predicates.CooldownDays,
        ["first_fired_day"] = fired.Count > 0 ? fired[0] : null,
        ["last_fired_day"] = fired.Count > 0 ? fired[^1] : null,
        ["pressure_predicates"] = preds,
        ["model"] = "observed: a real Simulation run, storylet effects and all",
    };

    // --- 11. tellability histogram ------------------------------------------------------

    private static IEnumerable<Finding> TellabilityHistogram(Town t)
    {
        var scored = t.Storylets.Where(s => s.Effects.Any(e => e.Chronicle))
                                .Select(s => (s.Id, Tell: Tellability(s)))
                                .OrderByDescending(x => x.Tell)
                                .ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        if (scored.Count == 0) yield break;

        int want = t.Config.SummaryLines;
        var buckets = new JsonObject();
        foreach (var g in scored.GroupBy(x => Math.Min(9, (int)(x.Tell * 10)))
                                .OrderByDescending(g => g.Key))
            buckets[$"{g.Key / 10.0:0.0}-{(g.Key + 1) / 10.0:0.0}"] = g.Count();

        // The cut: if the whole bank fired on one day, the top `summary_lines` by score are shown.
        //
        // WHAT THIS MEANS DEPENDS ENTIRELY ON novelty_decay, and saying it unconditionally was a lie
        // the moment that knob landed (2026-07-16). Tellability is authored-static, so at decay 1.0
        // the cut is permanent — a rule below it is unreachable forever, which is what this finding
        // was written to expose. Below 1.0 the cut is a *starting* position: a told rule is fatigued
        // by decay^(recent tellings) and sinks past fresher ones, and the live town measurably tells
        // rules at the very bottom of its bank (waterline 0.25 = the bank floor, 14 nights).
        //
        // This file has made exactly this mistake before, one section up: `stranded-beats` modelled
        // only clause (b) of hearsay-lite and called beats provably untellable while the sim was
        // busy telling them. The note there is the one that matters here too — a check that cries
        // wolf gets an author to delete good content. So the finding reports the regime it is in.
        double cut = scored.Count > want ? scored[want - 1].Tell : 0.0;
        var below = scored.Skip(want).Select(x => x.Id).ToList();
        var atCut = scored.Where(x => Math.Abs(x.Tell - cut) < 1e-9).Select(x => x.Id).ToList();
        bool fatigueOn = t.Config.NoveltyDecay < 1.0;

        yield return new Finding("tellability", "bank",
            $"{scored.Count} chronicling rule(s), summary_lines={want}: {below.Count} sit below the cut "
            + $"(tellability < {cut:0.00}) on a night the whole bank fires"
            + (fatigueOn
                ? $" — but novelty_decay={t.Config.NoveltyDecay:0.##} fatigues a rule by that factor per recent "
                  + "telling, so the cut is a starting order, not a ceiling: rules below it surface as the ones "
                  + "above them tire. Do NOT retune these upward on the strength of this count alone; check "
                  + "`variety.rules_fired_but_never_told` in --report, which measures what actually reached a player."
                : " and, at novelty_decay=1.0, are only ever seen on a light night — tellability is "
                  + "authored-static, so with fatigue off this cut is permanent and they are unreachable by design.")
            + (atCut.Count > 1
                ? $" {atCut.Count} tie exactly at the cut ({string.Join(", ", atCut)}), so which one shows is "
                  + "decided by slot order, not by authoring"
                  // Worth stating rather than leaving to be rediscovered: fatigue does not break a tie,
                  // it *sequences* one. The tiebreak is static, so within a tied cluster the same member
                  // wins every time until it is told and fatigued, and the cluster drains in id order
                  // rather than fairly. This is the mechanism behind the rules that still never surface.
                  + (fatigueOn ? " — fatigue then drains the tied cluster in that same order, one telling at a time" : "")
                : ""),
            new JsonObject
            {
                ["summary_lines"] = want, ["bank"] = scored.Count,
                ["cut_tellability"] = cut,
                // The regime, emitted so a machine reader does not have to parse the prose above to
                // know whether `below_cut` means "unreachable" or "starts lower".
                ["novelty_decay"] = t.Config.NoveltyDecay,
                ["cut_is_permanent"] = !fatigueOn,
                ["histogram"] = buckets,
                ["below_cut"] = new JsonArray(below.Select(x => (JsonNode)x!).ToArray()),
                ["tied_at_cut"] = new JsonArray(atCut.Select(x => (JsonNode)x!).ToArray()),
            });
    }

    // --- 13. line tokens ----------------------------------------------------------------

    /// <summary>
    /// Validates every authored <c>lines</c> triad against what <see cref="LineRenderer"/> actually
    /// resolves. <b>This is the check that catches fiction.</b>
    /// <para>The two failure modes are not equally survivable, and that asymmetry is the whole point:
    /// an unknown <i>role</i> (<c>{Q}</c>) renders verbatim — ugly, obvious, caught on first read. An
    /// unknown <i>drive</i> (<c>{A.purs}</c>, a typo for <c>purse</c>) renders <b>0.00</b>, because
    /// <c>Townee.Pressure()</c> returns 0.0 for any key it does not know and <c>World.Build</c> only
    /// ever populates the four. So "(purse 0.00)" ships, and it reads as a destitute man rather than
    /// as a bug. Nothing else catches it: <c>SchemaValidator</c> only checks the lines are non-empty,
    /// and the linter's own drive vocabulary comes from predicates, which never see the prose.</para>
    /// <para>Scope is storylet lines — the ones <c>StoryletEngine.BuildEntry</c> really renders.
    /// Posting-template lines are deliberately not checked here: nothing renders them at
    /// <c>PNO.M1</c>, and asserting a token vocabulary against a render path that does not exist yet
    /// would be inventing a contract rather than enforcing one.</para>
    /// </summary>
    private static IEnumerable<Finding> LineTokens(Town t)
    {
        var rx = new System.Text.RegularExpressions.Regex(@"\{([^}]+)\}");
        var drives = Town.Drives.ToHashSet(StringComparer.Ordinal);

        foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var roles = s.Predicates.Copresent.ToHashSet(StringComparer.Ordinal);
            foreach (var (register, text) in new[]
                     {
                         ("hearsay", s.Lines.Hearsay), ("gossip", s.Lines.Gossip), ("report", s.Lines.Report),
                     })
            {
                foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
                {
                    string tok = m.Groups[1].Value;
                    if (tok is "place" or "slot") continue;

                    int dot = tok.IndexOf('.');
                    if (dot < 0)
                    {
                        if (roles.Contains(tok)) continue;
                        yield return TokenFinding(s, register, tok,
                            $"'{tok}' is not one of this storylet's copresent roles [{string.Join(", ", s.Predicates.Copresent)}] "
                            + "— it renders verbatim, braces and all", "unknown-role", roles, drives);
                        continue;
                    }

                    string role = tok[..dot], drive = tok[(dot + 1)..];
                    if (!roles.Contains(role))
                    {
                        yield return TokenFinding(s, register, tok,
                            $"role '{role}' is not one of this storylet's copresent roles [{string.Join(", ", s.Predicates.Copresent)}] "
                            + "— it renders verbatim, braces and all", "unknown-role", roles, drives);
                        continue;
                    }
                    if (drives.Contains(drive)) continue;

                    yield return TokenFinding(s, register, tok,
                        $"'{drive}' is not a drive ({string.Join("|", Town.Drives)}) — this renders as "
                        + "\"0.00\", NOT as an error: Townee.Pressure() returns 0.0 for an unknown key, so this "
                        + "line ships as plausible fiction and no test or playtest will ever flag it",
                        "unknown-drive", roles, drives);
                }
            }
        }
    }

    private static Finding TokenFinding(StoryletDto s, string register, string tok, string detail,
        string kind, HashSet<string> roles, HashSet<string> drives) =>
        new("line-tokens", $"{s.Id}:{register}:{{{tok}}}",
            $"'{s.Id}' {register} line has {{{tok}}}: {detail}",
            new JsonObject
            {
                ["storylet"] = s.Id, ["register"] = register, ["token"] = tok, ["kind"] = kind,
                ["silent"] = kind == "unknown-drive",
                ["valid_roles"] = new JsonArray(roles.OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
                ["valid_drives"] = new JsonArray(drives.OrderBy(x => x, StringComparer.Ordinal).Select(x => (JsonNode)x!).ToArray()),
            });

    // --- 14. partial bindings -----------------------------------------------------------

    /// <summary>Role counts <c>StoryletEngine.CandidateBindings</c>' <b>search</b> path enumerates.
    /// It has an arm for <c>roles.Count == 1</c> and one for <c>== 2</c>, and no <c>else</c> — so an
    /// unbound rule at any other arity falls off the end of the method and yields nothing. The anchored
    /// path has no such limit: it walks <c>roles</c> and yields when the binding covers them all.</summary>
    private static readonly int[] SearchableArities = { 1, 2 };

    /// <summary>
    /// A storylet that <c>StoryletEngine.CandidateBindings</c> can yield <b>no candidate binding</b> for,
    /// and which therefore <b>silently never fires</b>. Two ways in, and until 2026-07-16 this check saw
    /// only the first — while <i>recommending</i> the second.
    ///
    /// <list type="bullet">
    /// <item><c>partial</c> — anchored, but the <c>_binding</c> misses a <c>copresent</c> role.
    /// <c>CandidateBindings</c> yields the binding only when <c>b.Count == roles.Count</c> and then
    /// <c>yield break</c>s, so the search path is never reached and nothing is yielded.
    /// <c>SchemaValidator</c> proves the bound ids are real townees; it does not prove they are all
    /// there.</item>
    /// <item><c>unsearchable</c> — unbound, at an arity the search path has no arm for
    /// (<see cref="SearchableArities"/>: v0 handles 1 and 2). The method simply ends, and an unbound
    /// 3-role rule yields nothing for exactly the same reason a partial binding does.</item>
    /// </list>
    ///
    /// <para><b>The check's own advice used to manufacture the second one.</b> It skipped unbound rules
    /// — correct, they bind by search on purpose — and then told authors to <i>"drop <c>_binding</c>
    /// entirely to opt into the search path"</i>. Applied to a 3-role rule that produces, bit for bit,
    /// the defect this check exists to catch: a rule that never fires and never says so. The remediation
    /// was a recipe for the disease, in the sentence a reader trusts most. It is now arity-aware, and it
    /// tells a 3-role author the opposite thing.</para>
    ///
    /// <para><b>Latent, not live</b> (measured 2026-07-16): 0 instances in <c>data/</c> — every rule is
    /// anchored, and both 3-role rules carry a <c>_binding</c>. Which is the reason to fix it now rather
    /// than the reason not to: the only thing standing between this town and a silently dead rule was
    /// that nobody had taken the advice yet.</para>
    /// </summary>
    private static IEnumerable<Finding> PartialBindings(Town t)
    {
        foreach (var s in t.Storylets.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var roles = s.Predicates.Copresent;
            bool searchable = SearchableArities.Contains(roles.Count);
            string arities = string.Join(" or ", SearchableArities);

            if (s.Binding is not { Count: > 0 })
            {
                // Unbound. The search path binds it — if it has an arm for this arity at all.
                if (searchable) continue;
                yield return new Finding("partial-binding", s.Id,
                    $"'{s.Id}' authors no _binding, so it opts into the search path — but its copresent "
                    + $"roles are [{string.Join(", ", roles)}] ({roles.Count}), and "
                    + $"StoryletEngine.CandidateBindings only enumerates {arities} role(s) on that path. "
                    + $"There is no arm for {roles.Count}: the method falls off the end, yields NO candidate "
                    + "binding, and this storylet can never fire — the same silent death as a partial "
                    + "binding, reached from the other side. Nothing reports it at runtime; the rule is "
                    + "simply never a candidate. Fix: add a _binding naming EVERY role (what both of this "
                    + $"town's 3-role rules do), or cut the rule to {arities} copresent role(s). The limit "
                    + "is in the engine, not in this data — widening it means touching CandidateBindings, "
                    + "and that changes what fires bank-wide.",
                    new JsonObject
                    {
                        ["kind"] = "unsearchable",
                        ["copresent"] = new JsonArray(roles.Select(x => (JsonNode)x!).ToArray()),
                        ["role_count"] = roles.Count,
                        ["searchable_arities"] =
                            new JsonArray(SearchableArities.Select(x => (JsonNode)x).ToArray()),
                    });
                continue;
            }

            var missing = roles.Where(r => !s.Binding.ContainsKey(r))
                               .OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (missing.Count == 0) continue;
            yield return new Finding("partial-binding", s.Id,
                $"'{s.Id}' binds [{string.Join(", ", s.Binding.Keys.OrderBy(x => x, StringComparer.Ordinal))}] "
                + $"but its copresent roles are [{string.Join(", ", roles)}] — role(s) "
                + $"[{string.Join(", ", missing)}] are unbound, so no candidate binding is ever yielded "
                + "and this storylet can never fire. Fix: bind every role"
                // The advice splits on arity because the old one-size version created this check's own
                // defect at 3 roles. Never recommend the search path to a rule the search path cannot bind.
                + (searchable
                    ? $" — or drop _binding entirely to opt into the search path, which does handle "
                      + $"{roles.Count} role(s). (Those are different rules, not a fallback: unbound means "
                      + "'anyone who fits', anchored means this cast.)"
                    : $". Do NOT drop _binding here: StoryletEngine.CandidateBindings only enumerates "
                      + $"{arities} role(s) on the search path and this rule has {roles.Count}, so going "
                      + "unbound would yield no candidate either — you would trade one silently dead rule "
                      + "for another."),
                new JsonObject
                {
                    ["kind"] = "partial",
                    ["missing_roles"] = new JsonArray(missing.Select(x => (JsonNode)x!).ToArray()),
                    ["copresent"] = new JsonArray(roles.Select(x => (JsonNode)x!).ToArray()),
                    ["role_count"] = roles.Count,
                    // Whether the advice may point at the search path at all — the discriminator the old
                    // remediation text lacked, emitted so a reader can check the advice against it.
                    ["search_path_handles_this_arity"] = searchable,
                });
        }
    }

    // --- 12. unreachable postings -------------------------------------------------------

    private static IEnumerable<Finding> UnreachablePostings(Town t)
    {
        var named = t.Storylets.SelectMany(s => s.Effects)
                               .Select(e => e.Post?.Template)
                               .Where(x => !string.IsNullOrEmpty(x))
                               .ToHashSet(StringComparer.Ordinal)!;

        foreach (var p in t.Postings.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (named.Contains(p.Id)) continue;
            yield return new Finding("unreachable-posting", p.Id,
                $"posting template '{p.Id}' (requester {p.Requester}, reach {p.Reach}) is named by no "
                + "storylet's post effect — nothing can ever file it",
                new JsonObject { ["requester"] = p.Requester, ["reach"] = p.Reach });
        }
    }

    // --- output -------------------------------------------------------------------------

    /// <summary>Render the findings and <b>return the process exit code</b>.
    /// <para>The contract — "exit non-zero if any error-class finding <b>that a ruling has not
    /// accepted</b>" — is computed here, once, and the number this returns is the number printed. It is
    /// deliberately not re-derived at the call sites: the `load` path used to hand-return a literal 1
    /// next to a payload that said <c>"errors": 0, "ok": true</c>, and two places deciding the same
    /// thing is how they came to disagree. One source, reported and returned.</para>
    /// <para><b>What <c>errors</c> means in the payload, now that the concept has split.</b> It stays
    /// <i>the number that sets the exit code</i> — which is the meaning every consumer actually depends
    /// on, and it keeps the invariant this file bled for: <c>errors &gt; 0 ⟺ exit != 0 ⟺ !ok</c>. The
    /// split is carried by two <i>new</i> keys, <c>errors_accepted</c> and <c>errors_total</c>, so
    /// nothing that already greps this file starts quietly reading a different number. Emitting
    /// <c>"errors": 14, "ok": true</c> would have been the old load-path bug wearing a mirror.</para></summary>
    private static int Emit(List<Finding> findings, string townDir, string? reportPath,
        bool jsonToStdout, bool worldResolved, List<Acceptance>? ledger = null)
    {
        ledger ??= new List<Acceptance>();

        // Attach the ruling to every error-class finding the ledger names. The finding is otherwise
        // untouched — same class, same detail, same data — because it is the same finding it was.
        var matched = new HashSet<(int, string)>();
        findings = findings.Select(f =>
        {
            var hit = MatchOf(f, ledger);
            if (hit is null) return f;
            matched.Add((hit.Entry, f.Subject));
            return f with { Accepted = hit };
        }).ToList();
        if (worldResolved) findings.AddRange(UnmatchedAcceptances(ledger, matched));

        int errors = findings.Count(f => f.Gates);
        int accepted = findings.Count(f => f.Severity == "error" && f.Accepted is not null);
        int warns = findings.Count(f => f.Severity == "warn");
        int exit = errors > 0 ? 1 : 0;

        // Gating findings first, then the accepted proofs, then warnings — the same order the text
        // output uses. For a town with no ledger this is byte-identical to the old ordering: every
        // error gates, so the keys are 0 and 2 and "error" still sorts above "warn".
        var arr = new JsonArray();
        foreach (var f in findings.OrderBy(f => f.Gates ? 0 : f.Severity == "error" ? 1 : 2)
                                  .ThenBy(f => f.Check, StringComparer.Ordinal)
                                  .ThenBy(f => f.Subject, StringComparer.Ordinal))
            arr.Add(new JsonObject
            {
                ["check"] = f.Check,
                // NOT rewritten by an acceptance. An accepted ratchet is still error-class because it is
                // still a proof; `gates` is the axis that moved, and `gates` is what a machine should read.
                ["class"] = f.Severity,
                ["gates"] = f.Gates,
                ["accepted"] = f.Accepted is null ? null : new JsonObject
                {
                    ["entry"] = f.Accepted.Entry,
                    ["reason"] = f.Accepted.Reason,
                    ["ruling"] = f.Accepted.Ruling,
                    ["date"] = f.Accepted.Date,
                },
                ["subject"] = f.Subject, ["detail"] = f.Detail, ["data"] = f.Data.DeepClone(),
            });

        var root = new JsonObject
        {
            ["town"] = townDir,
            ["ok"] = exit == 0,
            ["errors"] = errors,
            ["errors_accepted"] = accepted,
            ["errors_total"] = errors + accepted,
            ["warnings"] = warns,
            ["exit"] = exit,
            ["world_resolved"] = worldResolved,
            ["findings"] = arr,
        };

        if (reportPath is not null) DataJson.WriteText(reportPath, root.ToJsonString(DataJson.Pretty));

        if (jsonToStdout) { Console.WriteLine(root.ToJsonString(DataJson.Pretty)); return exit; }

        Console.WriteLine($"lint · town={townDir}");
        Console.WriteLine(new string('-', 68));
        // Grouped by (bucket, check, accepting entry) rather than by check alone, and labelled from the
        // findings instead of from ErrorChecks membership. A check may now split across all three
        // buckets, and the old header would have printed [ERROR] over a by-design ghost-cast note that
        // does not gate — the one thing this section of the output exists to tell an author. Same rule
        // as `exit`: the label and the count come from the same place, so they cannot disagree.
        //
        // The accepting entry is in the key so one ruling prints its reason ONCE over the subjects it
        // covers, which is the shape the ruling actually had. It is keyed by the entry's index rather
        // than by the record, so two entries that happen to carry identical prose still group apart.
        foreach (var g in findings.GroupBy(f => (Bucket: f.Gates ? 0 : f.Severity == "error" ? 1 : 2,
                                                 f.Check, Entry: f.Accepted?.Entry ?? -1))
                                  .OrderBy(g => g.Key.Bucket)
                                  .ThenBy(g => g.Key.Check, StringComparer.Ordinal)
                                  .ThenBy(g => g.Key.Entry))
        {
            string label = g.Key.Bucket switch { 0 => "ERROR   ", 1 => "ACCEPTED", _ => "warn    " };
            Console.WriteLine($"[{label}] {g.Key.Check}  ({g.Count()})");

            // The ruling, above the findings it forgave, every single run. An accepted finding that
            // printed without its reason would be a suppression that had learned to say sorry.
            if (g.First().Accepted is { } a)
            {
                Console.WriteLine($"    ruled{(a.Date is null ? "" : $" {a.Date}")} · {a.Ruling}");
                Console.WriteLine($"    because: {a.Reason}");
                Console.WriteLine($"    ^ NOT FIXED and NOT DOWNGRADED — still class=error, still {g.Count()} "
                                  + "proof(s), printed here in full. Accepted means known, not gone. Any NEW "
                                  + $"'{g.Key.Check}' finding, or one more subject, still fails the build.");
            }

            foreach (var f in g.OrderBy(f => f.Subject, StringComparer.Ordinal))
                Console.WriteLine($"    · {f.Subject}: {f.Detail}");
        }
        Console.WriteLine(new string('-', 68));
        if (!worldResolved)
            Console.WriteLine("NOTE: the world did not resolve — occupancy/drift checks were skipped"
                              + (ledger.Count > 0
                                  ? ", so stale-acceptance reporting was skipped too (an unmatched "
                                    + "acceptance would be evidence about the skip, not about the ledger)"
                                  : "") + ".");
        if (reportPath is not null) Console.WriteLine($"report → {reportPath}");

        // The gate line. Machine-greppable, and it states the exit code rather than leaving a reader
        // to infer it — the last report of this tool claimed "15 errors / 104 warnings, exited 0",
        // which was a shell pipeline handing back the exit code of `tail`. The process said 1 the
        // whole time. Printing the number costs nothing and ends that class of bug report.
        //
        // `errors` is the gate number, so it reads 0 on a pass — but a PASS with accepted findings must
        // never be allowed to read as "this town is clean", or the ledger has quietly become the thing
        // it was built to prevent. So the pass line says the count out loud and names the file.
        Console.WriteLine($"errors={errors} accepted={accepted} warnings={warns} exit={exit}"
            + (exit == 0
                ? accepted == 0
                    ? "  (PASS — no error-class findings)"
                    : $"  (PASS — 0 UNACCEPTED error-class findings. {accepted} error-class finding(s) are "
                      + $"present and still defective; every one is accepted by a ruling in {AcceptedFile} "
                      + "and is printed above in full.)"
                : $"  (FAIL — {errors} unaccepted error-class finding(s) present"
                  + (accepted > 0 ? $"; {accepted} other(s) are accepted by a ruling" : "") + ")"));
        return exit;
    }
}

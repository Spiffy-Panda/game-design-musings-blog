# PLAN-fishbowl-postings-outings.handoff.md — the `PNO` build (reusable agent handoff)

**You are receiving this in a fresh chat, with the GameDesignMusings checkout.** Your job: implement
**`PNO` — postings & outings** in the village fish-bowl prototype, milestone by milestone, gate-checked
in-engine. The spec is [`PLAN-fishbowl-postings-outings.md`](./PLAN-fishbowl-postings-outings.md); this
document is your operating brief — the gate, the rules, the traps, the order of work, and what "done"
means.

Division of labor: **the spec says what to build and why; this doc says how to work.** Do not re-argue
the design — if you think a ruling is wrong, say so to Panda and stop. Do not silently redesign.

---

## 0 · STOP — read this before you write a line

**The build is gated. Nine rulings `PNO.D1`–`PNO.D9` are open as of 2026-07-16.** Check the spec's
**Status** line first.

- **If the rulings are unresolved:** you are not cleared to build. Ask Panda to rule, presenting the
  recommendation column from the spec's *The asks*. Do not guess, and do not "start on the safe parts" —
  `PNO.D1` (vocabulary) and `PNO.D2` (golden town posting-free) determine type names and test strategy
  from the first commit.
- **If Panda says "adopt the recommendations":** that is a full ruling — proceed on the recommended
  option for all nine, and record the adoption in the spec's *The asks* + `DEV-LOG.md`. This is exactly
  what happened for `VFB.D1`–`D4` on 2026-07-15; follow that precedent's paper trail.
- **If Panda rules against a recommendation:** fold the ruling into the spec **in place** (strike, don't
  renumber — house style), then build to it.

## 1 · The hard rules (non-negotiable, and they outrank your judgment)

**Rule 1 — no inline interpreter calls, VERBATIM, WITH A STERN WARNING.** No `python -c`, `python3 -c`,
`py -c`, `node -e`, etc. **Trigger: if `import` (or `require`, `using`, `#include`) appears in a command
line you are about to send to a shell, STOP.** Create `scrap_scripts/<lang>/<NN>_<slug>.<ext>` and run
that file instead. Shell one-liners are fine (`git status`, a single `grep`, `ls | head`); escalate to a
file the moment the one-liner grows loops, variables, conditionals, or more than a couple of pipes. Every
script — scrap or util — must anchor to the repo root (`Path(__file__).resolve().parents[N]` or the
language equivalent) so it runs from any CWD. **Pass this rule, verbatim and with a stern warning, into
every sub-agent prompt you write. Sonnet-tier models have ignored it before; do not be one of them.**

**The isolation rule (inherited from `VFB`, hard, standing).** You **do not read or modify**
`adventuring-guild-teller/morning-queue/**` or `plans/PLAN-morning-queue-tiers.md`. The fish-bowl shares
**no code** with the desk prototype and was built without reading it. This plan is where the two are most
tempted to touch — the desk prototype has its own postings bank and a dues rule — and `PNO.D9` says
**not now**. If you find yourself wanting to look, that is the rule working. The AGT plan's record of the
desk prototype is your only sanctioned channel. **Known-taken name: `moonwort`** is a desk-prototype item;
collision-check every new site/item name against the AGT plan's record, never by opening those files.

**Rule 3 — sync discipline.** Touching one tier means updating the others. The spec's *Sync footer* lists
exactly what `PNO` touches. **Force a sync check before any `git push`.** If you find an unexpected
desync, flag it to Panda, say docs need a resync, and **re-raise it at the start of every subsequent
phase until handled**.

**Rule 5 — `DEV-LOG.md` before every commit.** Append-only, newest on top, absolute dates. Git says
*what changed*; DEV-LOG says *why this, what you tried first, what would surprise the next person*.
Minimum one line; a paragraph for anything non-obvious. The bar is set by the existing entries — read the
2026-07-15 GTH entry for the register.

**Rule 7 — identity.** Machine paths and identity rules live in the gitignored `CLAUDE.local.md`. **Never
put a private absolute path, a dead name, or a real last name into a tracked file** — this doc set, code
comments, and commit messages included. Repo-relative paths only. The git author identity is already
pseudonymous and clean; commits may be pushed as-is.

**Rule 8 — handles.** Enumerated items in a repo page take the page's mnemonic (`PNO.D3`, `PNO.M1`,
`PNO.Q1`), no leading underscore. Handles are **stable and append-only** — strike, never renumber.

## 2 · Read-first chain (Rule 4) — and how to spend context

`CLAUDE.md` → `PLAN.md` → [`PLAN-fishbowl-postings-outings.md`](./PLAN-fishbowl-postings-outings.md)
(the spec — **read it whole, yourself**) → [`PLAN-village-fishbowl.md`](./PLAN-village-fishbowl.md) (the
parent; the machinery, determinism contract, and `VFB.Q1`) → `adventuring-guild-teller/fishbowl/FISHBOWL.md`
(the subproject's LLM entry point — layout, frozen interfaces, `test_id` table, harness) → the code.

Also relevant: `PLAN-adventuring-guild-teller.md` (the parent musing; `AGT.3` / `AGT.8` / `AGT.10` /
`AGT.11` / `AGT.12` are the contract inputs the spec cites), `PLAN-village-fishbowl.appendix-MinedUtilityAi.md`
(`MUA` — what was mined from the sibling UtilityAi project, and the determinism failure that motivated
the whole contract), `plans/PLAN-godot-test-harness.md` (`GTH` — how you verify).

### Delegation doctrine

Context is the scarce resource across a four-milestone build. Spend sub-agents on **breadth**; spend
yourself on the **engine**.

**Delegate** (fan out, run independent readers in parallel in a single message):

- the **drift check** (below) — the highest-value delegation in the build;
- doc-chain summaries of anything you need *oriented in* rather than *fluent in* (`MUA`, `GTH`, the
  `AGT` contract inputs);
- **storylet bank authoring** at `PNO.M1`/`M3` — JSON rules against a fixed schema, one agent per rule,
  each returning the file body. Cheap, parallel, and reviewable;
- **test sweeps** — "run `dotnet test`, return only failures with file:line";
- read-only reconnaissance you'd otherwise pay full file reads for.

**Never delegate** — these are yours, and a sub-agent's plausible-looking answer is worse than no answer:

- anything touching `Fishbowl.Core/Determinism/`, `World.ToHashNode`, stream derivation, or tick order.
  The determinism contract is the product (§4); a sub-agent that doesn't hold the whole contract will
  cheerfully reach for `HashCode.Combine` and everything will look fine until it doesn't;
- the **rulings** — `PNO.D1`–`D9` are Panda's, not a sub-agent's, and not yours;
- **red pre-existing tests** (§3) — diagnose those yourself, in full context;
- the **`PNO.Q*` findings** — the numbers are the point of the build; you report them, having seen them.

**Every sub-agent prompt you write carries Rule 1 verbatim with a stern warning, and the isolation rule**
(§1). Sub-agents do not inherit them by osmosis, and Sonnet-tier readers have broken Rule 1 in this repo
before. Scope every delegated glob/grep to `adventuring-guild-teller/fishbowl/` — a repo-wide sweep can
wander into `morning-queue/`.

### The drift check (fire this first, read-only)

The spec makes **22 falsifiable claims about the code**, verified 2026-07-16. The architecture rests on
about five of them — that `FireGate` draws no RNG at the default rate (so the golden town can stay
posting-free, `PNO.D2`); that no test pins a hash literal (so adding hash keys is free); that co-presence
and the summarizer already behave correctly off-screen (so a site can be an ordinary place); that the
phase machine generalizes a selector that already exists. **If any drifted, a milestone changes shape.**
Learn it in five minutes from a reader, not on day three from a red test — and have it checked by someone
who isn't invested in the plan being right.

```
You are a READ-ONLY briefing agent for the GameDesignMusings repo. Verify a spec's claims against
the code and return ONE terse report. You do NOT write or edit any file. Cite file:line. No
preamble. Your reader is an implementing agent whose context you are saving, not spending.

[PASTE RULE 1 VERBATIM + STERN WARNING HERE — see the handoff §1. You need no scripts for this
task: grep and read.]
[PASTE THE ISOLATION RULE HERE — scope every glob/grep to adventuring-guild-teller/fishbowl/.]

Read: plans/PLAN-fishbowl-postings-outings.md (the spec), adventuring-guild-teller/fishbowl/FISHBOWL.md,
then the code under adventuring-guild-teller/fishbowl/core/ and data/ (~2,700 lines — small enough
to read properly).

For EACH claim below return: CONFIRMED (file:line) or DRIFTED (what it says now, file:line).
One line per confirmed claim. Spend your words on drifted ones.

 B1. Townee.Away is a one-way trapdoor: Clockwork.ResolveDay does `if (world.Day > dd) t.Away = true;`
     with no return path. Brindle Ashe (departs_day 1) never comes home.
 B2. Clockwork.ResolveDay picks block lists via `(t.Away && plan.Away is { Count: > 0 }) ? plan.Away
     : plan.Weekday` — a state-driven variant selector already exists.
 B3. Clockwork.ResolveDay skips occupancy for the "away" anchor (`if (anchor == "away") continue;`),
     so an away townee is co-present nowhere.
 B4. data/places.json: guildhall-steps is board:true, shut:true, hours 0-48, capacity 12.
 B5. data/dayplans.json: adventurer-default authors an `away` block list (place: "away").
 B6. Rng.SubStream(worldSeed, day, streamName, key) exists in Determinism/Rng.cs, exposed as
     World.SubRngFor(stream, key).
 B7. StoryletEngine.FireGate early-returns WITHOUT drawing when `s.MustFire || rate >= 1.0` — so at
     the default storylet_rate of 1.0 the storylets stream is never advanced.
 B8. M1_ClockworkDeterminismTests.At_Default_Config_Hash_Is_Seed_Independent exists and asserts the
     day-1 hash is identical at seed 1123 and seed 999999.
 B9. NO test pins a hash literal: M1 compares run-to-run; M3_StoryletSummaryTests.Golden_Day_Reproduces_Its_Beats
     pins {storylet, participants} only; data/golden/day1.json carries no hash. Confirm all three.
 B10. StoryletEngine.CheckPredicates hardcodes the Flag predicate to one flag:
      `bool actual = flag == "departing_today" && ...DepartingToday;` — any other flag silently
      evaluates false and fails the predicate rather than erroring.
 B11. StoryletEngine.CandidateBindings yields townee ids only and handles 1-2 roles; CheckPredicates
      then intersects CommonPlace over every bound role.
 B12. StoryletEngine.Apply handles exactly three effect kinds: Regard, Pressure, Chronicle.
 B13. Townee.DayplanId is `init` ("authored identity — never mutated during a run").
 B14. Summarizer.Candidates filters to `e.Day == day` and IsCarried scans only the current day's
      occupancy — so an event at an off-screen site can never reach any summary.
 B15. The `chronicle_since: {days, kind}` predicate exists and works (ChronicleSinceDto).
 B16. copresence_bonus is read by NO engine code: declared (Model/TownDtos.cs), settable
      (World.SetKnob), projected (Api/WorldView.cs), authored (data/simconfig.json), UI-bound — and
      never consumed. Grep the whole subproject and say so plainly either way.
 B17. storylet_weight_mods is consumed by nothing; StoryletDto.Weight is used ONLY to order
      candidates in StoryletEngine.RunSlot.
 B18. World.ToHashNode includes `["away"] = t.Away`.
 B19. data/townees.json: Corvo Lunt has purse 0.2, trait indebted-shame, regard marrow-bray
      {score:-0.2, tags:["debtor"]}, and NO departs_day. Brindle Ashe has departs_day 1.
 B20. The bank is 12 storylets and includes stock-runs-low + fetch-arranged (the proto-posting pair)
      and debt-nagged. Confirm EVERY rule carries a _binding anchor.
 B21. Simulation.StepSlot order is Pressures.DriftSlot -> StoryletEngine.RunSlot ->
      World.RecordPressures; FinalizeDay is hash -> summary -> advance day -> Clockwork.ResolveDay.
 B22. Town.Drives is exactly four: purse, trade, heart, restlessness.

Then two short sections:

DESYNC (Rule 3): any place FISHBOWL.md, PLAN-village-fishbowl.md, PLAN.md, and the code disagree.

SURPRISES: anything the spec did NOT anticipate that an implementer would want to know. If the spec
is wrong about something load-bearing, say so plainly here. Do not propose a design — the design is
ruled elsewhere. Report what is there.
```

## 3 · The one invariant that governs the whole build

**`PNO` is strictly additive. Every pre-existing test stays green, verbatim.**

The mechanism (`PNO.D2`): the golden town stays **posting-free**, so it consumes no outing RNG, so
`M1_ClockworkDeterminismTests.At_Default_Config_Hash_Is_Seed_Independent` keeps passing and the golden day
keeps reproducing its 7 beats. Postings/sites live in a separate fixture town.

**Therefore: if a pre-existing test goes red, you broke something. Do not edit the test to make it pass.
Stop and report.** The golden day and the seed-independence invariant are the project's proof that the sim
never leaked wall-clock or process state — the exact illusion that broke the mined Autonome project
(`MUA.N1`). They are not obstacles; they are the product.

Day-hashes **will** shift once (new `phase`/`postings` keys). That is free — **no test pins a hash
literal**; M1 compares run-to-run, M3 pins beat types + participants, `data/golden/day1.json` carries no
hash. Note it in `DEV-LOG.md` and move on.

## 4 · The determinism contract (the #1 way to destroy this project)

- **Never** `DateTime.Now`, `Guid.NewGuid()`, `Math.Random`, or `HashCode.Combine` in `Fishbowl.Core`.
  Not "temporarily." Not in a test helper. `HashCode.Combine` is unseeded per-process and looks fine
  until it doesn't.
- All randomness comes from **named streams**: `Rng.Stream(seed, day, name)` /
  `Rng.SubStream(seed, day, name, key)`. `PNO` adds the `outings` stream and draws **per posting** via
  `world.SubRngFor("outings", posting.Id)` — so bank growth never shifts another posting's resolution.
- Single-threaded tick. Townees iterate in **stable id order**, places in stable id order. No
  `_process`-driven sim time; the engine steps only via `StepSlot()` / `RunToDawn()`.
- **Ordering (load-bearing, get this wrong and days drift):**
  - day boundary: `Outings.ResolveDay(world)` **before** `Clockwork.ResolveDay(world)` — clockwork reads
    the phase to choose the block list.
  - per slot: `Pressures.DriftSlot` → **`Outings.StepSlot`** → `StoryletEngine.RunSlot` → `RecordPressures`
    — so site storylets see the current leg.
- Godot's `JSON.stringify` float-ifies whole numbers (`4` → `4.0`). Tolerant int/long converters exist and
  are registered from day one; the test suite replays that round-trip over every real `data/` file. **Any
  new `data/` file joins that suite.**

## 5 · Traps (each of these will bite; they are in the code today)

1. **The `Flag` predicate is hardcoded to one flag.** `StoryletEngine.CheckPredicates` does
   `bool actual = flag == "departing_today" && ...DepartingToday;` — any other flag silently evaluates
   false and *fails the predicate rather than erroring*. Phases need `on_outing` / `in_cooldown`: make it
   a **dispatch table**, and make unknown flags **throw at load** in `Data/SchemaValidator.cs`.
2. **`CandidateBindings` yields townee ids only, and handles 1–2 roles.** `CheckPredicates` then
   intersects `CommonPlace` over *every* bound role. **Posting roles must bind from the board and be
   excluded from the co-presence intersection.** Keep townee-role arity ≤2 and the O(n²) search is
   untouched — that is why `PNO.D8` says solo outings in v1.
3. **This is the first system that cannot be `_binding`-anchored.** v0's whole bank is anchored, which is
   what makes the golden day exact. *Who takes which posting* is emergent by definition, so the search
   path stops being decorative and becomes load-bearing. Expect to find its rough edges first.
4. **`Townee.DayplanId` is `init` — "authored identity, never mutated during a run." Keep it that way.**
   Phase selects a *variant within* the plan (`plan.Outing ?? plan.Away` / `plan.Cooldown ?? cooldown-default`
   / `plan.Weekday`), which is the idiom already there. Do not add a settable plan id.
5. **`Away` is derived, not deleted.** `Phase == Outing`. This keeps `SetAway`, the `away` hash key, and
   `departs_day` working unchanged (`PNO.D6`).
6. **Do not wire up the two dead hooks as a drive-by.** `copresence_bonus` is a knob read by no engine
   code; `storylet_weight_mods` is authored on every trait and consumed by nothing. Connecting them
   changes what fires and would move `VFB.Q1`'s numbers under Panda mid-measurement. They have their own
   pending ruling. `PNO.D4` (self-selection weighting) is the *one* place the spec proposes touching
   weight logic — do it inside `PNO`'s own code path, not by retrofitting the trait mods.
7. **C# `[Signal]` names stay PascalCase in GDScript** (`bridge.SlotTicked.connect(...)`). JSON strings
   cross the bridge boundary — never typed objects.
8. **Tag every new control with `test_id` meta.** The observatory's UI is code-built; auto node names
   (`@HSlider@68`) shift on relayout, and the GTH harness resolves by `test_id`. New: `board`, `outings`,
   plus knob ids per the spec.
9. **`FISHBOWL.md` marks the bridge surface and data contract "frozen."** `PNO` unfreezes them — update
   that file as part of the work, don't route around it.

## 6 · Order of work

Milestones are in the spec with full accept criteria. **Gate-check each in-engine before the next opens** —
that is the `VFB` house discipline and it is why that build landed clean.

| # | Milestone | The gate |
|---|---|---|
| `PNO.M1` | the board (postings, no outings) | a shortage files a posting; it stands, ages, expires; every transition has a because-list; **all existing tests green** |
| `PNO.M2` | outings (phase machine, sites, legs, cooldown) | take → leave → findable at the site every slot → return → cooldown → daily life; **the one-way trapdoor is gone**; same-seed hashes reproduce editor-vs-CLI |
| `PNO.M3` | the loop closes | **the Corvo fixture** — takes a paying posting, returns `carried`, pays Marrow, `debtor` tag clears, `debt-nagged` stops firing. Then at `outing_hazard_scale: 3` he is routed and the axe lands on the board |
| `PNO.M4` | instruments | stats strip + CLI soak, **3 seeds × 14 days** (14, not 7 — an outing plus cooldown eats most of a week) → `PNO.Q1`/`Q2` answerable from the report JSON |

`PNO.M1` is a clean first slice and ships value alone: the board fills and empties with no outings in
sight. Do not skip ahead to outings because they're more fun.

## 7 · How to verify (never by eyeballing, never by the OS screenshotter)

```bash
# core — engine-free, no Godot needed. Run constantly.
dotnet test  adventuring-guild-teller/fishbowl/core/Fishbowl.Core.Tests/Fishbowl.Core.Tests.csproj
dotnet run   --project adventuring-guild-teller/fishbowl/core/Fishbowl.Cli -- --days 3 --chronicle
dotnet run   --project adventuring-guild-teller/fishbowl/core/Fishbowl.Cli -- --soak --days 14

# bridge compile-check without opening the editor
dotnet build adventuring-guild-teller/fishbowl/Fishbowl.csproj

# class_name registration needs an import pass on a fresh checkout (Godot is NOT on PATH —
# absolute path or the GODOT_STD env var; see CLAUDE.local.md)
"C:/Program Files/godot/godot.exe" --headless --path adventuring-guild-teller/fishbowl --import
```

**Drive and verify the running observatory with the GTH harness** — the `mcp__gth-fishbowl__*` tools
(`session_start` · `snapshot` · `click_element` · `capture` · `wait_for`) or a prescripted
`tests/harness/` scenario. **Never roll a new harness and never use the OS screenshotter.** Captures need
a **rendered window** — `--headless` has no framebuffer, so pixels come back blank (`GTH.D7`). Full
command API in `addons/gd_test_harness/README.md`.

The engine-free core is where the honest tests live: **if a thing can be tested without Godot, test it
without Godot.** That is what `VFB.D2` bought.

## 8 · Scope discipline (say no by list, not by accident)

Out of scope for `PNO` v0, per the spec: desk gameplay (the board is self-served; the desk stays shut) ·
floor verbs · teller assignment · tactical resolution (legs + hazard scalars, **never a combat sim**) ·
gear as inventory (gear-lost is a **flag and a posting**, not an item ledger) · parties (`PNO.D8`) · site
maps or pathfinding (a site is a graph node; travel is slot arithmetic) · injuries, morale, XP, levels ·
the desk prototype's vocabulary (`PNO.D9`) · **new drives — four stay four**.

If the work starts growing a fifth drive, an inventory, or a combat resolver, you have left the plan. Stop
and ask.

## 9 · Done means

1. **Code + tests** — new xUnit coverage for the board, the phase machine, and the Corvo fixture; the
   Godot-stringify round-trip suite extended over every new `data/` file; **all pre-existing tests green**.
2. **`fishbowl/FISHBOWL.md`** — bridge surface, data contract, `test_id` table, milestone status refreshed
   (it is the next agent's entry point; it is currently marked frozen, and you unfroze it).
3. **`fishbowl/README.md`** — the human code-doc.
4. **`DEV-LOG.md`** — an entry before every commit; the *why*, the options weighed, the surprises.
5. **The spec ticked** — `PNO.M*` status, `PNO.D*` adoption recorded, `PNO.Q*` **answered with numbers**.
   The research questions are the point of the build, not a footnote to it.
6. **`PLAN.md`** + [`PLAN-village-fishbowl.md`](./PLAN-village-fishbowl.md) index/pointer lines refreshed
   (Rule 3; the parent's `VFB.Q1` gains the `PNO.Q1` interaction).
7. **A report to Panda** that leads with the `PNO.Q*` findings — especially **`PNO.Q1`**: outings take
   bodies out of town and pay it back as a return-day burst, and `VFB.Q1` already sits at ~4.4 distinct
   lines/night with 3/21 nights below 4. **Whether this made the town quieter is the single most
   interesting number the build produces.** Say it plainly, with the figure, even if it's bad news —
   *especially* if it's bad news.

## 10 · Resource list

| What | Where |
|---|---|
| The spec (read whole) | [`PLAN-fishbowl-postings-outings.md`](./PLAN-fishbowl-postings-outings.md) |
| The kickoff prompt (Panda pastes it in a fresh chat; it sends you here) | [`PLAN-fishbowl-postings-outings.doc-reader.md`](./PLAN-fishbowl-postings-outings.doc-reader.md) |
| Parent plan (machinery, `VFB.Q1`, isolation) | [`PLAN-village-fishbowl.md`](./PLAN-village-fishbowl.md) |
| Mined-from-Autonome appendix (`MUA`) | [`PLAN-village-fishbowl.appendix-MinedUtilityAi.md`](./PLAN-village-fishbowl.appendix-MinedUtilityAi.md) |
| Grandparent musing (`AGT` contract inputs) | [`PLAN-adventuring-guild-teller.md`](./PLAN-adventuring-guild-teller.md) |
| Subproject entry point | `adventuring-guild-teller/fishbowl/FISHBOWL.md` |
| Harness (how you verify) | `plans/PLAN-godot-test-harness.md`, `adventuring-guild-teller/fishbowl/addons/gd_test_harness/README.md` |
| Repo rules | `CLAUDE.md` (Rules 1–8) · `CLAUDE.local.md` (gitignored: machine paths, identity) |
| Decision log | `DEV-LOG.md` |

**The fish-bowl is not published.** The site build copies only top-level `*.html` from a musing folder, so
`fishbowl/**` never deploys — Rule 6's public-surface gate doesn't bite here. Rule 7 still does: nothing
private reaches a **tracked** file, commit messages included.

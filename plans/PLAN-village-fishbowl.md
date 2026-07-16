# PLAN — village-fishbowl (the Roster pillar's town sim, as a playable observatory)

**Mnemonic:** `VFB` (gates `VFB.D*`, milestones `VFB.M*`, research questions `VFB.Q*`).
**Status:** **first release built 2026-07-15** — rulings `VFB.D1`–`VFB.D4` + `FB.8` adopted on the
recommendations (the staged `data/` already committed to them); **M0–M3 complete + gate-checked, M4
in place** (22 xUnit green, golden day reproduces, observatory boots clean). The code + frozen
interfaces live in [`../adventuring-guild-teller/fishbowl/FISHBOWL.md`](../adventuring-guild-teller/fishbowl/FISHBOWL.md);
build-time decisions are logged in `../DEV-LOG.md`. **Open:** `VFB.Q1` gossip-yield tuning (soak sits
at avg ~4.4 distinct lines/night — a live-knob research question, not a code gap).
**Parent musing:** `../adventuring-guild-teller/` (AGT). Proposal pages:
`fishbowl-studies.html` (machinery studies `FBS.1`–`FBS.6`) and `fishbowl.html`
(prototype claims `FB.1`–`FB.10`, the hand-cranked observatory mock).
**Appendix:** [`PLAN-village-fishbowl.appendix-MinedUtilityAi.md`](./PLAN-village-fishbowl.appendix-MinedUtilityAi.md)
(`MUA`) — what the fish-bowl mines from the sibling `../UtilityAi` (Autonome) project
vs. makes anew: jargon adoptions `MUA.J*`, patterns `MUA.M*`, rejections `MUA.N*`, and
open questions `MUA.Q*` that feed the `VFB.D*` rulings.
**Build target (on approval):** `../adventuring-guild-teller/fishbowl/` — a second
non-published subfolder beside `morning-queue/` (the site build copies only top-level
`*.html`, so the project never deploys).

---

## ⚠ Isolation rule (hard, standing)

Fish-bowl sessions **do not read or modify** `adventuring-guild-teller/morning-queue/**`
or `plans/PLAN-morning-queue-tiers.md`. The Morning Queue is a parallel workstream
mid-refactor (MQT); the fish-bowl was commissioned explicitly *without* reading it.

- **No shared code in v0.** Even where duplication is obvious (an RNG wrapper, JSON
  helpers, a capture harness), write the fish-bowl's own copy. Convergence into a shared
  library is a post-v1 decision for Panda, made once both prototypes have settled shape.
- **No name collisions with its content.** Cast and place names below are checked
  against what the AGT plan records of the desk prototype's directories; keep it that way
  without opening those files.
- The one sanctioned import is a **lesson, not code** — recorded in `../DEV-LOG.md`
  (2026-07-15): GDScript's `JSON.stringify` float-ifies whole numbers (`4` → `4.0`), so
  strict `System.Text.Json` int binding rejects engine-round-tripped payloads, and tests
  that feed raw file text never catch it. The fish-bowl core registers a tolerant
  int converter **from day one** and its test suite replays the Godot stringify
  round-trip over real `data/` files (see `VFB.M0` acceptance).

**Sub-agents:** every sub-agent prompt carries `CLAUDE.md` Rule 1 (no inline interpreter
calls) **verbatim, with a stern warning**, per `CLAUDE.md`.

## The brief (Panda, 2026-07-15 — the chat dump this plan serves)

> Without reading (or modifying) the morning-queue: spec out a playable prototype of the
> village fish-bowl part, in Godot (.NET code with GDScript and JSON data). The objective
> is the simulation of town life for NPCs, and the creation menus, first pass. For the
> initial build there is no "guild floor" gameplay — just data readouts of how things
> progress, and debug knobs. This is the more experimental part of the gameplay, so
> approach it like research: run game-design studies over several approaches at surface
> level, select one, spec it. Produce a `.md` for the LLMs and HTML pages proposing the
> prototype to the human boss.

This file is the `.md` for the LLMs. The HTML pages are the proposal to the boss.

## Contract inputs (what the pitch already fixed — cite, never fork)

- **The bowl is the substrate** the desk and floor act on (`AGT.2`, settled). Building it
  first and alone is the correct dependency order.
- **Day-cadence outside, async underneath** (`AGT.3`): the player meets the town at dawn
  (the Summary); expeditions span days; nothing pins to a morning tick.
- **Directed affinities, two-score acceptance** (`AGT.8`): suggestee→teller trust ×
  suggestee→target liking. The bowl must maintain directed dyad scores from day one, and
  refusals/events need **citable causes** (`AGR.2`).
- **The creator covers everyone; customization optional beyond required** (`AGT.9`), and
  it stays paperdoll + statlet + quotable prose — **never a scripter** (`AGR.5`).
- **Summaries may be actionable; bios hold every stat** (`AGT.10`); register is gossip,
  not telemetry (pillar III's rule). The "quotable, barely actionable" dial is now a
  **debug knob to be tuned, not a guess** (`AGR.3`).
- **No death** (`AGT.12`): nothing in the jar dies; failure produces material/story
  consequences, not grief.
- **The floor never ticks** (`AGT.11`) — the *bowl* ticks. The observatory's
  scrub/play-speed controls are debug affordances of the prototype, not player-facing
  promises.

## The machinery (selected by `FBS.1`–`FBS.6`; argued on the studies page)

**CPS — Clockwork · Pressures · Storylets**, three layers plus a summarizer:

1. **L1 Clockwork** *(adopted from `FBS.1`)* — every townee resolves an authored
   **day-plan** into a slot-by-slot itinerary (work, meals, haunts, home). Output: the
   **co-presence timeline** — who is where, together, when. Legible, findable, cheap.
2. **L2 Pressures** *(distilled from `FBS.2`/`FBS.5`)* — slow scalar drives per townee
   (`purse`, `trade`, `heart`, `restlessness`) and **directed regard** per dyad (score +
   tags like `kin`/`rival`/`courting`/`debtor`, plus reserved townee→teller regard).
   Pressures drift by rule (data-driven gains/decays fed by L1 context and L3 aftermath).
   They never pick actions; they are **fuel**.
3. **L3 Storylets** *(adopted from `FBS.4`)* — JSON-authored event rules with
   **predicates** over co-presence + pressures + regard + calendar. When one fires it
   mutates state and appends a **chronicle** entry that records the predicate snapshot
   that let it fire — explainability by construction (`AGR.2`).
4. **The Summarizer** *(register from the research page; selection lens from `FBS.6`,
   thinned)* — at dawn, picks 5±2 chronicle entries by **tellability** (type weight ×
   novelty decay × participant regard-delta), filtered through a **hearsay-lite** layer
   (an event reaches the summary only if a gossip-carrier was co-present or later shared
   a room with a witness — the summary quotes the town's telephone game, not the engine
   log). Renders each through the **actionability dial** (`hearsay` / `gossip` /
   `report` line variants). Events flagged `mark` append dated one-liners to
   participant **bios** (pending `FB.8` ratification; build toggleable).

**Declined:** GOAP/planner AI (`FBS.3` — opacity tax, overkill for a town read at dawn)
and ledger economies (`FBS.5` — `purse`/`trade` scalars only, no resource flows).

## Determinism (non-negotiable; the research method depends on it)

- Single-threaded tick; townees iterate in stable id order; places in stable id order.
- Seeded, **named RNG streams**: `plans`, `storylets`, `drift`, `gen` — each derived from
  `(world_seed, day, stream_name)` so adding a draw in one system never shifts another.
- **Day hash:** FNV-1a 64 over the canonical (sorted-key, ints-as-ints) state JSON at
  each dawn; shown in the observatory, logged by the CLI, pinned in golden tests.
- No wall-clock, no `Node._process`-driven sim time — the engine steps only via
  `StepSlot()` / `RunToDawn()`.

## Tick shape (pending `VFB.D1`)

A day = **48 half-hour slots** (recommended). Day-plans author in slots; storylet checks
run once per slot after movement resolves. Scrubbing *backward* in the observatory =
restore the dawn snapshot + re-sim forward (cheap at village scale, keeps one code path).

## Data (JSON, authored in `fishbowl/data/`, snapshots to `user://`)

All ints tolerant-parsed (see isolation rule). Every file carries `"version": 1`.
Sketches (field lists are the contract; exact C# names may vary):

```jsonc
// townees.json — one entry per townee
{ "id": "fenn-halloway", "name": "Fenn Halloway", "role": "fisher",
  "adventurer": false,                       // adventurers get the away-flag knob
  "traits": ["patient", "proud", "early-riser"],
  "dayplan": "fisher-default",               // template id; creator can inline a custom one
  "home": "karsk-rents", "work": "the-millpond",
  "haunts": ["market-row", "the-long-table"],
  "pressures": { "purse": 0.22, "trade": 0.55, "heart": 0.5, "restlessness": 0.3 },
  "regard": { "widow-karsk": { "score": -0.2, "tags": ["debtor"] } },   // directed, sparse
  "teller_regard": 0.5,                      // reserved for the floor (AGT.8)
  "bio": "Fenn has fished the millpond since his father's boat was his to lose…",
  "marks": [ /* sim-appended: {day, line} — behind the FB.8 toggle */ ] }
```

```jsonc
// storylets/rent-quarrel.json — one rule
{ "id": "rent-quarrel", "kind": "social",
  "predicates": {                            // all must hold; snapshot goes to the chronicle
    "copresent": ["A", "B"],                 // bound roles
    "regard": { "A->B": { "tag": "debtor", "flip": true } },  // B owes A
    "pressure": { "B.purse": { "below": 0.3 } },
    "cooldown_days": 3 },
  "weight": 1.4, "streams": "storylets",
  "effects": [ { "regard": "A->B", "delta": -0.15 },
               { "pressure": "B.heart", "delta": -0.1 },
               { "chronicle": true, "tellability": 0.8, "mark": ["B"] } ],
  "lines": {
    "hearsay": "Someone at {place} was shouting about money owed.",
    "gossip":  "{A} cornered {B} over the rent at {place} — voices carried.",
    "report":  "{A} demanded {B}'s arrears ({B} purse low) at {place}, slot {slot}." } }
```

```jsonc
// simconfig.json — every knob's default lives here (the knobs panel binds to these)
{ "slots_per_day": 48, "seed": 1123,
  "pressure_rates": { "purse": 1.0, "trade": 1.0, "heart": 1.0, "restlessness": 1.0 },
  "storylet_rate": 1.0, "storylet_cooldown_scale": 1.0,
  "copresence_bonus": 1.0, "hearsay_required": true,
  "actionability": 0.5, "summary_lines": 5, "bio_marks_enabled": true }
```

Also: `places.json` (id, name, kind, hours, capacity, **`board: bool`** — the six
place-board cards vs. private homes; homes like `karsk-rents` above are `board: false`,
still tracked for co-presence but not cardified), `dayplans.json` (named slot templates
with weekday/holiday variants), `traits.json` (trait → pressure-rate and storylet-weight
modifiers), `data/golden/` (the pinned golden-day cast + expected beats).

## Godot project skeleton (pending `VFB.D2`)

Godot **4.6.1 mono**, `gl_compatibility` renderer, no web-export assumption.

```
adventuring-guild-teller/fishbowl/
  FISHBOWL.md                  agent-nav spec for the subproject (M0 creates; frozen interfaces live here post-build)
  README.md                    human code-doc
  project.godot                autoload: FishbowlBridge (cs/FishbowlBridge.cs)
  Fishbowl.sln
  Fishbowl.csproj              Godot game project; references core/Fishbowl.Core
  cs/FishbowlBridge.cs         the ONLY C# the engine touches directly
  core/Fishbowl.Core/          engine-free classlib: model/ engine/ json/ text/
  core/Fishbowl.Core.Tests/    xUnit; includes the Godot-stringify round-trip suite
  core/Fishbowl.Cli/           headless runner: --town data/ --seed N --days 7 --report out.json
  scripts/                     GDScript UI panels (node scripting only; no sim logic)
  scenes/                      Observatory.tscn + panel scenes
  data/                        the JSON above
  .captures/                   capture-harness output (gitignored)
```

**Bridge surface** (C# autoload; GDScript calls it, JSON strings across the boundary):
`LoadTown(path)`, `GenerateTown(configJson)`, `StepSlot()`, `RunToDawn()`,
`GetRoster()`, `GetTownee(id)`, `GetPlaces()`, `GetChronicle(day)`, `GetSummary(day)`,
`GetPressureSeries(id, drive)`, `SetKnob(name, value)`, `GetKnobs()`,
`CreateTownee(json)`, `CreatePlace(json)`, `InjectStorylet(id, participantsJson)`,
`SaveSnapshot(path)` / `LoadSnapshot(path)`.
Signals: `slot_ticked(day, slot)`, `event_logged(event_json)`,
`dawn_ready(day, summary_json)`, `hash_ready(day, hash)`.

**Engine notes:** validate via the in-engine viewport-capture harness pattern (readable
`.captures/` folder, editor-toggleable, ships disabled) — never the OS screenshotter;
`class_name` registration needs a headless `--import` pass after fresh checkout; drive
runs via the godot MCP where available.

## The observatory (readouts — the only "gameplay" in v0)

- **Top bar:** day/slot clock · pause/1×/8×/dawn-skip · **day hash** chip · seed.
- **Roster panel:** sortable table (name, role, place-now, activity, top pressure);
  click → inspector.
- **Inspector:** authored bio + (toggle) sim-appended marks · four pressure sparklines
  (trailing 3 days) · directed regard list with tags, both directions · today's resolved
  itinerary.
- **Place board:** six place cards with live occupant chips — co-presence made visible.
- **Chronicle feed:** filterable by townee/type/day; every entry expands to its
  **because-list** (the predicate snapshot) and its three line variants.
- **Dawn summary:** the 5±2 lines as the player would read them, each linking back to
  its chronicle entry; re-renders live when the actionability dial moves.
- **Stats strip:** events/day by type · distinct-vs-repeat ratio · summary starvation
  warning (fires when < 4 candidate lines) — these are the `VFB.Q*` instruments.

## Debug knobs (bind to `simconfig`; all live without restart)

`seed` (re-seed + regen) · `pressure_rates.*` (0–3×) · `storylet_rate` (0–3×) ·
`storylet_cooldown_scale` · `copresence_bonus` · `hearsay_required` (bool) ·
`actionability` (0–1; snaps to the three authored stops) · `summary_lines` (3–7) ·
`bio_marks_enabled` (the `FB.8` toggle) · `away_flag(id)` (send/return an adventurer —
the expedition system's stand-in) · `inject grievance/shortage` (force-fire helpers) ·
snapshot save/load.

## Creation menus (first pass — the creator pillar's v0, `AGT.9`/`AGR.5` scoped)

- **Townee creator:** identity (name, role, portrait-placeholder initials) · statlets
  (starting pressures) · traits (pick ≤3) · day-plan (template pick + haunt overrides) ·
  home/work assignment · authored bio (multiline prose the summarizer may quote) ·
  starting regard rows. Writes a `townees.json`-shaped entry to `user://town/`;
  promotion into tracked `data/` is a deliberate manual copy, documented in the README.
- **Place creator:** name, kind, hours, capacity → `places.json`-shaped entry.
- **Town generator:** seeded — N townees, role mix sliders, relationship-density dial;
  guarantees the invariants (everyone has home+work; at least one gossip-carrier).
- **Storylet browser:** read-only list of loaded rules + force-fire (debug). Authoring
  storylets stays JSON-in-editor for v0 (pending `VFB.D3`).

## Milestones (each gate-checked in-engine before the next opens)

**Status (2026-07-15):** M0 ✅ · M1 ✅ · M2 ✅ · M3 ✅ (golden day reproduces its 7 beats) ·
M4 ◑ (generator + four menus + stats + soak shipped; `VFB.Q1` yield-tuning is the open research
question, not a build gap). Acceptance detail in `../adventuring-guild-teller/fishbowl/FISHBOWL.md`.

- **`VFB.M0` — scaffold.** Project boots; bridge echo test; `dotnet test` green including
  the **Godot-stringify round-trip suite** over every `data/` file; capture harness in
  place (ships disabled); `FISHBOWL.md` + `README.md` stubs. *Accept:* boot with zero
  engine errors; hash of an empty town stable across two runs.
- **`VFB.M1` — clockwork.** Golden cast loads; day-plans resolve; roster + place board +
  clock live; dawn-skip works. *Accept:* 12 townees × 3 days, deterministic day-hash
  sequence across two runs and across editor-vs-CLI; every townee findable at any slot.
- **`VFB.M2` — pressures.** Drives + directed regard drift by data rules; inspector
  sparklines; knobs live; snapshots round-trip. *Accept:* snapshot→load→run reproduces
  the no-reload hash sequence; pressure-rate knobs visibly bend sparklines.
- **`VFB.M3` — storylets + summary.** v0 bank (≥10 rules incl. the golden-day set);
  chronicle with because-lists; dawn summary + actionability dial + hearsay-lite;
  bio-marks behind the toggle. *Accept:* the **golden-day fixture** — seeded with the
  cast below, day 1 must produce the scripted beat *types and participants* (not exact
  text) pinned in `data/golden/`; dial's three stops render three distinct reads.
- **`VFB.M4` — creation + research instruments.** All four menus; town generator; stats
  strip; CLI soak (3 seeds × 7 days → report JSON). *Accept:* a menu-created townee
  appears in the next dawn's chronicle candidates; soak shows ≥4 distinct summary lines
  per night with ≤1 repeated type (else tune before calling it done — that's `VFB.Q1`).

## Research questions (what the prototype exists to answer)

- **`VFB.Q1` — gossip yield.** Does CPS at 12 townees sustain ≥4 distinct tellable
  events per night across a week without repetition fatigue? *(stats strip + CLI soak)*
- **`VFB.Q2` — explainability.** Can a reader trace any summary line to its causes in
  ≤2 clicks (line → chronicle → because-list)? *(walkthrough at M3 review)*
- **`VFB.Q3` — authoring cost.** New townee via menu ≤3 min; new storylet via JSON
  ≤15 min including its firing test. *(timed once at M4)*
- **`VFB.Q4` — determinism.** Same seed + data ⇒ identical hash sequence, editor and
  headless. *(from M1 on)*
- **`VFB.Q5` — the dial.** Do the three actionability stops read differently enough
  that Panda can pick a shipping default by feel? *(M3 review — this is `AGR.3`'s dial,
  finally measured)*

## The asks (rulings that gate the build)

**All adopted on the recommendation, 2026-07-15** (the staged `data/` had already committed to each;
Panda's "create the first release" confirmed). Build-time follow-on decisions — `_binding`-anchored
storylets, an awake gate, `departs_day` scheduling, regard-via-effects-only, 6-decimal hash
quantization — are recorded in `../DEV-LOG.md`.

- **`VFB.D1` — tick shape.** 48×30-min slots *(recommended)* vs 96×15-min vs continuous
  minutes. Sets schedule grain, scrub cost, authoring feel.
- **`VFB.D2` — core split.** Engine-free `Fishbowl.Core` + CLI + xUnit *(recommended:
  headless soaks and honest tests)* vs all-in-engine C#.
- **`VFB.D3` — storylet authoring surface.** JSON-only in v0 *(recommended; AGR.5
  discipline)* vs a minimal in-app editor.
- **`VFB.D4` — v0 town size.** 12 townees / 6 places / 2 adventurers *(recommended:
  readouts stay readable, tuning stays tractable)* vs 24+.
- **Ratify `FB.8`** (bio-marks): sim appends dated one-liners to authored bios —
  graduating the AGT plan's open Wildermyth-style question toward `AGT.13`. Built
  toggleable either way.

## The golden-day cast (shared fixture: the mock page hand-simulates it; M3 must reproduce its beats)

12 townees / 6 places, all original names (checked against AGT plan records; no overlap
with the desk prototype's directories): **Odile Vance** (innkeep, gossip-carrier),
**Petch** (herbalist, stock arc), **Marrow Bray** (smith), **Tam Underhill** (apprentice,
longing arc), **Brindle Ashe** (adventurer, departs mid-day), **Corvo Lunt** (adventurer,
owes Marrow for a re-hafted axe), **Sela Quick** (courier, co-presence vector),
**Widow Karsk** (landlady), **Fenn Halloway** (fisher, rent arc), **Dob Millet**
(miller), **Nan Pellow** (baker), **Grigg Paulet** (market warden).
Places: the Long Table (inn) · Petch's Simples · Bray & Daughter, Farriers · Market Row ·
the Millpond · Guildhall Steps (the desk itself stays shut in v0 — on purpose).
Scripted beats: rent quarrel (Karsk→Fenn) · stock-runs-low (Petch) + fetch-arranged
(Sela) · departure-farewell (Brindle; Tam's `heart` dips) · debt-nagged (Marrow→Corvo) ·
rumor-retold (Odile, hearsay chain) · market-squabble (Dob↔Nan, low stakes).

## Out of scope for v0 (say no by list, not by accident)

Desk gameplay · floor verbs · expedition resolution (the away-flag knob is the stand-in)
· any 2D/3D town view or pathfinding (places are graph nodes; travel is slot arithmetic)
· dialogue generation · belief distortion (hearsay-lite only) · in-app storylet editor ·
web export · art beyond placeholder chips.

## Sync footer (Rule 3)

Advancing this plan touches: this file (tick milestones) · `PLAN.md` (index line) ·
`../adventuring-guild-teller/ADVENTURING-GUILD-TELLER.md` (file map, once `fishbowl/`
exists) · `../adventuring-guild-teller/fishbowl.html` (fold rulings into `FB.*` claims,
strike-not-renumber) · `../DEV-LOG.md` before every commit.

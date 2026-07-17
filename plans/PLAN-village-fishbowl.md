# PLAN — village-fishbowl (the Roster pillar's town sim, as a playable observatory)

**Mnemonic:** `VFB` (gates `VFB.D*`, milestones `VFB.M*`, research questions `VFB.Q*`).
**Status:** **first release built 2026-07-15** — rulings `VFB.D1`–`VFB.D4` + `FB.8` adopted on the
recommendations (the staged `data/` already committed to them); **M0–M3 complete + gate-checked, M4
in place** (22 xUnit green, golden day reproduces, observatory boots clean). The code + frozen
interfaces live in [`../adventuring-guild-teller/fishbowl/FISHBOWL.md`](../adventuring-guild-teller/fishbowl/FISHBOWL.md);
build-time decisions are logged in `../DEV-LOG.md`. ~~**Open:** `VFB.Q1` gossip-yield tuning (soak sits
at avg ~4.4 distinct lines/night — a live-knob research question, not a code gap).~~
**`VFB.Q1` IS SATURATED — the metric cannot see the thing it was built to measure (2026-07-16).** It
reads a flat **5.00** and is `min(pool, summary_lines)` averaged over a pool of ≥20. It is not a tuning
question any more; it is a measurement defect. **Read the correction under *Research questions* before
citing this number anywhere.** Its replacement is `Api/Variety.cs`.
**Parent musing:** `../adventuring-guild-teller/` (AGT). Proposal pages:
`fishbowl-studies.html` (machinery studies `FBS.1`–`FBS.6`) and `fishbowl.html`
(prototype claims `FB.1`–`FB.10`, the hand-cranked observatory mock).
**Successor plan:** [`PLAN-fishbowl-postings-outings.md`](./PLAN-fishbowl-postings-outings.md) (`PNO`,
spec'd 2026-07-16, unbuilt) — the **board + outings** this plan deferred to the away-flag knob. It
resolves the out-of-scope line below, and its `PNO.Q1` bears directly on `VFB.Q1`: outings take bodies
*out* of town (fewer co-presence beats) and pay it back as a return-day burst, so gossip yield may
dip before it lifts. **Do not tune `VFB.Q1` against a posting-bearing town without reading `PNO.Q1`
first.**
**Appendix:** [`PLAN-village-fishbowl.appendix-MinedUtilityAi.md`](./PLAN-village-fishbowl.appendix-MinedUtilityAi.md)
(`MUA`) — what the fish-bowl mines from the sibling `../UtilityAi` (Autonome) project
vs. makes anew: jargon adoptions `MUA.J*`, patterns `MUA.M*`, rejections `MUA.N*`, and
open questions `MUA.Q*` that feed the `VFB.D*` rulings.
**Build target (on approval):** `../adventuring-guild-teller/fishbowl/` — a second
non-published subfolder beside `morning-queue/` (the site build copies only top-level
`*.html`, so the project never deploys).

---

## ⚠ Isolation rule — **discharged for the harness, 2026-07-16 (read this first)**

> **The convergence clause below has been ruled on.** Panda ruled `GTH.Q4` **full convergence** on
> 2026-07-16: the GTH test harness is now shared tooling — canonical at
> `../utils/godot/gd_test_harness/`, fanned into each Godot project by
> `../utils/python/sync_gth_addon.py --check` — and `morning-queue/`'s bespoke `DevHarness.gd` has
> been **retired onto it**. A fish-bowl session may now read and modify `morning-queue/` *for
> harness convergence*.
>
> **The rule was not broken; it was discharged on its own terms.** It names "a capture harness" as
> an example of duplication written *on purpose*, and says in so many words that "convergence into
> a shared library is a post-v1 decision for Panda, made once both prototypes have settled shape."
> Both settled; the decision got made; this is what it looks like when a deliberately-deferred
> decision comes due rather than rots. The workstream reason has also expired — the MQT refactor
> this rule was protecting completed 2026-07-15 (`MQT.6`).
>
> **What survives:** the *lesson-not-code* clause (still the right instinct), and the rule's value
> as history — it is why the fish-bowl's early code looks duplicated, which is otherwise a mystery
> to the next reader. Everything else below is now record, not law.

Fish-bowl sessions **do not read or modify** `adventuring-guild-teller/morning-queue/**`
or `plans/PLAN-morning-queue-tiers.md`. The Morning Queue is a parallel workstream
mid-refactor (MQT); the fish-bowl was commissioned explicitly *without* reading it.

- **No shared code in v0.** Even where duplication is obvious (an RNG wrapper, JSON
  helpers, a capture harness), write the fish-bowl's own copy. Convergence into a shared
  library is a post-v1 decision for Panda, made once both prototypes have settled shape.
  → **Called in for the test harness on 2026-07-16 (`GTH.Q4` = full convergence); still standing
  for everything else** (the RNG wrapper and JSON helpers stay per-project until someone rules
  otherwise). The trigger was concrete rather than aesthetic: six harness bugs were fixed in the
  fish-bowl's private copy on 2026-07-16, and any second copy would have been six bugs stale with
  nothing to say so. Duplication is cheap right up until the duplicate needs a fix.
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
runs via the godot MCP where available. **(2026-07-15)** the reusable **GTH test harness** was added as a
drop-in addon (`fishbowl/addons/gd_test_harness/`, additive to F9, inert unless activated) that drives the
observatory via synthetic input and captures to `.captures/gth/` — see `PLAN-godot-test-harness.md` +
`fishbowl/FISHBOWL.md`.

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
  > **The starvation warning has never fired and structurally cannot** (2026-07-16): the candidate
  > pool measures ≥20 every night against a threshold of 4. It is not evidence of health. Same
  > defect as `VFB.Q1` — see its correction below.

## Debug knobs (bind to `simconfig`; ~~all live without restart~~ — see the correction)

`seed` (re-seed + regen) · `pressure_rates.*` (0–3×) · `storylet_rate` (0–3×) ·
`storylet_cooldown_scale` · `copresence_bonus` · `hearsay_required` (bool) ·
`actionability` (0–1; snaps to the three authored stops) · `summary_lines` (3–7) ·
`bio_marks_enabled` (the `FB.8` toggle) · `away_flag(id)` (send/~~return~~ an adventurer —
the expedition system's stand-in) · `inject grievance/shortage` (force-fire helpers) ·
snapshot save/load.

> **Correction (2026-07-16 — found by the `PNO` drift check, verified against the code).**
> This list overstates three things, and `VFB.Q1` is being tuned with them:
>
> - **`copresence_bonus` is not live — nothing reads it.** Declared (`TownDtos.cs:115`),
>   settable (`World.cs:126`), projected (`WorldView.cs:171`), authored
>   (`simconfig.json:8`) — and **consumed by no engine code at all**. It also has **no
>   slider**: `Observatory.gd:167-176` builds exactly six controls and this is not one.
> - **`storylet_rate`'s upper half does nothing.** `FireGate` gates on `rate >= 1.0`
>   (`StoryletEngine.cs:75`) while the slider runs 0.0–3.0 (`Observatory.gd:171`), so
>   **1.0→3.0 is a no-op** — 2.5 behaves exactly like 1.0. Only 0.0→1.0 thins.
>   **`VFB.Q1` cannot be tuned upward with this dial.**
> - **`away_flag`'s return direction has never worked.** `World.SetAway` sets the flag then
>   calls `Clockwork.ResolveDay` **in the same call**, which re-runs
>   `if (world.Day > departs_day) t.Away = true` — so `SetAway("brindle-ashe", false)` on any
>   day ≥ 2 is a no-op. `departs_day` silently outranks the knob. (The one-way trapdoor
>   `PNO` exists to remove is tighter than either plan recorded.)
> - **Mirror:** `storylet_cooldown_scale` **is** consumed (`StoryletEngine.cs:68`) but likewise
>   has **no slider**. One live knob with no dial; one dial-less knob with no life.
>
> Neither dead hook is being fixed inside `PNO` — wiring them would change what fires and move
> `VFB.Q1`'s numbers mid-measurement. They need a ruling of their own.
>
> **Addendum (2026-07-16): a seventh knob landed, and it is the one that moves the needle.**
> `novelty_decay` (default **0.5**, slider 0.0–1.0, `test_id` `knob-novelty_decay`) fatigues a rule's
> claim on the summary by that factor per night it was **told** in the last 7. It is a **rendering**
> knob — it re-orders nights that are already finished, because the summary is derived on read rather
> than stored — so **drag it to 1.0 to ablate it and see the pre-2026-07-16 defect live**: a literal
> constant ranking that told 31 distinct sentences in a fortnight and never once told 20 of the 50 rules
> that fired. The `VFB.Q1` dial nobody could find was never `copresence_bonus`; the bank's floor was
> unreachable **by construction**, and no existing knob addressed it.
>
> **Why the old knobs could not have fixed it, which is why this needed code and not tuning.**
> `tellability` is *authored-static per rule* — `rent-quarrel` scores 0.800 every night it fires,
> `a-fair-hand` 0.250 every night it fires. `Score` was `tellability + 0.05 carrier bump`, sorted,
> `Take(5)`: not "mostly a leaderboard" but **literally a constant ranking**, with every rule below
> fifth place unreachable *forever*. No `storylet_rate`, no `hearsay_required`, no
> `storylet_cooldown_scale` reaches that.

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

**Status (2026-07-15):** M0 ✅ · M1 ✅ · M2 ✅ · M3 ✅ (golden day reproduces its ~~7~~ beats) ·
M4 ◑ (generator + four menus + stats + soak shipped; ~~`VFB.Q1` yield-tuning is the open research
question, not a build gap~~). Acceptance detail in `../adventuring-guild-teller/fishbowl/FISHBOWL.md`.

> **Updated 2026-07-16.** The suite is **53 tests** (was 22 at release, 30 mid-day). The golden day now
> pins **5** beats, not 7 — `FBT.Q1` ruled that `stock-runs-low` and `fetch-arranged` be dropped, both
> being **fossils of the trade ratchet**: they fired only because Petch's trade fell below 0.35, which
> only happened *because of the defect*. The fixture's acceptance list had been **pinning the bug rather
> than catching it**. And `VFB.Q1` is not an open tuning question — it is saturated; see *Research
> questions*.

> **⚠ M4 correction (2026-07-16) — the town generator has never worked from the observatory.** Found by
> a GTH harness pass driving the real UI (fired while opening `PNO`; the fish-bowl had not been driven
> end-to-end since release). **`btn-generate` fails every time**, and fails *quietly*:
>
> - `FishbowlBridge.cs:49` calls `TownLoader.Rebuild(World.Town, townees)` **before** it builds its
>   storyletless town at lines 50–57. `Rebuild` (`TownLoader.cs:63`) copies `Storylets = from.Storylets`
>   — the **old anchored** bank — swaps in the new cast, then validates; `SchemaValidator.cs:73` checks
>   every `_binding` against the new `TowneeById`, **all 20 dangle**, and it throws. **The
>   storylet-stripping at 50–57 is dead code** — it runs after the throw it was written to prevent.
> - The failure surfaces only as a transient `_toast` (`catch → Err(e)`), so the roster simply doesn't
>   change and it reads as "nothing happened".
> - **`M4_GeneratorTests.Generated_Town_Holds_The_Invariants_And_Validates` passes** — because its own
>   `BuildTownWith` helper strips storylets *before* validating. **The test exercises a path the bridge
>   does not take.** The green test is why this survived release. A generator test that doesn't go
>   through `GenerateTown` isn't testing the generator anyone can reach.
>
> Three more, same pass — none blocking, all real:
>
> - **`btn-storylets` is unreachable once a day has run.** The `hash` readout widens ~103px going from
>   `—` to 16 hex digits, shoving the 90px button from x=1173 to x=1276 in a **1280** viewport. The
>   force-fire debug tool is reachable only *before* there is anything to debug. Right-panel bios and
>   summary lines clip mid-word past x=1280 too.
> - **The `register` label lies about the lines beneath it.** `WorldView.SummaryJson` serves
>   `w.Summaries[day]` — cached at dawn — while `register` is recomputed live from the current dial. So
>   moving `actionability` across all three stops relabels the header while the five lines stay
>   character-for-character identical. The engine is right (a fresh day at 1.00 renders true report
>   lines); the readout is misleading. This is `VFB.Q5`'s instrument, and it half-lies.
> - **`hearsay_required` is unobservable on the golden data.** Toggled off, the summary is identical
>   — "events 12 · distinct types 12" either way. The gate never binds on seed 1123 because **every
>   event is already witness-carried**; the cast is thick with gossip-carriers. Not a bug — but it
>   means `VFB.Q1`'s hearsay dial cannot be measured on this fixture, which is worth knowing before
>   tuning against it.
>
> Ruled out of `PNO`'s scope (they are `VFB`'s), but the generator one is a **shipped feature that has
> never worked** and M4 is ticked as if it does. Panda's call on when.

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
  appears in the next dawn's chronicle candidates; ~~soak shows ≥4 distinct summary lines
  per night with ≤1 repeated type (else tune before calling it done — that's `VFB.Q1`)~~.
  > **The acceptance bar is void (2026-07-16).** "≥4 distinct summary lines per night" is
  > unfailable — it is `min(pool, 5)` against a pool of ≥20, and it holds with 30 of 46 rules
  > deleted. **M4 has been ticked against a test that cannot fail.** The honest replacement is
  > `Api/Variety.cs`, and it is deliberately un-scored, so **M4's acceptance is now a human
  > read of a number, not a gate** — see the `VFB.Q1` correction under *Research questions*.

## Research questions (what the prototype exists to answer)

- **`VFB.Q1` — gossip yield.** Does CPS at 12 townees sustain ≥4 distinct tellable
  events per night across a week without repetition fatigue? *(stats strip + CLI soak)*

  > **⚠ SATURATED — RETIRED AS A TARGET, KEPT AS A RECORD (2026-07-16, measured).** This question was
  > answered "yes, ~4.4" and the answer was honest. **The metric is broken, and it is broken in the way
  > that is hardest to see: it passes.**
  >
  > **The defect.** `VFB.Q1` is specified as "avg distinct *tellable* lines/night". The soak computes it
  > as the distinct texts **within one night's summary** — but the summary has already been truncated to
  > `summary_lines` (5) by the time it is counted. So the metric is **`min(pool, 5)` averaged**, and it
  > is asking "is the pool ≥ 5?" of a pool that measures **≥20 every night**. It reads a flat **5.00**
  > and it cannot move.
  >
  > **The ablation that settled it.** **Delete 30 of the 46 rules and it still reads 5.00, with 0/42
  > nights below 4.** It cannot see two-thirds of the bank. A metric that survives the deletion of most
  > of the content it measures is not measuring the content.
  >
  > **The old figure is superseded, not falsified — and the comparison IS the finding.** ~~avg ~4.4
  > distinct lines/night, 3/21 nights below 4~~ was truly measured on the 12-townee town of 2026-07-15.
  > **It is deliberately not deleted.** The pair of numbers — an honest 4.4 that meant something, and a
  > 5.00 that means nothing — is the whole lesson: the metric drifted into its own ceiling as the bank
  > grew, and *nothing said so*, because saturation looks exactly like success.
  >
  > **What it never asked, which is the part that mattered.** `VFB.Q1` never compares night N to night
  > N−1. Because `_binding` pins each rule's cast and `place` pins its room, a told rule renders the
  > **identical sentence** every time it fires. The town could say the same five things forever and score
  > a perfect 5.00.
  >
  > **The replacement: `Api/Variety.cs`** (`--soak`, `--report`). Distinct rendered **texts** — keyed on
  > the final sentence, never the rule id, since two fires of one rule with a different cast or room are
  > two different sentences and that distinction is the entire point. Plus told/fired,
  > rules-fired-but-never-told, repeat vs N−1 and vs any prior night. **Deliberately un-scored — no
  > threshold, because "a threshold invites tuning toward it"**, and it was written by a different agent
  > than the ones it judges. It reports; a human reads.
  >
  > **The live town, measured 2026-07-16** (3 seeds × 14 nights, all three seeds identical — this town
  > draws no RNG at its authored knobs, so the seed sweep measures one run three times):
  >
  > | | `novelty_decay=1.0` (ablated) | **0.5 (default)** |
  > |---|---|---|
  > | distinct sentences / 14 nights | 31 | **47** |
  > | delivered lines | 70 | 70 |
  > | novelty rate | 0.44 | **0.67** |
  > | repeat vs night N−1 | 0.00 | **0.00** |
  > | repeat vs any prior night | 0.60 | **0.35** |
  > | told / fired | 70/319 = 0.22 | 70/319 = 0.22 |
  > | rules fired but never told | 20 of 50 | **5 of 50** |
  >
  > **`told/fired` is structural and will not move.** It is `summary_lines × nights / beats fired` —
  > fixed by the truncation and the firing rate, invariant under **any** reordering. If it ever appears
  > to move, the summary has started affecting the sim.
  >
  > **Do not tune against the replacement either.** The `novelty_decay` sweep is the worked example:
  > measured against variety alone, **lower is monotonically better right down to 0.0** — and 0.0 means a
  > rule told once is silenced for a week regardless of how good it is, i.e. `tellability` stops meaning
  > anything. The metric applauds that. Distinct **saturates at 50 from 0.3 downward** while the rank
  > correlation between authored tellability and times-told collapses **0.76 (off) → 0.50 (0.5) → 0.33
  > (at 0.0)**. The last stretch spends authorial intent and buys nothing. **Variety cannot see that
  > axis, by construction. It is a good instrument; it is not a fitness function.**
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

Desk gameplay · floor verbs · expedition resolution (the away-flag knob is the stand-in
— **taken up 2026-07-16 by [`PLAN-fishbowl-postings-outings.md`](./PLAN-fishbowl-postings-outings.md)**)
· any 2D/3D town view or pathfinding (places are graph nodes; travel is slot arithmetic)
· dialogue generation · belief distortion (hearsay-lite only) · in-app storylet editor ·
web export · art beyond placeholder chips.

## Sync footer (Rule 3)

Advancing this plan touches: this file (tick milestones) · `PLAN.md` (index line) ·
`../adventuring-guild-teller/ADVENTURING-GUILD-TELLER.md` (file map, once `fishbowl/`
exists) · `../adventuring-guild-teller/fishbowl.html` (fold rulings into `FB.*` claims,
strike-not-renumber) · `../DEV-LOG.md` before every commit.

> **⚠ OUTSTANDING (raised 2026-07-16, not yet done).** Two items on this footer never landed,
> found while opening `PNO`:
>
> 1. **`fishbowl.html` still reads as awaiting the rulings** — the fold above never happened
>    (9 mentions of "ruling" survive). `VFB.D1`–`D4` + `FB.8` were adopted 2026-07-15 and the
>    build shipped the same day. **It is a public surface**, so Rule 6 applies; re-raise before
>    any push touching it. Not folded into `PNO` — it is a `VFB` chore and Panda's call.
> 2. **The musing's registration shipped a release late** — `97a3710` added `fishbowl.html` +
>    `fishbowl-studies.html` but committed no `MUSING-CONFIG.json` links, so both pages deployed
>    to Pages as orphans. **Fixed 2026-07-16** (`c55dccc`); logged in `../DEV-LOG.md`. The
>    Rule-2 nav spec had been updated and committed; only the registration tier was missed —
>    which is why it survived a release. Worth knowing the shape: "half a sync" hides.

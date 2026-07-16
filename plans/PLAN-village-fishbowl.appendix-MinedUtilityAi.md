# APPENDIX — PLAN-village-fishbowl: mined from UtilityAi (Autonome)

**Mnemonic:** `MUA` (jargon adoptions `MUA.J*`, mine `MUA.M*`, make-anew `MUA.N*`,
questions raised `MUA.Q*`).
**Parent plan:** [`PLAN-village-fishbowl.md`](./PLAN-village-fishbowl.md) (`VFB`).
**Source:** the sibling repo `../UtilityAi/` ("Autonome" / "PandasAutonome") — assessed
2026-07-15 by a four-subagent sweep (docs/spec · simulator core · Godot side · tooling),
read-only, with the morning-queue isolation rule in force. This file records what the
fish-bowl **mines** (vocabulary, shapes, lessons — never code) and what it **makes anew**.

---

## What Autonome is (one paragraph of context)

A hierarchical utility-AI world sim in **C#/.NET 8** — engine-free core
(`AutonomeSimulator/src/Autonome.Core`) plus a Godot 4.6.1 mono front-end
(`GodotProject/`, all C#, **no GDScript**) — simulating a coastal city of ~95–190
embodied NPCs and ~21 disembodied orgs across 41 locations. Every entity is an
**Autonome** with decaying float **Properties**; embodied ones score candidate actions by
utility curves each evaluation tick. It reached a tuned, working state (its
world-stability roadmap marks Phases 1–6 complete, 2026-03). It is a near-sibling of the
fish-bowl's planned shape — which makes both its wins and its scars unusually load-bearing.

## Headline findings

1. **The jargon maps almost 1:1** onto CPS L2/L3 — adopt it (`MUA.J*`), don't invent
   parallel terms.
2. **Its determinism is broken** — every "Deterministic\*" function derives from .NET
   `HashCode.Combine`, which is **seeded per process**: reproducible within one run,
   never across launches. No world seed exists anywhere, and the three xUnit test
   projects contain **zero tests**, so nothing ever caught it. This independently
   validates `VFB`'s countermeasures (named streams from `(world_seed, day,
   stream_name)`, FNV-1a day-hash, golden fixtures at `VFB.M0`). Port no RNG.
3. **The core split is proven.** `Core / Data / History / Analysis / Cli` layering — pure
   engine-free core, single `WorldState`, numbered tick phases, headless CLI — served a
   CLI, Godot, and a WebSocket host from one core. `VFB.D2` is empirically de-risked.
   Caveat: the Godot side never crosses a GDScript↔C# JSON boundary, so the fish-bowl's
   bridge design gets **no precedent** from it.

## `MUA.J` — jargon adoptions (the minimum import)

| # | Autonome term | Fish-bowl mapping |
|---|---|---|
| `MUA.J1` | **Property** — `{value, min, max, decayRate, critical}`; needs and resources share one shape ("same shape ⇒ same thing") | L2 pressure. A "need" is just a Property with nonzero decay + a critical threshold. |
| `MUA.J2` | **ResponseCurve** / **Keyframe** / named **curve presets** (`linear`, `constant`, `desperate`, `threshold_low`, `inverse_linear`, `smooth_step`) — piecewise cubic Hermite, [0,1]→[0,1], string-referenced from JSON | L2 drift shaping; summarizer tellability scoring. |
| `MUA.J3` | **magnitude** — scalar weight on a curve's output within one rule | storylet / tellability weighting term. |
| `MUA.J4` | **Modifier** — one unified struct for memories, passives, traits (`duration`, `decayRate`, **`intensity`**) | storylet effects and lingering regard nudges. |
| `MUA.J5` | **Relationship** — directed source→target, **tags**, stateful properties `affinity` / `familiarity` / `trust` | regard dyad. `trust` was added late, specifically to gate gossip fidelity — see `MUA.Q2`. |
| `MUA.J6` | **Requirements** — optional typed conditions ANDed (`propertyMin` / `propertyBelow` / `propertyMinAny` OR-bucket, `locationTags`, `timeOfDay`, `noActiveModifier`) | the storylet predicate schema shape. |
| `MUA.J7` | **gossipType** + trust-weighted propagation (`intensity × 0.5 × trust` per hop) | hearsay-lite and the actionability register's fidelity story. |
| `MUA.J8` | **Aggregation** — `avg/sum/min/max/count` × blend mode over a filtered group | summary rollups (drop the `ratio` arm — it was never implemented there). |
| `MUA.J9` | **vital zero-lock** — deterministic must-fire override inside weighted selection | the escape hatch pattern for must-fire storylets. |
| `MUA.J10` | **fragile equilibrium** — "decay is the clock"; production ≈ 90–95% of consumption so shocks cascade into story | the tuning north-star for pressure rates vs. storylet thresholds — see `MUA.Q7`. |
| `MUA.J11` | **chosen rank** — logging whether the winner was candidate #1/#2/#3 | chronicle instrumentation; feeds the because-list. |

## `MUA.M` — mine (re-implement the pattern, never the code)

- `MUA.M1` — **Property-unifies-needs-and-resources** with per-property decay/critical
  (`Autonome.Core/Model/Property.cs`). The strongest architectural lesson. Use the
  *minutes-scaled* decay formula (`value × rate × minutesPerSlot`, tick-granularity-
  independent) — not the flat per-tick variant.
- `MUA.M2` — **Hermite curve evaluator + JSON preset library**
  (`Runtime/CurveEvaluator.cs`, `data/curves.json`, `Data/CurvePresetLibrary.cs`).
  The cleanest, most portable asset in the repo; linear fast-path when tangents are 0.
- `MUA.M3` — **Softmax-over-top-K weighted selection with a deterministic must-fire
  override** (`SimulationRunner.WeightedRandomSelect`). Autonome learned that pure
  argmax makes one action win hundreds of ticks straight (margins ~0.08–0.11). Storylet
  selection and the summarizer's pick-5 will hit the same degeneracy — mine the math,
  feed it from a proper seeded stream (`MUA.N1`).
- `MUA.M4` — **Decision record = outcome + inputs**: `ActionEvent` carries ranked
  `TopCandidates` + full `PropertySnapshot` + chosen rank (`SimulationRunner.cs:327`).
  The proto because-list; the chronicle keeps the full predicate snapshot (per `AGR.2`)
  — Autonome shows the halfway version and its limits.
- `MUA.M5` — **The analysis/report layer** (`Autonome.Analysis/*`,
  `web/js/views/analysis.js`, `daily_rhythm.js`): decision margins, close-calls
  (margin < 0.05), consecutive-run detection, dominant-action %, per-property sparkline
  trajectories, sortable roster comparison table, `balance.md` verdict format,
  timestamped report-bundle folders with `meta.json`. This is the observatory stats
  strip and the CLI soak report, pre-designed. Its validation metrics (action diversity
  ≥5, max-consecutive <6–8, dominant-action <25–30%) are ready-made `VFB.Q1` numbers.
  Also: one stable color map per dimension, reused across every chart.
- `MUA.M6` — **System.Text.Json conventions** (`Data/DataLoader.cs`): case-insensitive,
  comments + trailing commas allowed, camelCase enums, hand-rolled `Utf8JsonReader`
  converters for polymorphic fields (the exact technique the tolerant-int converter
  needs), export LF-normalized and **BOM-free** (BOMs broke their STJ loads; a strip
  script exists as the scar). Anti-pattern to avoid: their two-pass
  strip-and-reparse action loader — design storylet JSON so one converter per field
  suffices.
- `MUA.M7` — **Godot harness patterns** (`GodotProject/Scripts/Core/ScreenGrab.cs`,
  `InputPlayback.cs`): viewport-capture to a debug folder; JSON input-replay with
  hotkeys (speed/pause/single-tick/export). Rekey both to **sim slots, not wall-clock**
  (theirs are real-time-timed, so captures aren't reproducible) and route output to
  `.captures/`.
- `MUA.M8` — **Validate-then-run discipline**: a `SchemaValidator` pass (range checks,
  ID-reference integrity, DAG acyclicity, fail-loud warnings) before the sim starts;
  plus the lesson that dead schema surface is poison ("returning 0f silently is worse
  than failing loudly"). L3 fails loud on unimplemented predicate/effect types.

## `MUA.N` — make anew / ignore

- `MUA.N1` — **All RNG.** The `HashCode.Combine` illusion (see headline 2). Rebuild on
  seeded named streams exactly as the parent plan specifies; also keep `DateTime.Now` /
  `Guid.NewGuid()` out of `Fishbowl.Core` entirely (Autonome let them leak into CLI
  framing and its possession API).
- `MUA.N2` — **The test harness.** Nothing to mine; three empty projects. Golden-day +
  day-hash tests go in first (`VFB.M0`) — precisely what would have caught `MUA.N1`.
  Their minimal GitHub CI skeleton (restore → build → test) is fine to copy; fill the
  test step they left empty. No multi-seed soak runner exists either — the seeds×days
  loop is new.
- `MUA.N3` — **The Godot bridge.** Theirs is a scene-child node (not an autoload) with
  typed-object access from consumers and primitive-carrying signals, plus a documented
  signal-vs-`_Ready` race patched with `CallDeferred`. The fish-bowl's single-autoload,
  JSON-string, pull-based-init `FishbowlBridge` is the correction, not a port. Ticking
  stays single-threaded and synchronous — Autonome bolted two concurrency models (lock +
  background task; fire-and-forget broadcasts) onto one engine.
- `MUA.N4` — **Utility-AI as the action picker.** Autonome's scorer *chooses what NPCs
  do*; the fish-bowl declined that (`FBS.3`) — pressures are fuel, day-plans are
  clockwork. Mine the curve math for weighting only; drop the score→act control loop,
  travel/pathfinding, `TravelState`, and Dijkstra routing. Keep only the who-is-where
  reverse index (`_locationEntities`) for L1 co-presence.
- `MUA.N5` — **Wrong scale / wrong domain:** the authority DAG + directives + org
  hierarchy, aggregation-as-runtime-feedback on org properties, the food/gold economy,
  the ASP.NET/WebSocket live host, the MCP possession server, LOD zoom rendering and all
  tile-art tooling, the 315-entry accreted permission allowlist. Twelve flat townees
  observed at dawn need none of it.
- `MUA.N6` — **Magic-number sprawl.** Night multipliers, string-literal action bonuses,
  hardcoded rent-owner maps scattered through scorer logic. `simconfig.json`-or-nothing
  is the fix; Autonome shows how fast inline constants proliferate without that rule.

## `MUA.Q` — questions the comparison raises (feed the `VFB.D*` rulings)

- `MUA.Q1` — Which named stream drives storylet selection vs. the summarizer's pick-5,
  and are softmax K/temperature fixed per seed? (Variety without losing `VFB.Q4`
  reproducibility.)
- `MUA.Q2` — Is regard one scalar + tags, or sub-properties? Autonome's experience: one
  scalar was insufficient once gossip mattered — `trust` had to be retrofitted to gate
  fidelity.
- `MUA.Q3` — Which storylet effects are persistent state vs. recomputed each slot?
  Autonome's split (passives recomputed idempotently under deterministic IDs; memories
  accumulate and decay) prevented drift/leak bugs — copy or reject deliberately.
- `MUA.Q4` — Do pressures ever hard-gate a storylet, or appear only as predicate
  operands and weights? Autonome blurred gate-vs-weight and needed the zero-lock escape
  hatch when gating filtered to zero candidates.
- `MUA.Q5` — What exactly enters the day-hash canonicalization (float precision, dict
  ordering, modifier lists)? No precedent anywhere in Autonome — genuinely unspecified.
- `MUA.Q6` — Tellability scoring is genuinely new: Autonome scores actions to *take*,
  never events to *narrate*. Closest borrowable piece: run salience scalars through a
  named ResponseCurve (`MUA.J2`).
- `MUA.Q7` — The fish-bowl has no economy to destabilize — what is its fragile-
  equilibrium analog (`MUA.J10`) so pressures cross storylet thresholds often enough to
  feed a chronicle, but not constantly?

## Sync footer (Rule 3)

This appendix is referenced from `PLAN-village-fishbowl.md` (header block). Rulings that
resolve a `MUA.Q*` land in the parent plan's asks; this file then gets a strike-through
note, not a renumber.

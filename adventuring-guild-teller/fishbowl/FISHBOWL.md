# FISHBOWL.md — agent-nav spec for the village fish-bowl prototype

**Mnemonic:** `FB` (this subproject's page items; the parent plan is `VFB`).
**What this is:** the first release of the **village fish-bowl** — AGT's Roster-pillar town
sim as a playable observatory. Godot 4.6 mono front-end over an engine-free C# core, JSON
data. Data readouts + debug knobs only; no desk, no floor (per `PLAN-village-fishbowl.md`).

> **Isolation (standing):** this subproject shares **no code** with
> `adventuring-guild-teller/morning-queue/**` and was built without reading it. Convergence
> is a post-v1 decision for Panda. Do not wire the two together here.

---

## Layout

```
fishbowl/
  FISHBOWL.md            this file — the LLM entry point
  README.md              human code-doc
  project.godot          Godot 4.6 mono; autoloads FishbowlBridge + TestHarness (GTH, inert); gl_compatibility; main = Observatory.tscn
  Fishbowl.sln           game project + the three core projects
  Fishbowl.csproj        the Godot game (references core/Fishbowl.Core); only cs/ is engine-facing
  icon.svg
  cs/FishbowlBridge.cs   THE ONLY C# the engine touches directly — JSON strings across the boundary
  scenes/Observatory.tscn  root Control + Observatory.gd (UI built in code, not hand-authored nodes)
  scripts/Observatory.gd   the observatory: readouts, knobs, creation menus (node scripting only)
  scripts/Sparkline.gd     class_name Sparkline — inspector pressure sparkline
  core/Fishbowl.Core/      engine-free classlib: Determinism/ Json/ Model/ Data/ Engine/ Text/ Api/
  core/Fishbowl.Cli/       headless runner (--town --seed --days --report --chronicle --soak)
  core/Fishbowl.Core.Tests/  xUnit — 22 tests incl. the Godot-stringify round-trip suite
  data/                    the authored town (see "Data contract")
  addons/gd_test_harness/  GTH test harness — project-agnostic input/inspect/capture addon (own README)
  harness.config.json      GTH config — artifacts dir, capture caps, bridge port
  tests/harness/           GTH prescripted scenarios (smoke.json)
  .captures/               capture output (gitignored; F9 DevHarness + GTH's .captures/gth/)
```

**Entry-point chain:** this file → `core/` (the sim) and `scripts/` (the view) → `data/`.
The core is where the logic and determinism live; GDScript is presentation only. To **drive or verify the
running observatory**, use the GTH harness (§ *Test harness* below) — a `tests/harness/` scenario or the
`mcp__gth-fishbowl__*` tools — rather than rolling a new one or clicking by hand.

---

## Determinism contract (non-negotiable — the research method depends on it)

- **Named RNG streams** (`Determinism/Rng.cs`): SplitMix64 seeded from `(world_seed, day,
  stream_name)` via FNV-1a. Streams `plans` / `storylets` / `drift` / `gen` are independent,
  so adding a draw in one system never shifts another. Per-day streams are cached and reset at
  each day setup (`World.ResetDayStreams`). **Never** `HashCode.Combine`, `DateTime.Now`, or
  `Guid.NewGuid()` in `Fishbowl.Core` — that illusion is exactly what broke the mined Autonome
  project (plan appendix `MUA.N1`).
- **Day-hash** (`Determinism/FnvHash.cs` + `Json/CanonicalJson.cs`): FNV-1a 64 over the
  canonical dawn-state JSON. Canonical = keys sorted ordinal, integral numbers emitted as ints
  (so Godot's `4.0` hashes identically to `4`), non-integral rounded to 6 decimals. This is the
  resolution of appendix `MUA.Q5` (what enters the hash: day, per-townee pressures/regard/marks,
  cooldowns, and the day's chronicle digest).
- **Tolerant ints** (`Json/TolerantIntConverter.cs`, `int` + `long`): registered from day one so
  Godot-`JSON.stringify`-floatified payloads (`4` → `4.0`) still bind. The test suite replays
  that round-trip over every real `data/` file.
- Single-threaded tick; townees iterate in stable id order, places in stable id order. The engine
  steps only via `Simulation.StepSlot()` / `RunToDawn()`.

## The machinery (CPS — Clockwork · Pressures · Storylets)

| Layer | File | What it does |
|---|---|---|
| **L1 Clockwork** | `Engine/Clockwork.cs` | resolves each townee's day-plan into a slot-by-slot itinerary + the per-slot co-presence index (who is where, together). Sets `mode` (work/home/haunt/away) and `asleep` per slot. |
| **L2 Pressures** | `Engine/Pressures.cs` | four drives (`purse`,`trade`,`heart`,`restlessness`) drift by rule each slot, minutes-scaled and trait/knob-scaled. Fuel, never an action-picker. Regard changes only via L3 effects in v0. |
| **L3 Storylets** | `Engine/StoryletEngine.cs` | JSON rules with predicates over co-presence + pressures + regard + calendar. Snapshot semantics within a slot (evaluate all, then apply all). Appends a chronicle entry carrying the because-list. |
| **Summarizer** | `Engine/Summarizer.cs` | picks `summary_lines` (5±2) chronicle entries by tellability, gated by hearsay-lite (a gossip-carrier witnessed it or later shared a room with a witness), rendered at the actionability register. |

**Binding:** a storylet with an authored `_binding` is anchored to that cast (still gated by every
predicate each slot); one without binds by predicate search over co-present townees. v0's bank is
all anchored — that is what makes the golden day reproduce exactly while leaving the search path
open for emergent rules. **Awake gate:** non-`must_fire` storylets don't fire on a sleeping
participant (keeps beats in waking hours); a departure is `must_fire` and exempt.

## The bridge surface (frozen — `cs/FishbowlBridge.cs`)

GDScript calls these; **JSON strings cross the boundary** (never typed objects). Getters are
pull-based; state also arrives via four signals. C# `[Signal]` names stay **PascalCase** in
GDScript (`bridge.SlotTicked.connect(...)`).

- Lifecycle: `LoadTown(path)`, `GenerateTown(configJson)`, `Reseed(seed)`
- Tick: `StepSlot()`, `RunToDawn()`, `RunDays(n)`, `CurrentDay()`
- Readouts (JSON out): `GetClock()`, `GetRoster()`, `GetTownee(id)`, `GetPlaces()`,
  `GetChronicle(day)`, `GetSummary(day)`, `GetStats(day)`, `GetKnobs()`,
  `GetPressureSeries(id,drive)`, `GetStorylets()`
- Knobs: `SetKnob(name,value)`, `SetAway(id,away)`
- Creation: `CreateTownee(json)`, `CreatePlace(json)`, `InjectStorylet(id,participantsJson)`
- Snapshots: `SaveSnapshot(path)`, `LoadSnapshot(path)`
- Signals: `SlotTicked(day,slot)`, `EventLogged(eventJson)`, `DawnReady(day,summaryJson)`,
  `HashReady(day,hash)`

The JSON projections live in the engine-free core (`Api/WorldView.cs`) so they are unit-testable;
the bridge is a thin marshalling shim.

## Data contract (`data/`, all `"version": 1`, ints tolerant-parsed)

`simconfig.json` (every knob's default) · `places.json` (6 board cards + residences;
`board:true/false`, `shut`) · `townees.json` (12 golden cast; `departs_day` schedules an
adventurer's expedition) · `dayplans.json` (one template per role; `haunt:<id>` tokens, courier
`roams`) · `traits.json` (`pressure_rate_mods`, `storylet_weight_mods`, `hearsay_carrier`) ·
`storylets/*.json` (12 rules; `_binding` anchors, `must_fire` override) · `golden/day1.json`
(the pinned beat types + participants the M3 test reproduces).

## Milestone status (this release)

- **M0 scaffold** ✅ — projects build; `dotnet test` green incl. the Godot-stringify round-trip;
  empty-town hash stable; capture harness present (F9, ships as a manual key).
- **M1 clockwork** ✅ — golden cast loads; itineraries resolve; roster + place board + clock live;
  12×3-day deterministic hash sequence; every townee findable at every slot.
- **M2 pressures** ✅ — drives drift by data rules; inspector sparklines; live knobs; snapshots
  round-trip to the same forward hash sequence.
- **M3 storylets + summary** ✅ — 12-rule bank; chronicle with because-lists; dawn summary +
  actionability dial + hearsay-lite; bio-marks behind the FB.8 toggle; **golden day reproduces
  its 7 scripted beats**.
- **M4 creation + research instruments** ◑ — seeded town generator (invariant-guaranteeing) ✅;
  four creation menus in the observatory ✅; stats strip ✅; CLI soak ✅. **`VFB.Q1` finding:**
  the soak sustains **avg ~4.4 distinct tellable lines/night** across a week (3/21 nights dip
  below 4). That is the open tuning question the prototype exists to answer — drive it with the
  live knobs, not a code change.

## Build · test · run

```bash
# core (engine-free — no Godot needed)
dotnet test  core/Fishbowl.Core.Tests/Fishbowl.Core.Tests.csproj      # 22 tests
dotnet run   --project core/Fishbowl.Cli -- --days 3 --chronicle      # readable trace
dotnet run   --project core/Fishbowl.Cli -- --soak --days 7           # VFB.Q1 instrument

# game (bridge compile-check without opening the editor — pulls Godot.NET.Sdk from NuGet)
dotnet build Fishbowl.csproj

# Godot (class_name registration needs the import pass on a fresh checkout)
"C:/Program Files/godot/godot.exe" --headless --path . --import
# then run via the godot MCP (run_project / get_debug_output / stop_project); F9 → .captures/

# verify the UI end-to-end with the GTH harness (synthetic input + capture) — see "Test harness" below.
# prescripted (rendered window; --headless has no framebuffer to capture):
"C:/Program Files/godot/godot.exe" --path . -- --gth-scenario=res://tests/harness/smoke.json --gth-exit-after
# or live from chat: the mcp__gth-fishbowl__* tools (session_start · snapshot · click_element · capture)
```

## Test harness (GTH — `addons/gd_test_harness/`)

A drop-in, **project-agnostic** input / inspect / capture harness (spec: root
`plans/PLAN-godot-test-harness.md`, mnemonic `GTH`). It is **additive** to the F9 DevHarness (which
stays) and **inert** unless activated — normal play and golden-day determinism are untouched. Two thin
drivers over one GDScript core (InputInjector / SceneProbe / Capturer / Bridge):

- **Prescripted** (CI-friendly): set `GTH_SCENARIO=res://tests/harness/smoke.json` + `GTH_EXIT_AFTER=1`,
  run the observatory windowed → drives clicks/keys through the real input pipeline, writes captures to
  `.captures/gth/` + a `manifest.jsonl`, prints a JSON result. Bundled `smoke.json` gate-checks the whole
  surface (snapshot, clickability, element + location clicks with consumption reports, key injection,
  settle / sha-dedup / annotate capture). Verified 2026-07-15 — a synthetic `btn-step` click advances the
  clock to slot 1; a location click selects a roster row.
- **Live (MCP)**: `--gth-serve` opens a loopback WebSocket for the external MCP server at
  `utils/dotnet/gth-mcp-server/` (.NET 8, live-verified 2026-07-15), **registered project-scoped in the repo
  `.mcp.json` as `gth-fishbowl`** (launch mode). After a fresh clone: `dotnet build
  utils/dotnet/gth-mcp-server`, restart the client, approve the server — then drive the observatory from chat.
  Tools appear as **`mcp__gth-fishbowl__*`**: `session_start` · `snapshot` · `query_element` · `read_element` ·
  `hit_test` · `click_at` · `click_element` · `press_key` · `capture` · `wait_for` · `run_scenario`. Launch
  mode starts (and stops) the observatory for you — no separate `run_project` needed.

**Stable handles** — the observatory tags every readout / button / knob with `test_id` meta, because the UI
is code-built and auto-generated node names (`@HSlider@68`) shift on relayout. Resolve by these, not paths:

| Group | `test_id`s |
|---|---|
| Readouts | `clock` · `hash` · `seed` · `register` · `stats` · `summary` |
| Tables | `roster` · `chronicle` |
| Buttons | `btn-step` · `btn-dawn` · `btn-run3` · `btn-reseed` · `btn-generate` · `btn-townee` · `btn-place` · `btn-storylets` · `seed-spin` |
| Knobs | `knob-actionability` · `knob-storylet_rate` · `knob-pressure_rates.trade` · `knob-summary_lines` · `knob-hearsay_required` · `knob-bio_marks_enabled` |

The **knobs are the `VFB.Q1` tuning surface** (drive them, don't change code) — `drag` a slider by handle, or
`click_element` a checkbox. See the addon's `README.md` for the full command API and activation precedence.
**Captures need a rendered window** — pixels are blank under `--headless` (`GTH.D7`).

## Rulings adopted (parent-plan "The asks")

`VFB.D1` 48×30-min slots · `VFB.D2` engine-free core + CLI + xUnit · `VFB.D3` JSON-only storylets
· `VFB.D4` 12 townees / 6 board-places / 2 adventurers · `FB.8` bio-marks ratified **on**,
toggleable. Build-time decisions layered on top and recorded in `DEV-LOG.md`: `_binding`-anchored
storylets, the awake gate, `departs_day` scheduling, regard-via-effects-only, and
canonical-hash quantization at 6 decimals.

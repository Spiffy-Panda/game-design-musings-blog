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
  project.godot          Godot 4.6 mono; autoload FishbowlBridge; gl_compatibility; main = Observatory.tscn
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
  .captures/               capture-harness output (gitignored; F9 in the observatory)
```

**Entry-point chain:** this file → `core/` (the sim) and `scripts/` (the view) → `data/`.
The core is where the logic and determinism live; GDScript is presentation only.

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
```

## Rulings adopted (parent-plan "The asks")

`VFB.D1` 48×30-min slots · `VFB.D2` engine-free core + CLI + xUnit · `VFB.D3` JSON-only storylets
· `VFB.D4` 12 townees / 6 board-places / 2 adventurers · `FB.8` bio-marks ratified **on**,
toggleable. Build-time decisions layered on top and recorded in `DEV-LOG.md`: `_binding`-anchored
storylets, the awake gate, `departs_day` scheduling, regard-via-effects-only, and
canonical-hash quantization at 6 decimals.

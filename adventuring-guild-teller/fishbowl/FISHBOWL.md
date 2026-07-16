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
  scripts/Observatory.gd   the observatory: readouts, knobs, creation menus (node scripting only).
                           LAYOUT RULE, load-bearing: two FIXED rails (roster+board / knobs+inspector)
                           and ONE fluid reading pane (summary+chronicle) whose width is derived, never
                           declared. Machine readouts (clock/hash/seed/stats) go through `_readout()` —
                           monospace + `clip_text`, so their text can never be a layout demand. A bare
                           Label's text IS its minimum width, and a minimum beats a FULL_RECT anchor:
                           that is how a 108px `hash` once forced the root to 1371px in a 1290 viewport
                           and starved the Dawn Summary. Don't put a variable-width bare Label in a
                           width-negotiating container.
  scripts/Sparkline.gd     class_name Sparkline — inspector pressure sparkline
  core/Fishbowl.Core/      engine-free classlib: Determinism/ Json/ Model/ Data/ Engine/ Text/ Api/
  core/Fishbowl.Cli/       headless runner (--town --seed --days --report --chronicle --soak)
  core/Fishbowl.Core.Tests/  xUnit — 30 tests incl. the Godot-stringify round-trip suite
  data/                    THE LIVE TOWN — all features on; postings/sites authored here (see "Data contract")
  addons/gd_test_harness/  GTH test harness — a GENERATED COPY. Canonical: ../../utils/godot/
                           gd_test_harness/ (edit there, then `python utils/python/sync_gth_addon.py`;
                           `--check` exits 1 on drift). Shared with morning-queue since GTH.Q4.
  harness.config.json      GTH config — artifacts dir, capture caps, bridge port
  tests/harness/           GTH prescripted scenarios (smoke.json, regression-b1-b6.json)
  tests/towns/golden-town/ THE FROZEN GOLDEN FIXTURE — posting-free; the town every xUnit acceptance
                           test loads. Do not add features to it (PNO.D2)
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
| **Summarizer** | `Engine/Summarizer.cs` | picks `summary_lines` (5±2) chronicle entries by tellability, gated by hearsay-lite (a gossip-carrier witnessed it or later shared a room with a witness), rendered at the actionability register. **Split along the axis that exists:** `SealDay` runs at dawn (gossip carriage is the one occupancy-dependent phase); everything downstream **derives on read**, with no cached summary. That is what makes the rendering knobs live — see "the knobs" under *Test harness*. |

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

## Data contract (all `"version": 1`, ints tolerant-parsed)

**Two towns, and the split is load-bearing** (`PNO.D2`, ruled 2026-07-16 — see
`plans/PLAN-fishbowl-postings-outings.md`):

| | `data/` — **the live town** | `tests/towns/golden-town/` — **the frozen fixture** |
|---|---|---|
| Who loads it | the observatory (`FishbowlBridge._Ready()` → `res://data`), the CLI (default; `--town` overrides) | every xUnit test, via `TestSupport.LoadGoldenTown()` |
| Features | **all of them** — postings/sites are authored here | **posting-free, forever** |
| Why | the board filling and emptying is the readout; it has to be the town you actually run | it pins seed-independence, the 12/6/2 counts, and the golden day's 7 beats |

**Do not add postings, sites, or cast to the fixture.** Its entire value is that it stopped changing.
A golden master living inside the live data directory tracks the very thing it is meant to pin — which
is what it did until 2026-07-16, and why this split exists. The fixture is a **full copy** (`TownLoader`
requires all 5 files; there is no overlay/merge), so it will drift from `data/`. **Drift is the feature.**

Files, both towns: `simconfig.json` (every knob's default) · `places.json` (6 board cards + residences;
`board:true/false`, `shut`) · `townees.json` (12 golden cast; `departs_day` schedules an adventurer's
expedition) · `dayplans.json` (one template per role; `haunt:<id>` tokens, courier `roams`) ·
`traits.json` (`pressure_rate_mods`, `storylet_weight_mods`, `hearsay_carrier`) · `storylets/*.json`
(12 rules; `_binding` anchors, `must_fire` override). **Fixture only:** `golden/day1.json` (the pinned
beat types + participants the M3 test reproduces; `TownLoader` treats it as optional, so the live town
reports `Town.Golden == null` and that is correct).

**`TestSupport.DataDir` deliberately stays on `data/`** — it drives `WriteFloatifiedData`'s recursive
sweep, so `M0.GodotStringify_RoundTrip_Of_Every_File` covers every newly authored file automatically.
Anything placed under `data/` joins that suite; that is why the fixture lives under `tests/`.

## Milestone status (this release)

- **M0 scaffold** ✅ — projects build; `dotnet test` green incl. the Godot-stringify round-trip;
  empty-town hash stable; capture harness present (F9, ships as a manual key).
- **M1 clockwork** ✅ — golden cast loads; itineraries resolve; roster + place board + clock live;
  12×3-day deterministic hash sequence; every townee findable at every slot.
- **M2 pressures** ✅ — drives drift by data rules; inspector sparklines; live knobs; snapshots
  round-trip to the same forward hash sequence **and to the same summaries** — since the Summarizer
  derives on read from the chronicle, and the chronicle is in the snapshot, `Snapshot` never needed
  to know summaries exist (`M3_SummaryRenderTests.Snapshot_Round_Trip_Preserves_The_Summary`).
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
dotnet test  core/Fishbowl.Core.Tests/Fishbowl.Core.Tests.csproj      # 30 tests
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
  clock to slot 1; a location click selects a roster row. **`regression-b1-b6.json`** (2026-07-16) is the
  standing cover for the `GTH.B1`–`B6` fixes; it **used to** lean on the observatory's own `btn-storylets`
  layout bug as its fixture, and was re-fixtured onto the invariant when that bug was fixed (see below).
  Runs green, 34 steps.
- **Live (MCP)**: `--gth-serve` opens a loopback WebSocket for the external MCP server at
  `utils/dotnet/gth-mcp-server/` (.NET 8, live-verified 2026-07-15), **registered project-scoped in the repo
  `.mcp.json` as `gth-fishbowl`** (launch mode). After a fresh clone: `dotnet build
  utils/dotnet/gth-mcp-server`, restart the client, approve the server — then drive the observatory from chat.
  Tools appear as **`mcp__gth-fishbowl__*`**: `session_start` · `snapshot` · `query_element` · `read_element` ·
  `hit_test` · `click_at` · `click_element` · `press_key` · `capture` · `wait_for` · `window_state` ·
  `run_scenario` ⚠️. Launch mode starts (and stops) the observatory for you — no separate `run_project` needed.

**⚠️ Open harness bugs — read this before you plan a session (`GTH.B9`–`B12`, all open as of 2026-07-16;
check the plan before working around them, they may be fixed by the time you read this):**

- **`run_scenario` over MCP is a silent no-op.** It returns `{}` and executes nothing. The **file-based**
  runner accepts identical steps and works — use `--gth-scenario=res://...`, or drive step-by-step with the
  individual tools. Four separate agents burned time on this in one day before it was written down here.
- **Before building a capture corpus, override two config defaults.** `max_dim` defaults to **1280**, which
  is *narrower than this app's real 1290px viewport*, so frames silently downscale to 1280×803 and stop
  being 1:1 with the rects you measure them against (`GTH.B10`). And `if_changed: true` dedups against the
  **global last** capture regardless of label, so a deliberate A/B whose two arms look identical writes
  **no file at all** (`GTH.B11`) — confirmed live, on exactly such an A/B. Set `max_dim: 1290`,
  `if_changed: false`.
- **`session_id` can't be set without editing tracked config** (`GTH.B12`), so a read-only consumer cannot
  choose where its captures land.

**The real viewport is 1290×810**, though `project.godot` declares 1280×800. Trust `query_element` rects,
not the config. And **the window is resize-locked while the harness is active** (`GTH.B5`), so 1290×810 is
the only size you can honestly test — do not claim responsive behaviour you cannot observe.

**Two things to know before you read a harness result** (both fixed 2026-07-16, both were live through the
first release — see `GTH.B1`–`B6`):

- `on_screen` is **strict** (fully inside the viewport) and is *not* the same as `clickable` (a click aimed
  here would land). The worked example **used to be the observatory's own `btn-storylets`**: after a day ran,
  the `hash` readout widened from `—` to 16 hex digits and shoved the button to ~x=1281 in a 1290 viewport,
  leaving ~10% of it showing, and it reported `on_screen: false, visible_fraction: 0.100, clipped: [right],
  clickable: true` — all four at once, because all four were true. The harness used to just say
  `on_screen: true` and click 34px past the window edge.
  **That example is gone: the layout bug behind it was fixed (2026-07-16, the usability view pass).** The
  `hash` now lives in a fixed-width monospace status strip and `btn-storylets` sits at x=1134,
  `visible_fraction: 1.0`, at every day and every seed. The distinction the example taught is still real and
  still load-bearing — it is simply no longer demonstrable in this app, which is the point. **Note the
  hazard it leaves behind:** `regression-b1-b6.json` had encoded that clipping as
  `expect on_screen: false` and ran green on it for a release, so the assertion that should have caught the
  bug was pinning it instead. It now asserts the invariant (`on_screen: true` after the hash populates), and
  the `on_screen: false` path it used to cover is **uncovered** — it needs a deliberately off-screen control
  in the addon's own test scene, not a defect in the host app.
- **The window is resize-locked while the harness is active**, and a **minimized window is restored before
  any capture**. Minimized, the framebuffer freezes while the sim keeps running, so a capture comes back
  byte-identical to the last one and dedup calls it `changed: false`. Measured on this project, not assumed.

**Stable handles** — the observatory tags every readout / button / knob with `test_id` meta, because the UI
is code-built and auto-generated node names (`@HSlider@68`) shift on relayout. Resolve by these, not paths:

| Group | `test_id`s |
|---|---|
| Readouts | `clock` · `hash` · `seed` · `register` · `stats` · `summary` |
| Tables | `roster` · `chronicle` |
| Buttons | `btn-step` · `btn-dawn` · `btn-run3` · `btn-reseed` · `btn-generate` · `btn-townee` · `btn-place` · `btn-storylets` · `seed-spin` |
| Knobs — *rendering, applies now* | `knob-actionability` · `knob-summary_lines` · `knob-hearsay_required` |
| Knobs — *simulation, applies next dawn* | `knob-storylet_rate` · `knob-pressure_rates.trade` · `knob-bio_marks_enabled` |

**The `test_id`s are the knob KEYS and never change; the on-screen labels are not the keys.** Two read
differently to a human: `knob-actionability` is labelled *"register (actionability)"* (it is the register
dial — it is why the line above the summary says `gossip`) and `knob-pressure_rates.trade` is labelled
*"trade drift rate"* (the dotted path implied `.purse`/`.heart` siblings that no slider offers). Resolve by
`test_id`; read the value back with `read_element {contains: "<label>"}` using the **label**, since the value
labels carry no `test_id` of their own.

The **knobs are the `VFB.Q1` tuning surface** (drive them, don't change code) — `drag` a slider by handle, or
`click_element` a checkbox. **They are half live and half deferred, and the UI groups them by which:**
`actionability` / `summary_lines` / `hearsay_required` are **rendering** knobs and re-present the day you are
already looking at, so they apply on release; `storylet_rate` / `pressure_rates.trade` / `bio_marks_enabled`
are **simulation** knobs and cannot apply retroactively without re-running the day (which would move the
day-hash), so they take effect at the **next dawn**. `bio_marks_enabled` looks like a display toggle beside
`hearsay_required` and is not — it writes hashed bio Marks at storylet-fire time.
See the addon's `README.md` for the full command API and activation precedence.
**Captures need a rendered window** — pixels are blank under `--headless` (`GTH.D7`).

## Rulings adopted (parent-plan "The asks")

`VFB.D1` 48×30-min slots · `VFB.D2` engine-free core + CLI + xUnit · `VFB.D3` JSON-only storylets
· `VFB.D4` 12 townees / 6 board-places / 2 adventurers · `FB.8` bio-marks ratified **on**,
toggleable. Build-time decisions layered on top and recorded in `DEV-LOG.md`: `_binding`-anchored
storylets, the awake gate, `departs_day` scheduling, regard-via-effects-only, and
canonical-hash quantization at 6 decimals.

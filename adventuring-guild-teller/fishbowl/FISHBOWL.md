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
  core/Fishbowl.Cli/       headless runner (--town --seed --days --report --chronicle --soak --lint
                           --json --seeds --knob). Linter.cs runs the REAL Clockwork/Simulation — a
                           linter that reimplements what it audits eventually audits its own fiction
  core/Fishbowl.Core.Tests/  xUnit — 53 tests incl. the Godot-stringify round-trip suite
  data/                    THE LIVE TOWN — all features on; postings authored here (see "Data contract").
                           Also `lint-accepted.json` — the per-town acceptance ledger --lint reads
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

**The absolute pin, and the one time it moved.** `M1_ClockworkDeterminismTests.Twelve_Townees_Three_Days_Hash_Sequence_Is_Pinned`
hard-asserts the fixture's day-1..3 hashes as literals. It exists because the test beside it
(`..._Identical_Hash_Sequence`) only ever compared run A to run B **inside one build** — self-consistency,
not stability — so a change that moved every hash *consistently* sailed through it green. The contract
claims stability; nothing was testing it. The pin is only possible **because** of `PNO.D2`: a hash literal
asserted against a town that can be edited underneath it pins nothing.

| | day 1 | day 2 | day 3 |
|---|---|---|---|
| **was** (through 2026-07-16) | `b8d15299d8817639` | `e3478bc4ff7d4848` | `02bc86b987c547c3` |
| **now** | `2a6a8a3af0a1a81d` | `d615d01daa2c8020` | `619649026a9d8895` |

**They have moved exactly once**, 2026-07-16, on a Panda ruling (`NTD.Q1` + `FBT.Q1`; the `DEV-LOG.md`
entry of that date *is* the ruling). Cause: `Pressures.BaseDaily`'s `trade` arm stopped being a flat
`−0.11/day` countdown and became a restoring force. Two other fixes landed in the same change — signed
`pressure_rate_mods`, `heart` `pressure_targets` — and moved **nothing** here, verified by staging them
alone and watching the old literals stay green, which is what "hash-neutral" has to mean if it means
anything. **If this test goes red, that is the test working. Do not re-baseline it** — either the change
was not supposed to touch the hash, or it was, and that needs a ruling in `DEV-LOG.md` first.

## The machinery (CPS — Clockwork · Pressures · Storylets)

| Layer | File | What it does |
|---|---|---|
| **L1 Clockwork** | `Engine/Clockwork.cs` | resolves each townee's day-plan into a slot-by-slot itinerary + the per-slot co-presence index (who is where, together). Sets `mode` (work/home/haunt/away) and `asleep` per slot. |
| **L2 Pressures** | `Engine/Pressures.cs` | four drives (`purse`,`trade`,`heart`,`restlessness`) drift by rule each slot, minutes-scaled and trait/knob-scaled. Fuel, never an action-picker. Regard changes only via L3 effects in v0. **Two shapes, and the difference is load-bearing** — see below. |
| **L3 Storylets** | `Engine/StoryletEngine.cs` | JSON rules with predicates over co-presence + pressures + regard + calendar + **`place`** (`{any:[ids]}` / `{kind:[kinds]}`). Snapshot semantics within a slot (evaluate all, then apply all). Appends a chronicle entry carrying the because-list. Effects: `regard` / `pressure` / **`post`** / `chronicle`, **exactly one meaningful per entry, enforced at load**. |
| **Board** | `Engine/Board.cs` | the standing index — file (`post`) / expire. Expiry is a **mechanism, not a rule**, so it synthesizes its own because-list. |
| **Summarizer** | `Engine/Summarizer.cs` | picks `summary_lines` (5±2) chronicle entries by tellability **fatigued by how recently each rule was told** (`novelty_decay`), gated by hearsay-lite (a gossip-carrier witnessed it or later shared a room with a witness), rendered at the actionability register. **Split along the axis that exists:** `SealDay` runs at dawn (gossip carriage is the one occupancy-dependent phase); everything downstream **derives on read**, with no cached summary. That is what makes the rendering knobs live — see "the knobs" under *Test harness*. The fatigue ledger obeys the same rule and is the sharpest test of it: night N's ranking depends on night N−1's delivery, and *that is re-derived by a forward fold over the chronicle on every read* rather than cached, which is what keeps the knob retroactive. Cost: a render is O(nights), so a measured run is O(nights²). |

**Binding:** a storylet with an authored `_binding` is anchored to that cast (still gated by every
predicate each slot); one without binds by predicate search over co-present townees. v0's bank is
all anchored — that is what makes the golden day reproduce exactly while leaving the search path
open for emergent rules. **The search path handles 1–2 roles only** — dropping `_binding` from a
3-role rule does not "opt into the search path", it makes the rule unfireable. **Awake gate:**
non-`must_fire` storylets don't fire on a sleeping participant (keeps beats in waking hours); a
departure is `must_fire` and exempt.

**The two Pressures shapes (`NTD.Q1`, ruled 2026-07-16).** `purse` and `restlessness` are
**mode-constant**: they ignore `current`, so the dayplan's slot counts set a sign and the drive travels
until it clamps. `heart` and `trade` are **restoring**: the drift is proportional to the gap between
where the townee is and where the mode says they belong, so the sign flips around a rest point and the
drive *converges* instead of *arriving*.

- **`trade` was a mode-constant `−0.11/day` with no positive path anywhere in the engine** — a
  guaranteed countdown to 0 for all 18. **Retuning the constant could not have rescued it, and that is
  the part to keep: a mode-constant drift has no interior fixed point at all.** Whatever the numbers,
  the daily sum is some constant `D`; the drive pegs at 1.0 if `D>0` and 0.0 if `D<0`. The obvious fix
  (a mode-constant *work* gain) would only have **relocated** the ratchet while **silencing `--lint`**,
  which tested the per-mode sign rather than the net drift — *a green gate over a live countdown, which
  is strictly worse than the bug.* Now: `(TradeRestFor(mode) − current) × 0.20`, rest **0.55** at work /
  **0.12** idle, so the rest point is the dayplan's time-weighted blend. **The bank's budget:** a
  sustained `B`/day of trade effects moves the rest point to `rest + B/0.20`, and pegs only past
  `≈+0.115/day`. Budget against that, not against the old `−0.11`.
- **`heart` is restoring, so `cheerful`/`gruff` could never have been rate mods** — scaling a restoring
  drive's *rate* changes only how fast it converges, never on what, so `cheerful ×1.1` and `gruff ×0.85`
  were **indistinguishable in the limit** (both land on 0.5). "Cheerful" is a claim about *where you
  rest*: `pressure_targets: {"heart": 0.6}`. Two targets combine by **mean** — a determinism decision,
  not an aesthetic one ("last wins" would make the day-hash depend on JSON array order). `SchemaValidator`
  refuses a target on any drive but `heart`.
- **`restlessness` ships with the shape `trade` was just cured of, knowingly** (Panda ruled: leave it,
  ship the finding). Mode-constant `−0.10` engaged / `+0.06` at rest; break-even at **18** engaged slots
  of 48 bare, but `TraitRateMod` picks gain/decay off each slot's *sign*, so an asymmetric trait
  (`wanderlust {gain 1.3, decay 0.7}`) re-weights the halves and moves it to **~25**. The live cast sits
  at 8 or 22–36, so **16 of 18 ride a clamp (13 floored, 3 pegged)**. It is `trade`'s bug one drive
  later. Restlessness is directional *by design* — the buildup exists to push a townee somewhere, and
  today there is nowhere to be pushed. **`PNO.M2` (outings) is where it may finally get somewhere to
  discharge — but see the open question below; do not treat that as settled.**

> **⚠ OPEN QUESTION — flagged, not resolved.** The ruling's reasoning is that `PNO.M2` gives
> restlessness its discharge. But `plans/PLAN-fishbowl-postings-outings.md:222` argues the opposite for
> the mechanism nearest to hand: `haunt:<site-id>` would make `ModeOf` tag a site visit `haunt`, and
> mode feeds `BaseDaily` — so **restlessness would burn off at the site, "which is precisely backwards
> for an outing."** The two reconcile only if M2's discharge is an authored `take`/`resolve` **effect**
> rather than a mode-label side-effect. Nobody has ruled which. `data/lint-accepted.json`'s 14 accepted
> ratchets are the specification M2 has to discharge; if they are still there after M2, the ruling was
> wrong and that ledger entry is the evidence.

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
| Counts *(measured 2026-07-16)* | **18** townees · **16** places (**8** `board:true`) · **4** adventurers · **50** storylets · **49** regard edges | **12** townees · **12** places (**6** `board:true`) · **2** adventurers · **12** storylets · **10** regard edges |
| Features | **all of them** — postings are authored here | **posting-free, forever** |
| `--lint` | `errors=0 accepted=14 warnings=70 exit=0` — clean, via its ledger | `errors=23 accepted=0 warnings=64 exit=1` — **correct**: it has no ledger and it really is defective |
| Why | the board filling and emptying is the readout; it has to be the town you actually run | it pins seed-independence, the 12/6/2 counts, the three hash literals, and the golden day's beats |

**The live town was rebuilt wholesale on 2026-07-16** (Panda's ruling). The 12 original townee
names/bios were retained and 6 added; **the schedule and social web underneath were not** — of 16
dayplans, 5 are wholly new and essentially no block boundary survived, and 39 of 49 regard edges are
new. Every retained edge is now actually convened (Tam↔Brindle 0→6 awake slots, Corvo↔Marrow
unreachable→8, Karsk↔Fenn ~4→14). The retention deviated from the ruling as given and was **re-ruled
to stand** — see `DEV-LOG.md`, 2026-07-16.

**"Frozen" (`PNO.D2`) means the fixture does not drift *silently* underneath what it pins** — its text
bans **adding** postings, sites or cast. It was never a claim that a fossil in it cannot be corrected by
an explicit ruling. It has been corrected exactly once: `FBT.Q1` (2026-07-16) dropped 2 of `golden/day1.json`'s
7 beats — `stock-runs-low`, `fetch-arranged` — because both were **fossils of the trade ratchet**, firing
only because Petch's trade fell below 0.35, which only happened *because of the defect*. **That acceptance
list had been pinning the bug rather than catching it.** The golden day now pins **5** beats.

**Do not add postings, sites, or cast to the fixture.** Its entire value is that it stopped changing.
A golden master living inside the live data directory tracks the very thing it is meant to pin — which
is what it did until 2026-07-16, and why this split exists. The fixture is a **full copy** (`TownLoader`
requires all 5 files; there is no overlay/merge), so it will drift from `data/`. **Drift is the feature.**

Files, both towns: `simconfig.json` (every knob's default) · `places.json` (board cards + residences;
`board:true/false`, `shut`) · `townees.json` (`departs_day` schedules an adventurer's expedition) ·
`dayplans.json` (one template per role; `haunt:<id>` tokens, courier `roams`) · `traits.json`
(`pressure_rate_mods` — **`{gain, decay}` or a bare number**, see below; `storylet_weight_mods`,
`hearsay_carrier`, `pressure_targets`) · `storylets/*.json` (`_binding` anchors, `must_fire` override).
**Live town only:** `postings.json` (authored seeds + generator templates) · `lint-accepted.json` (the
acceptance ledger). **Fixture only:** `golden/day1.json` (the pinned beat types + participants the M3
test reproduces; `TownLoader` treats it as optional, so the live town reports `Town.Golden == null` and
that is correct).

**`pressure_rate_mods` is signed-aware.** `{"gain": n, "decay": m}` splits the scalar by the sign of the
base drift. **A bare number still parses and still means `{gain: n, decay: n}` — today's arithmetic, to
the bit**, which is both the back-compat rule and why the frozen fixture is untouched by the change. The
reason it had to change is one sentence: **multiplication preserves sign, so a scalar could only ever
scale a drift's magnitude, never its direction** — every trait was a volatility knob wearing a
direction's name (`wanderlust ×1.3` made an engaged Tam *settle 30% faster*). Migration is a judgement
about what a word means, so the loader never makes it silently: `--lint`'s `legacy-rate-mods` names the
traits still authored bare (8 in the fixture, deliberately and forever; 0 in `data/`).

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
- **M3 storylets + summary** ✅ — 50-rule bank in the live town (12 in the fixture); chronicle with
  because-lists; dawn summary + actionability dial + hearsay-lite; bio-marks behind the FB.8 toggle;
  **golden day reproduces its 5 scripted beats** (was 7 — `FBT.Q1` dropped two ratchet fossils; see
  *Data contract*).
- **M4 creation + research instruments** ◑ — seeded town generator (invariant-guaranteeing) ✅;
  four creation menus in the observatory ✅; stats strip ✅; CLI soak ✅; `--lint` / `--report` /
  `--knob` ✅.
  > **`VFB.Q1` IS SATURATED — read this before you tune anything against it** (2026-07-16).
  > The metric is specified as "avg distinct tellable lines/night", but it counts distinct lines
  > *within* a summary **already cut to `summary_lines` (5)**. So it is `min(pool, 5)` averaged, and
  > the pool measures **≥20 every night**. It reads a flat **5.00** and cannot move. Ablation settled
  > it: **delete 30 of the 46 rules and it still reads 5.00, with 0/42 nights below 4.** The old figure
  > (~4.4, 3/21 nights below 4) is **superseded, not falsified** — it was honestly measured on the old
  > town, and *the comparison between the two numbers is itself the finding*. The metric cannot see the
  > thing it was built to measure. The soak still prints it, labelled, for continuity.
  > **Its replacement is `Api/Variety.cs`** — distinct rendered *texts* (not rule ids), told/fired,
  > rules-fired-but-never-told, repeat vs night N−1 and vs any prior night. **Deliberately un-scored:**
  > no threshold, because a threshold invites tuning toward it — and it was written by a different agent
  > than the ones it judges. Live town, 3 seeds × 14 nights, measured 2026-07-16:
  > **47 distinct sentences of 70 delivered lines · novelty 0.67 · repeat(N−1) 0.00 · repeat(any) 0.35 ·
  > told/fired 70/319 = 0.22 · 5 of 50 rules fired but never told.**
- **PNO.M1 the board** ✅ *(see `plans/PLAN-fishbowl-postings-outings.md`)* — `Engine/Board.cs`, the
  `post` effect, the `posting` predicate, `postings.json`. Measured on the live town: a posting **files
  on day 2** (`posting-filed`, Petch @ Petch's Simples, slot 16) and **expires on day 6**
  (`posting-expired`, slot 0 — `expires_days: 4`). Expiry is a **board mechanism, not a rule**, so it is
  the first chronicle entry ever built outside `BuildEntry` and synthesizes its own because-list (5
  facts). It is tellable at 0.3: over 14 nights it fires 10× and reaches a summary **once**, on day 8.

## Build · test · run

```bash
# core (engine-free — no Godot needed)
dotnet test  core/Fishbowl.Core.Tests/Fishbowl.Core.Tests.csproj      # 53 tests
dotnet run   --project core/Fishbowl.Cli -- --days 3 --chronicle      # readable trace
dotnet run   --project core/Fishbowl.Cli -- --soak --days 14          # variety instrument (VFB.Q1 is saturated)
dotnet run   --project core/Fishbowl.Cli -- --lint --town data        # content linter: errors=0 accepted=14 warnings=70
dotnet run   --project core/Fishbowl.Cli -- --report out.json         # machine-readable run JSON
dotnet run   --project core/Fishbowl.Cli -- --soak --days 14 --knob novelty_decay=1.0   # ablate the fatigue term

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

## `--lint` and its acceptance ledger (`data/lint-accepted.json`)

A **content** linter, not a code one: it runs the **real** `Clockwork`/`Simulation` and reads outcomes
off `World.PressureLog`, because *a linter that reimplements the thing it audits eventually audits its
own fiction* — which this one has twice paid to learn. Findings are `error` (a proof) or `warn` (a
smell). `--json` emits them machine-readably.

**The ledger is not a suppression list, and the file's own header is the contract.** Keyed exactly on
`(check, kind, subject)` — ordinal, no globs, subjects enumerated one at a time. The price of a line is
a **written `reason` and a `ruling`**, both required, both non-blank, and the ruling has to resolve; an
entry without them fails the build. Crucially:

- **Nothing is silenced.** Every accepted finding still prints **in full, every run**, with its reason
  and ruling above it, and keeps `class=error` because it is still a proof. Only `gates` moves.
- **A subject nobody listed is a NEW defect and still fails the build** — verified by injecting a 15th
  ratchet and watching it gate.
- **An acceptance outliving its finding warns** (`lint-accepted`/unmatched): it would otherwise be a
  standing pre-authorisation for that exact defect to walk back in unnoticed.
- **`load` / `lint-aborted` / `lint-accepted` can never be accepted** — each means the report itself is
  untrustworthy.
- **There is no ledger in `tests/towns/golden-town/` and there must never be one** (frozen, `PNO.D2`).
  That town is genuinely defective; its 23 errors must stay red.

Current: **live town `errors=0 accepted=14 warnings=70 exit=0`** · **fixture `errors=23 accepted=0
warnings=64 exit=1`**. The 14 are all `restlessness` ratchets under one ruling (one entry, one
`subjects` array — 14 copies of one sentence would misrepresent one judgement as fourteen).

**Two checks have been badly wrong, and both are worth knowing before you trust a third.** `latch-die`
"got 7 of 7 of its condemnations backwards" (`Linter.cs:578`) — the two rules it most confidently told
authors to delete were the two most *ungated* rules in the bank. And `ratchets` reported 3 findings, all
by-design, while blind to all 26 real ones, because it tested the **per-mode sign** rather than the
**net drift over the day actually lived**. Both were hand-modelling the engine instead of asking it.

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
  **And it has now happened twice.** On 2026-07-16 `golden/day1.json` turned out to be doing the same thing
  one layer down — pinning two beats that fired *only because* of the trade ratchet (`FBT.Q1`, see *Data
  contract*). **An acceptance test that encodes a defect looks exactly like one that encodes a requirement**,
  and the golden one survived a hash pin, a 30-test suite and a determinism contract, because a ratchet is
  perfectly deterministic. Determinism was never the missing property; two-wayness was, and nothing asserted
  it (`M2_TradeEquilibriumTests` now does). The only thing that told a fossil from a requirement was fixing
  the defect and watching the test object.
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
| Knobs — *rendering, applies now* | `knob-actionability` · `knob-summary_lines` · `knob-novelty_decay` · `knob-hearsay_required` |
| Knobs — *simulation, applies next dawn* | `knob-storylet_rate` · `knob-pressure_rates.trade` · `knob-bio_marks_enabled` |

**The `test_id`s are the knob KEYS and never change; the on-screen labels are not the keys.** Two read
differently to a human: `knob-actionability` is labelled *"register (actionability)"* (it is the register
dial — it is why the line above the summary says `gossip`) and `knob-pressure_rates.trade` is labelled
*"trade drift rate"* (the dotted path implied `.purse`/`.heart` siblings that no slider offers). Resolve by
`test_id`; read the value back with `read_element {contains: "<label>"}` using the **label**, since the value
labels carry no `test_id` of their own.

The **knobs are the `VFB.Q1` tuning surface** (drive them, don't change code) — `drag` a slider by handle, or
`click_element` a checkbox. **They are half live and half deferred, and the UI groups them by which:**
`actionability` / `summary_lines` / `novelty_decay` / `hearsay_required` are **rendering** knobs and re-present
the day you are already looking at, so they apply on release; `storylet_rate` / `pressure_rates.trade` /
`bio_marks_enabled` are **simulation** knobs and cannot apply retroactively without re-running the day (which
would move the day-hash), so they take effect at the **next dawn**. `bio_marks_enabled` looks like a display
toggle beside `hearsay_required` and is not — it writes hashed bio Marks at storylet-fire time.
`novelty_decay` is the inverse trap: it *reads history* (how often a rule was told in the last
`Summarizer.NoveltyWindow` nights) and still applies now, because the summary is derived on read rather than
stored — the ledger is re-folded out of the chronicle on every render, so moving the knob re-orders nights
that are already finished. **Drag it to 1.0 to ablate it**: that is exactly the pre-2026-07-16 fixed
leaderboard, which told 29 distinct sentences in a fortnight and never once told 23 of the rules that fired.
See the addon's `README.md` for the full command API and activation precedence.
**Captures need a rendered window** — pixels are blank under `--headless` (`GTH.D7`).

## Rulings adopted (parent-plan "The asks")

`VFB.D1` 48×30-min slots · `VFB.D2` engine-free core + CLI + xUnit · `VFB.D3` JSON-only storylets
· `VFB.D4` 12 townees / 6 board-places / 2 adventurers · `FB.8` bio-marks ratified **on**,
toggleable. Build-time decisions layered on top and recorded in `DEV-LOG.md`: `_binding`-anchored
storylets, the awake gate, `departs_day` scheduling, regard-via-effects-only, and
canonical-hash quantization at 6 decimals.

> **`VFB.D4`'s numbers now describe the fixture, not the live town** (2026-07-16). The wholesale rebuild
> took `data/` to **18 / 8 / 4**; `tests/towns/golden-town/` still pins **12 / 6 / 2**, and the `M0`
> equality assertions run against *it*. The ruling's *reason* — "readouts stay readable, tuning stays
> tractable" — was never re-litigated at 18; it simply has not been re-tested. Worth a look if the roster
> starts reading badly, not before.

**Rulings landed 2026-07-16, all recorded in `DEV-LOG.md`:** `NTD.Q1` (`trade` becomes a restoring
force — the one hash move) · `FBT.Q1` (the golden day may drop two ratchet fossils) · the wholesale
town rebuild + the ruling to **keep** the retained cast · the Summarizer novelty term at default
**0.5** · **restlessness ships broken, knowingly** (see the open question under *The two Pressures
shapes*) · `--report` serializes through `DataJson.ReportPretty`, ASCII-escaped, because machine output
has a different job from authored prose.

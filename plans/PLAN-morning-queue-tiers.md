# PLAN — morning-queue-tiers (code / script / data split + .NET)

**Status:** proposed 2026-07-15 — awaiting rulings on `MQT.D1`–`MQT.D3` before any phase runs.
**Folder:** `../adventuring-guild-teller/morning-queue/` (the Godot 4.6 desk-shift prototype).
**Specs it touches:** `MORNING-QUEUE.md` (architecture + invariants), `CONTENT-BANKS.md` (§4 recipes), this plan, `PLAN-adventuring-guild-teller.md` (parent).
**Mnemonic:** **MQT** — phases `MQT.1`–`MQT.6`, decisions `MQT.D1`–`MQT.D3`.
**Handoff:** `../adventuring-guild-teller/morning-queue/MQT-HANDOFF.md` — operating
manual for a Sonnet 5 coordinator: gates, settled architecture, and paste-ready
subagent briefs (WP-A/B/C Sonnet · WP-D/G Opus · WP-E Fable · WP-F Haiku). It adopts
this plan's recommended rulings as defaults; confirming them is its kickoff gate.

## The brief (chat, 2026-07-15)

> Check the morning-queue project and propose a refactor plan splitting **code, script
> and data** properly. If the project does not include .NET, include adding that as part
> of the plan.

Working definitions (from the same chat): **code** = engine-independent machinery —
systems, domain types, rules, tools; **script** = node-attached behavior the engine
lifecycle drives (`_ready`, signals, glue); **data** = authored content + config. In a
GD+.NET Godot project the natural mapping is C# for code, GDScript for script, JSON/.tres
for data — by convention, not engine requirement.

## Audit — where the tiers blur today (2026-07-15)

The prototype is healthy (12/12 harness assertions, `7 days, 97 visits, 0 problems`) and
its data tier is already genuinely good: five JSON banks, schema documented, validator at
boot. The blur is concentrated in scripts — of 4,347 GDScript lines, roughly **1,500
(~35%) are code or data wearing a script costume**:

| File | Lines | Nominal tier | Actual content |
|---|---|---|---|
| `scripts/gen/ShiftGenerator.gd` | 1,151 | script | **code** — pure deterministic composer (reads banks, writes nothing, emits schema) + ~40 lines of embedded **data** (decoy-scale prose, walk-in professions, the hardcoded `0.25` depth-rate in `_quote`) |
| `scripts/components/ReferencePanel.gd` | 941 | script | script, but lines ~622–666 re-implement the `accept`/`total` **limit rule** the generator also implements (`_limit_result`) — one rule, two homes |
| `scripts/autoload/DeckLoader.gd` | 324 | script | ~170 lines I/O + autoload shell (script) / ~155 lines schema **validation** (code) |
| `scripts/loc.gd` | 236 | script | ~55 lines humanize/fallback logic (script) / ~180 lines `_LOCALES` string tables (**data** — the file itself says "adding a locale is a data change," but today it's a code edit) |
| everything else (Main, components, theme, dev, Session) | 1,695 | script | correctly script — stays GDScript |

**.NET status:** absent. `project.godot` carries a `[dotnet] assembly_name` stamp (the
mono editor left it) but there is no `.sln`/`.csproj`/`.cs` anywhere → per the brief, the
plan adds it. Box check: .NET SDK **9.0.202** installed; Godot 4.6 mono needs SDK 8+. ✔

**The Web-export fact (verified 2026-07-15):** C#/.NET projects **still cannot export to
Web** in Godot 4.x, 4.6 included — prototypes only (GodotCon Boston demo, tracking
discussions). Sources: [Godot docs — Exporting for the Web](https://docs.godotengine.org/en/latest/tutorials/export/exporting_for_web.html),
[godot-proposals #13076 (4.6 / .NET 10 / C# Web)](https://github.com/godotengine/godot-proposals/discussions/13076),
[godot-proposals #10310 (WASM for C#)](https://github.com/godotengine/godot-proposals/discussions/10310),
[forum thread](https://forum.godotengine.org/t/is-there-an-update-on-exporting-c-projects-to-web/128821).
So `MORNING-QUEUE.md`'s invariant ("GDScript-only so the Web-export path stays open") is
factually current, and adding .NET **contradicts it** — that is decision `MQT.D1`, not a
thing to slide past.

## Target architecture

```
morning-queue/
├── project.godot              # unchanged apart from what the mono editor manages
├── MorningQueue.sln
├── MorningQueue.csproj        # Godot.NET.Sdk/4.6.x · net8.0 · Compile Remove core/** · refs Core
├── data/                      # ═ DATA — all authored content + config
│   ├── (five banks, as today)     visitors / references / townees / adventurers / generation
│   ├── locales/en.json            ← Loc._LOCALES moves here (MQT.1a)
│   └── (bank extensions)          ← decoy scales, walk-in professions, payout.depth_rate (MQT.1b)
├── core/                      # ═ CODE — pure .NET, zero Godot references; .gdignore'd
│   ├── MorningQueue.Core/         # typed model · Validator · ShiftComposer · payout math
│   └── MorningQueue.Core.Tests/   # xUnit: golden weeks, validator red/green, schema round-trip
├── cs/
│   └── CoreBridge.cs          # the ONE engine-facing C# file — JSON text in → Variant out
├── scenes/                    # ═ SCRIPT — unchanged
└── scripts/                   # ═ SCRIPT — Main, components, autoload shells, dev, thin loc.gd
```

**Boundary rules (the whole trick):**
- Cross the GDScript↔C# line **coarsely and rarely**: one `Validate` call at boot, one
  `PrepareShift` call per `load_day`. JSON text goes in, one marshaled Variant comes out.
  Never per-frame, never per-string.
- **File I/O stays script-side** (`FileAccess` reads `res://` — required in exports;
  `System.IO` can't see a PCK). Deck reads text; Core parses text into the typed model.
- **`Loc` stays script-side** (components call it per-draw); only its *tables* move to data.
- **Frozen contracts stay frozen.** Every component signature, autoload name, and signal
  in `MORNING-QUEUE.md` §Architecture is untouched; scenes never change. All bridging
  hides inside `DeckLoader.gd`. `GameState.gd` (79 lines of signal spine) stays GDScript.
- Generation stays **runtime + stateful**: `ShiftComposer.Generate(day, banks, duesState)`
  receives live dues (severing today's hidden `Deck.*` global coupling but preserving the
  floor-beat → next-shift interlock exactly).

## Decisions — need Panda's ruling before phases run

- **`MQT.D1` — runtime mode vs the Web invariant.** Options:
  **(A′) In-engine C#** *(recommended)*: mono runtime (already the MCP default binary),
  full fidelity including the dues interlock. Rewrites the invariant to: *"Web embed
  deferred until Godot ships .NET Web export; `core/` stays engine-free, so a `dotnet`
  pre-bake CLI (mode B) can restore a GDScript-only runtime if the local-site embed
  becomes pressing first."* Honest cost: no Web export while any C# is in the runtime,
  and `Godot_std.exe` can no longer run the project (mono build only).
  **(B) Out-of-engine baker**: all C# lives in a CLI that pre-bakes `data/shifts/day-N.json`;
  runtime stays GDScript-only and Web-exportable. Honest cost: pre-baked day N+1 can't see
  dues paid at runtime (the floor beat's one mechanical consequence dies or needs a patch
  pass), plus a bake step in the edit loop. Rationale for A′: the Web embed is a *local-only*
  aspiration (Pages can't set COOP/COEP anyway), no Web export has ever been produced, and
  A′ keeps B one small CLI away because Core is pure.
- **`MQT.D2` — RNG continuity.** Godot's `RandomNumberGenerator` (PCG32) and .NET's
  `Random` produce different streams: the C# port changes every generated shift for the
  same day-seed. **(a) Rebaseline** *(recommended)*: accept new-but-still-deterministic
  weeks; pin days 1–7 as golden-file fixtures in `Core.Tests` from then on. (`.captures/`
  is gitignored; the rev-3 heuristic-study artifacts stay valid as historical record.)
  **(b) Port PCG32** (+ Godot's bounded-int/randf mapping) for byte-identical weeks —
  only worth it if continuity of the exact current shifts matters.
- **`MQT.D3` — theme datafication.** Optional: replace code-built `ThemeFactory` with the
  originally-planned `theme/queue_theme.tres` (data tier). Default: **skip** — it works,
  it's readable, zero player-facing gain; revisit only if a second theme appears.

## Phases

- [x] **`MQT.1` — Data out of scripts** *(executed 2026-07-15)* *(pure GDScript churn; zero behavior change; no .NET yet)*
  - 1a. `_LOCALES` → `data/locales/en.json`; `loc.gd` keeps `t/humanize/vocab` + fallback
        logic and gains a ~15-line static loader. "Adding a locale is a data change"
        becomes literally true.
  - 1b. Generator-embedded content → banks: `_WALKIN_PROFESSIONS` → `generation.json`
        `name_pools.professions`; the `_decoy_scale` table → `generation.json`
        `decoy_scales`; `_quote`'s `0.25` → `references.json` `payout.depth_rate`.
  - Verify: boot self-check still `7 days, 97 visits, 0 problems`; day-1 output
    byte-identical (no RNG-order change); F12 capture spot-check.
- [x] **`MQT.2` — Stand up .NET** *(structure only, executed 2026-07-15)*
  - `MorningQueue.sln` + root csproj (`Godot.NET.Sdk/4.6.*`, net8.0, `Compile Remove
    core/**`, `ProjectReference` → Core) + empty `cs/CoreBridge.cs`; `core/` classlib +
    xUnit test project; `.gdignore` inside `core/`; `.gitignore` += `bin/`, `obj/`.
  - Verify: `dotnet build` + `dotnet test` green; `run_project` via the godot MCP still
    boots day 0. **Dev-loop change to document:** launching the game outside the editor
    now needs `dotnet build` first (editor Play builds automatically).
- [x] **`MQT.3` — Domain model + validator become code** *(executed 2026-07-15)*
  - POCOs for Visit/Claim/Truth/Check/Inspections + the five banks (System.Text.Json);
    port `_validate_banks/_validate_shift/_validate_inspections/_validate_standing_orders`
    into `Core.Validator`; add the **derive pass** that annotates every visit (curated
    *and* generated) with `inspections.scale.verdict` ∈ within/over/under/meets/no_order.
  - Bridge: `Validate(...jsonTexts) -> errors[]`, `PrepareShift(shiftJson, banksJson) ->
    annotatedJson`. Deck drops its ~155 validation lines (keeps file-missing/parse errors).
  - Verify: validator green over the real `data/`; red-case fixture per breakage axis;
    boot output unchanged.
- [ ] **`MQT.4` — Generator becomes code** *(the big move; executes `MQT.D2`)*
  - Port `ShiftGenerator.gd` (1,151 lines) → `Core.ShiftComposer`: pure
    `Generate(day, banks, duesState)`; templates/decoys/professions read from the banks
    (`MQT.1b`); no `Deck.*` reads. `Deck.load_day(d>0)` routes through the bridge.
  - Pin determinism: golden-file fixtures for days 1–7 committed in `Core.Tests` +
    distribution sanity asserts (task/axis coverage, no silent fallback visits).
  - Retire `scripts/gen/ShiftGenerator.gd`; regenerate the class-name cache
    (`--headless --import` — the known gotcha, now in reverse).
  - Verify: MCP auto-step day 0 → 16/16 and day 1 → N/N; `dotnet test` green (the boot
    self-check may then shrink to a smoke line or move entirely into tests).
- [ ] **`MQT.5` — De-duplicate rules out of components**
  - `ReferencePanel` consumes the precomputed `scale.verdict` (delete its ~45-line
    `accept`/`total` rule copy; keep the verdict→`Loc` key + `Palette` color mapping —
    that part *is* presentation). Sweep the other three components for rule copies
    (expected: none).
  - Verify: DeskFeatureHarness 12/12; capture diff on `nessa-broom` (amount-fail) and a
    `total`-order visitor.
- [ ] **`MQT.6` — Doc + plan sync (Rule 3) and final pass**
  - `MORNING-QUEUE.md`: engine line (drop "GDScript-only", state the three tiers + mono
    requirement + `dotnet build` in the run loop), architecture diagram gains the `core/`
    box, **Invariants rewritten per `MQT.D1`** (with the Web-status citation), and a
    short **Code map** section for `core/` (see tier note below).
  - `CONTENT-BANKS.md` §4 recipe pointers → the C# files; tick the parent item in
    `PLAN-adventuring-guild-teller.md`; DEV-LOG entry before the implementation commit;
    full capture pass.

## Risks & gotchas

- **Marshalling discipline** is a review item on every bridge change: JSON-text across,
  once per boot / per `load_day` — the moment someone crosses per-visitor or per-string,
  the split has failed its own thesis.
- **Root csproj globs `**/*.cs`** → without `Compile Remove="core/**"` the game assembly
  double-compiles Core. Pair with `.gdignore` in `core/` (importer) — they solve
  different halves.
- **Mono-only runtime** after `MQT.2`: `Godot_std.exe` can't open the project; the MCP
  already invokes the mono exe, so the daily loop is unchanged.
- **Test fixtures** reference `../../data/*.json` relative to the test project — keep the
  repo-root-anchoring spirit of Rule 1 (no CWD assumptions).
- **Tier bookkeeping (repo Rule 2):** ruling proposed here — this C# is
  *deliverable-internal* to the musing's prototype (same standing as `build-musing.py`),
  so its code-doc is a **Code map** section in `MORNING-QUEUE.md`, not a repo-level
  `CodeDocs/` tier. Stand the full tier up only if C# ever escapes this folder. Flag if
  that ruling should go the other way.
- **Rule 6:** n/a — this folder never reaches a public surface (not in the site build);
  commit identity is already clean.

## Definition of done

`dotnet test` green (validator + golden weeks) · MCP auto-step day 0 = 16/16, day 1
clean · DeskFeatureHarness 12/12 · no rule logic in any component script · no authored
prose in any script · `MORNING-QUEUE.md` invariants + diagram match reality · parent
plan + index + DEV-LOG synced.

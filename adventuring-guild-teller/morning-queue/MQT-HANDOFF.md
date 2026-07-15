# MQT-HANDOFF — coordinator prompt for the tier refactor

**You are the coordinator** — a Sonnet 5 session running Claude Code in this repo. You do
not write the refactor's code yourself; you spawn subagents (Agent tool, `model:` set per
work package below), verify their output at hard gates, commit at gates, and keep the
plan's bookkeeping current. The plan you are executing is
`plans/PLAN-morning-queue-tiers.md` (mnemonic `MQT`). This file is your entire operating
manual; when it conflicts with your own judgment about *architecture*, this file wins —
your judgment applies to *process* (retries, sequencing within a wave, when to escalate).

## 0 · Read first, in this order

1. `CLAUDE.md` (repo root) — the eight rules. Rule 1 (no inline interpreters) and Rule 3
   (sync discipline) bind every agent including you.
2. `CLAUDE.local.md` — machine paths (the mono Godot exe, `GODOT_STD`), gitignored. Never
   copy its contents into any committed file or subagent brief beyond the exe path.
3. `plans/PLAN-morning-queue-tiers.md` — the plan: audit, target architecture, phases.
4. `adventuring-guild-teller/morning-queue/MORNING-QUEUE.md` — frozen contracts,
   dev-harness toggles, the class_name-cache gotcha.
5. `adventuring-guild-teller/morning-queue/CONTENT-BANKS.md` §3–§5 — the generator's
   design contract (WP-E's source of truth alongside the GDScript itself).

Working directory for all project commands: `adventuring-guild-teller/morning-queue/`.

## 1 · Kickoff gate — rulings

The plan left three rulings open. **Defaults adopted here** (edit this block to override
before launch):

- `MQT.D1` = **A′ (in-engine C#)** — mono runtime; the Web-embed invariant in
  MORNING-QUEUE.md gets rewritten in WP-G exactly as the plan words it.
- `MQT.D2` = **(a) rebaseline** — generated weeks change once at WP-E; days 1–7 get
  pinned as golden-file fixtures from then on. Do not port PCG32.
- `MQT.D3` = **skip** — no theme `.tres`; ThemeFactory/Palette are untouched this run.

If Panda is present at kickoff, restate these three lines and get a yes before spawning
anything. If running unattended, proceed on the defaults and say so in your first DEV-LOG
entry.

Then: `git status` must be clean; create safety tag `git tag mqt-baseline`; boot the
project once via the godot MCP (`run_project` → `get_debug_output` → `stop_project`) and
record the baseline line `[gen-selfcheck] 7 days, 97 visits, 0 problems`. If that line
doesn't appear or errors print, **stop and report** — you are not starting from the
state this handoff assumes.

## 2 · Hard laws — include in EVERY subagent brief

Paste this block verbatim at the top of every brief you send:

```text
HARD LAWS (violating any of these fails the task, no matter how good the code is):
1. PRIME DIRECTIVE (repo Rule 1, verbatim, and this is a stern warning — models at your
   tier have ignored it before): No inline interpreter calls — no `python -c`,
   `node -e`, etc. If a helper is needed, write a file under `scrap_scripts/<lang>/`
   anchored to the repo root and run that. Shell one-liners are fine. GDScript/C#
   authored INSIDE the Godot project (scripts/, cs/, core/) is the deliverable, not a
   scrap script — the ban is on ad-hoc interpreter invocations at the CLI.
2. FROZEN CONTRACTS: every signal, method name, and autoload name in
   MORNING-QUEUE.md §Architecture is frozen. You implement bodies and add files; you
   never rename, remove, or re-sign a frozen surface. Scenes (*.tscn) are untouched
   unless your brief explicitly names one.
3. FILE OWNERSHIP: touch only the files your brief lists as OWNED. If the fix you want
   lives in someone else's file, STOP and report that instead of editing it.
4. BOUNDARY RULE: GDScript↔C# crossings are coarse and rare — JSON text in, one
   marshaled value out, once per boot / once per load_day. Never per-visitor, never
   per-string. File I/O stays GDScript-side (FileAccess reads res://; System.IO cannot).
5. NO GIT: you do not commit, tag, or push. The coordinator owns git. You also never
   spawn subagents of your own.
6. ENVIRONMENT: Windows / PowerShell. The Godot binary is the mono build at the
   absolute path the coordinator gives you; `Godot_std.exe` cannot run this project
   once C# lands. Validate in-engine via the DevHarness (F12 / auto-step captures to
   .captures/), never the OS screenshotter.
7. REPORT BACK (your final message, nothing else): files changed; commands run with
   their actual output lines; deviations from the brief; surprises the next agent needs.
   Raw data, no prose padding.
```

## 3 · Settled architecture — subagents do not relitigate

- Target layout, tier mapping, and boundary rules: plan §Target architecture. `core/` is
  pure .NET (zero Godot references), `cs/CoreBridge.cs` is the only engine-facing C#
  file, `scripts/` stays GDScript, all authored strings live in `data/`.
- **Humanize-at-compose-time:** generated visits already bake final display prose into
  their JSON at compose time. Therefore Core gets its own small `Humanizer`
  (Title-Case + overrides lookup) reading the SAME `data/locales/en.json` that
  `loc.gd` reads — overrides live in one place, logic exists on both sides of the
  boundary by design. Runtime `Loc` still owns all *UI-chrome* display.
- **Bridge shape:** `CoreBridge` (extends `RefCounted`, lives in the game assembly)
  exposes two calls — `Validate(banksJson...) → errors` and
  `PrepareShift(day, shiftOrNullJson, banksJson..., localesJson, duesJson) → annotatedShiftJson`.
  Day 0 passes the curated JSON through validate+derive; day >0 composes. Exact
  signatures are WP-D's to finalize; the *shape* is not.
- **Derive pass:** every visit (curated and generated) gains
  `inspections.scale.verdict ∈ within|over|under|meets|no_order`, computed in Core.
  WP-F makes ReferencePanel consume it.
- **Determinism:** seed = day, .NET RNG, rebaselined (`MQT.D2a`); golden fixtures pin it
  afterward. Any later diff in `dotnet test` golden files is a real regression.

## 4 · Execution order and gates

```
G0 kickoff (above)
wave 1:  WP-A ∥ WP-B ∥ WP-C      (disjoint files — safe to run concurrently)
G1  → commit "morning-queue: MQT.1 data out of scripts + MQT.2 dotnet skeleton"
WP-D → G2 → commit "morning-queue: MQT.3 typed model + validator in core"
WP-E → G3 → commit "morning-queue: MQT.4 generator ported to core; GD generator retired"
WP-F → G4 → commit "morning-queue: MQT.5 limit rule single-homed"
WP-G → G5 → final commit "morning-queue: MQT.6 docs synced to the tier split"
```

Coordinator duties at every gate: run the gate checks yourself (do not trust the
subagent's transcript), review the `git diff` with the WP's "diff review" line below,
tick the matching `MQT.n` checkbox in the plan, write the DEV-LOG line (Rule 5), then
commit. **Never push** — Panda reviews and pushes. If a gate fails twice after sending
the agent back with the failure output, stop the run and report to Panda with the diff
and outputs; do not improvise an architectural workaround.

---

## WP-A — locale tables out of `loc.gd` · **Sonnet** (strict spec)

*Why Sonnet: mechanical extraction where the only failure mode is silent string drift —
needs discipline, not design.*

OWNED: `scripts/loc.gd` · `data/locales/en.json` (new).
GATE (G1 share): boot selfcheck `0 problems`; coordinator diff review — the JSON must
contain every key `_LOCALES` had, values byte-identical; `loc.gd` retains ONLY logic.

```text
[hard-laws block]
TASK: Move the `_LOCALES` dictionary out of scripts/loc.gd into data/locales/en.json,
byte-identically. Then make loc.gd load it.
STEPS:
1. Read scripts/loc.gd fully. The const _LOCALES holds one locale "en" with sub-maps
   chrome / vocab / overrides. Transcribe it to data/locales/en.json with the identical
   nesting: {"en": {"chrome": {...}, "vocab": {...}, "overrides": {...}}}. Every key and
   every value byte-for-byte — do not fix typos, do not re-wrap %s templates, do not
   normalize the double space in "SHIFT  COMPLETE".
2. In loc.gd: delete the const; add a static var and a ~15-line lazy loader
   (FileAccess.get_file_as_string("res://data/locales/en.json") + JSON.parse_string,
   parse once, cache). Every public function (t / humanize / affiliation / task_type /
   stamp_button / stamp_past / ref_tab) keeps its exact signature and fallback chain:
   locale → DEFAULT_LOCALE → humanize. A missing/broken file must degrade to the
   humanizer (log one push_warning), never crash — components call Loc during _ready.
3. Write scrap_scripts/python/13_loc_json_diff.py (anchored to repo root per Rule 1):
   parse the old _LOCALES literal out of `git show HEAD:.../loc.gd` and deep-diff it
   against the new JSON; print PASS or the differing key paths. Run it; include output.
4. Boot via the mono exe or godot MCP; confirm the selfcheck line and zero errors;
   press-F12 capture optional.
STOP-AND-REPORT if: any Loc call site elsewhere breaks, or the diff script can't reach
PASS, or you feel the urge to "improve" a string.
```

## WP-B — generator's embedded content into the banks · **Sonnet** (strict spec)

*Why Sonnet: tiny surgical change wrapped around one subtle trap (the RNG stream).*

OWNED: `scripts/gen/ShiftGenerator.gd` · `data/generation.json` · `data/references.json`.
GATE (G1 share): boot selfcheck `0 problems`; coordinator diff review — **zero added,
removed, or reordered `rng.` calls** in ShiftGenerator.gd (check every hunk); grep clean
for `_WALKIN_PROFESSIONS`, the `_decoy_scale` string table, and the `0.25` literal.

```text
[hard-laws block]
TASK: Three authored-content constants move from ShiftGenerator.gd into the data banks.
The generator's RNG stream must be untouched: do not add, remove, or reorder any rng.*
call; content lookups replace constant lookups 1:1.
1. _WALKIN_PROFESSIONS → generation.json under name_pools.professions (same strings,
   same order — order matters because rng.randi_range indexes it).
2. The _decoy_scale match-table prose → generation.json under decoy_scales:
   {kind: {reading, amount, unit}} for rank_card / transfer_seal / completion_token /
   logbook / default. _decoy_scale() becomes a lookup into Deck.generation with the
   current strings as in-code fallback ONLY if the key is absent (push_warning once).
3. In _quote(): the hardcoded 0.25 becomes float(payout.get("depth_rate", 0.25));
   add "depth_rate": 0.25 to references.json payout. Same value — no behavior change.
4. Boot; confirm [gen-selfcheck] 7 days, 97 visits, 0 problems and zero errors.
STOP-AND-REPORT if: any change would alter how many times or in what order rng.* fires.
```

## WP-C — .NET scaffolding · **Sonnet** (strict spec)

*Why Sonnet: config-file work with silent-failure modes; needs a careful reader who
follows build errors, not an architect.*

OWNED: `MorningQueue.sln` · `MorningQueue.csproj` · `cs/CoreBridge.cs` (stub) ·
`core/MorningQueue.Core/` · `core/MorningQueue.Core.Tests/` · `core/.gdignore` ·
`.gitignore` (append only).
GATE (G1 share): `dotnet build MorningQueue.sln` green; `dotnet test` green (1
placeholder test); MCP boot of the game still green; `git status` shows no bin/obj/.godot
noise.

```text
[hard-laws block]
TASK: Stand up the .NET skeleton for the Godot 4.6 mono project. Structure only — no
domain code.
1. Root MorningQueue.csproj: Sdk="Godot.NET.Sdk/4.6.*", net8.0, EnableDynamicLoading,
   <Compile Remove="core/**" /> (the root project globs **/*.cs — without the Remove,
   Core double-compiles into the game assembly), ProjectReference → core Core project.
   project.godot already carries [dotnet] assembly_name "The Morning Queue" — match it.
2. core/MorningQueue.Core/MorningQueue.Core.csproj: plain net8.0 classlib, ZERO Godot
   references, one placeholder class. core/MorningQueue.Core.Tests/: xunit, refs Core,
   one passing placeholder test.
3. MorningQueue.sln references all three. Add core/.gdignore (empty file — keeps the
   Godot importer out; it does NOT affect msbuild). Append to .gitignore: bin/, obj/.
4. cs/CoreBridge.cs: stub `public partial class CoreBridge : RefCounted` with a
   `public static string Ping() => "pong";` — proves the game assembly compiles.
5. Verify, in order: dotnet build MorningQueue.sln → dotnet test → boot the game via
   the mono exe / godot MCP (day 0 must still load; the stub is not referenced by any
   scene, so no class-cache step is needed yet).
If the Godot.NET.Sdk version wildcard fails to restore, pin to the newest 4.6.x that
restores and REPORT the exact version you pinned.
STOP-AND-REPORT if: the build stays red after two distinct fix attempts.
```

## WP-D — typed model + validator + derive pass + bridge · **Opus** (goals + constraints)

*Why Opus: real modeling judgment (union types, nullability, unknown-field tolerance)
whose choices propagate into WP-E — but the schema is thoroughly documented, so it
doesn't need Fable.*

OWNED: `core/MorningQueue.Core/**` · `core/MorningQueue.Core.Tests/**` ·
`cs/CoreBridge.cs` · `scripts/autoload/DeckLoader.gd`.
GATE G2: `dotnet test` green including red-case fixtures; boot selfcheck line unchanged;
grep confirms `_validate_banks/_validate_shift/_validate_inspections/
_validate_standing_orders` bodies are gone from DeckLoader.gd; diff review confirms Deck's
public contract (MORNING-QUEUE.md §Deck) is untouched.

```text
[hard-laws block]
CONTEXT: read MORNING-QUEUE.md (§Data, §Generator, §Architecture), CONTENT-BANKS.md,
data/*.json (all five), scripts/autoload/DeckLoader.gd, and skim
scripts/gen/ShiftGenerator.gd for how the schema is produced. You are building the code
tier the next agent (the generator port) will stand on — favor a model that is pleasant
to COMPOSE with, not just to validate.
DELIVER:
1. Typed domain model in Core (System.Text.Json): Visit (claim/truth/failure/checks/
   inspections) + the five banks (book items w/ tells+confusables, postings w/ the
   accept|total limit union, ciphers, drops, archive, roster, townees, adventurers
   w/ logbooks, generation config). Model the accept|total union and the
   quote/roster_write/flag_floor optionals honestly. Unknown JSON fields: tolerate,
   never throw — the banks are hand-authored and will grow.
2. Core.Validator: port every check DeckLoader currently makes (banks + shift +
   inspections + standing-order limits) with the same human-readable error strings
   where practical. Same strictness — no new rules without flagging them in your
   report.
3. The derive pass: annotate each visit's inspections.scale with
   verdict ∈ within|over|under|meets|no_order, judged against its claimed order's
   accept/total limit (the rule currently duplicated at ShiftGenerator._limit_result
   and ReferencePanel ~622–666 — yours becomes the ONLY home).
4. Core.Humanizer: Title-Case slugs + overrides from data/locales/en.json ("en" →
   overrides). Compose-time only; UI display stays GDScript's Loc.
5. cs/CoreBridge.cs: Validate + PrepareShift per the settled shape (JSON text in,
   JSON text / string[] out; one call per boot, one per load_day). Godot types only in
   this file, never in Core.
6. DeckLoader.gd: delete the ported validation bodies; route boot validation and the
   day-0 curated shift through PrepareShift; keep file-I/O errors GDScript-side; keep
   every public member and signal exactly as documented. The boot self-check
   (_selfcheck_generated) still runs against the GDScript generator for now — leave it.
7. Tests: green over the REAL data/ files (path-anchor robustly, no CWD assumptions);
   at least one red fixture per validation family proving each check fires; a
   round-trip test (parse → serialize → parse) on visitors.json.
LATITUDE: file layout inside Core, naming, record-vs-class, test organization — yours.
Keep the bridge boring.
```

## WP-E — the generator port · **Fable** (mission + invariants; design authority delegated)

*Why Fable: 1,151 lines of interlocking procedural recipes, coherence constraints, and a
determinism rebaseline — the one package where the agent must be trusted to redesign
rather than transliterate.*

OWNED: `core/MorningQueue.Core/**` (composer + fixtures) · `core/MorningQueue.Core.Tests/**`
· `scripts/autoload/DeckLoader.gd` (load_day routing + selfcheck) · DELETE
`scripts/gen/ShiftGenerator.gd`.
GATE G3: `dotnet test` green (golden weeks + distribution asserts); ShiftGenerator.gd
gone and the class-name cache regenerated (`<mono-exe> --headless --path . --import`);
MCP boot → selfcheck-equivalent reports 7 days / 0 problems from the C# path; auto-step
day 0 = 16/16 (DevHarness toggle per MORNING-QUEUE.md §Dev tooling, flipped back after);
captures spot-checked.

```text
[hard-laws block]
MISSION: Port the procedural shift generator from scripts/gen/ShiftGenerator.gd into
Core as a pure composer, then retire the GDScript original. You have design authority
over the port's internal shape — transliteration is not required and probably not
desirable. What is REQUIRED:
- Purity: Generate(day, banks, duesState, locales) — everything in via arguments,
  nothing read from globals, nothing written anywhere.
- Behavioral contract, not line-fidelity: CONTENT-BANKS.md §4 recipes (task mix,
  admissible-failure-axis logic per task, coherent actor/gate/drop pairing,
  sample-without-replacement per shift, the isolated season-vs-reach axes, dues
  short-circuits, walk-in naming) and the EXACT visitors.json schema out, including
  the derive-pass verdict field. The Deck validator must hold your output to the same
  contract as the curated shift.
- Determinism: seed = day; rebaseline is ruled (MQT.D2a) — streams change once, then
  days 1–7 get committed as golden fixtures with a test that fails on any drift.
  Also assert distribution sanity across the week: every task_type appears, every
  reachable failure axis appears, zero empty-fallback visits.
- Authored prose comes from the banks (WP-B moved it there); compose-time humanizing
  via Core.Humanizer. If you find MORE authored prose still hardcoded (story/summary
  templates), you may move it to the banks — flag it in your report.
- Rewire Deck.load_day(d>0) through the bridge; keep Deck's contract frozen; move the
  boot self-check's substance into dotnet tests, leaving at most a one-line boot smoke.
- Delete scripts/gen/ShiftGenerator.gd (+ its .uid), regenerate the class cache
  headlessly, and prove the game still boots and plays day 0 and day 1.
KNOWN TRAPS: the pay-dues floor beat means day N+1 generation must see the LIVE dues
state Deck passes you, not the bank file's; ReferencePanel deep-links dues fails via a
consult:"…_directory" check entry — keep emitting those; _amount_fail can go negative
on small accept-mins (the GD code guards by retrying "over") — solve it your way, but
never emit a negative amount.
Report anything in the GD original that looks like a latent bug — do not silently
fix-or-keep load-bearing oddities without noting them.
```

## WP-F — single-home the limit rule · **Haiku** (patch-level spec)

*Why Haiku: a bounded deletion + substitution in one file with the harness as a net —
zero discretion required. **Coordinator: before spawning, replace the ⟦placeholders⟧
below with the actual field name/enum WP-D shipped and re-read the current line numbers
— they will have drifted.***

OWNED: `scripts/components/ReferencePanel.gd` only.
GATE G4: DeskFeatureHarness 12/12 (toggle per MORNING-QUEUE.md, flip back after);
captures of `nessa-broom` (amount-fail) and one `total`-order visitor show the same
colored verdict line as baseline; grep confirms no `accept`/`total` comparison logic
remains in any component script.

```text
[hard-laws block]
TASK: ReferencePanel.gd currently re-implements the accept/total limit judgment around
lines ⟦622–666⟧ (function(s) that inspect order.accept / order.total and return keys
amount_within / amount_over / amount_under / amount_meets with a Palette color). Every
visit now arrives with that judgment precomputed at ⟦inspections.scale.verdict⟧ with
values ⟦within|over|under|meets|no_order⟧.
1. Delete the comparison logic. Keep and reuse the presentation mapping: verdict value →
   Loc chrome key (amount_within/over/under/meets, no-order key) → Palette color
   (within/meets = GREEN, over/under = RED). Missing/empty verdict → the existing
   no-order fallback, never a crash.
2. Change nothing else in the file: no renames, no signal changes, no layout edits.
3. Run the game via the coordinator's mono exe; open a visitor with a standing order;
   report which Loc key rendered.
If ANYTHING does not match this description — line numbers, field name, enum values —
STOP and return to the coordinator. Do not adapt.
```

## WP-G — documentation sync · **Opus** (goals + constraints)

*Why Opus: compressing a finished refactor into this repo's dense, literate spec voice
without drift between doc and reality — judgment work, cheap to verify, wrong to
under-model.*

OWNED: `adventuring-guild-teller/morning-queue/MORNING-QUEUE.md` · `CONTENT-BANKS.md`
(pointers) · `plans/PLAN-morning-queue-tiers.md` (ticks/status) ·
`plans/PLAN-adventuring-guild-teller.md` (tick) · `DEV-LOG.md` (the pre-commit entry).
GATE G5: coordinator reads the new MORNING-QUEUE.md top-to-bottom against the actual
tree — every path, toggle, command, and invariant it states must be true; then the final
commit.

```text
[hard-laws block]
TASK: Make the docs match the refactored reality, in this repo's existing voice (read
MORNING-QUEUE.md as it stands first — match its density and tone; it is a nav spec for
agents, not marketing).
1. MORNING-QUEUE.md: engine line drops "GDScript-only" and states the three tiers
   (data/ JSON · scripts/ GDScript · core/ C#, cs/CoreBridge.cs as the only crossing);
   "Run it" gains the dotnet build step and the mono-only constraint; the architecture
   diagram gains the core/ box and the two bridge calls; Invariants: replace the
   GDScript-only bullet with the MQT.D1 ruling as worded in
   plans/PLAN-morning-queue-tiers.md (Web deferred; engine-free core keeps the pre-bake
   escape hatch; cite the verification links from the plan); refresh the class_name
   gotcha (ShiftGenerator is gone; Loc/Palette/ThemeFactory remain); add a short
   "Code map" section for core/ (the deliverable-internal code-doc ruling from the
   plan — note CodeDocs/ stays not-stood-up).
2. CONTENT-BANKS.md: §4 recipe pointers now name the C# composer files; note the
   rebaseline (streams changed once at MQT.4, golden-pinned since).
3. Tick every completed MQT.n in the tiers plan, set its Status line to executed +
   date; tick the tier-refactor line in PLAN-adventuring-guild-teller.md.
4. DEV-LOG.md: one entry, newest-on-top, absolute date — the WHY of the run: rulings
   applied, the rebaseline, anything the generator port flagged as a latent oddity,
   what would surprise the next person. Pull from the coordinator's gate notes.
Do not invent history: if a gate note contradicts a doc claim you want to make, ask the
coordinator rather than smoothing it over.
```

---

## 5 · Coordinator escalation & failure protocol

- A subagent reporting STOP, a gate failing twice, a frozen contract that seems wrong,
  or any temptation to edit the settled architecture → halt the run, leave the tree in
  the last green committed state, and report to Panda: which WP, the diff, the outputs,
  your one-paragraph read. `git tag mqt-baseline` and per-gate commits are your rewind
  points.
- Subagent tier map if the Agent tool wants full ids: haiku→`claude-haiku-4-5`,
  sonnet→`claude-sonnet-5`, opus→`claude-opus-4-8`, fable→`claude-fable-5`.
- Concurrency: wave 1 only (WP-A ∥ WP-B ∥ WP-C, disjoint files). Everything after runs
  serially — WP-D and WP-E both own DeckLoader.gd and Core.
- You own all git actions and all plan/DEV-LOG bookkeeping. Subagents write code and
  report; you verify, tick, log, commit. Never push.

# MORNING-QUEUE.md — wireframe + agent-allocation spec

The **Morning Queue** is the playable paper-prototype named by the Adventuring Guild
Teller musing's ghost card (`../index.html`): *one desk shift* — a stack of visitors, a
reference booklet, stamps, and a score — the fastest falsifier for the flow claim
(`AGT.5` / `AGR.1`). This file is the **agent-nav spec** for the sub-project: the frozen
interfaces and who builds what. Not published (the site build copies only top-level
`*.html`; this whole folder is invisible to Pages — see `../../musing-tech-notes.md`).

- **Engine:** Godot **4.6** (`.mono` install, but this project is **GDScript-only** so the
  Web-export path stays open — .NET cannot Web-export; GDScript can).
- **Renderer:** `gl_compatibility` (the Web/embed-safe renderer). Chosen so the eventual
  local-site embed (a `<canvas>`/iframe from a Web export) works. **Not** for GitHub Pages
  — Godot 4 Web needs COOP/COEP headers Pages can't set; the embed target is the *local*
  preview only, until that changes.
- **Run it:** open `project.godot` in Godot 4.6, or `run_project` via the godot MCP. Main
  scene is `scenes/Main.tscn`.
- **Status (2026-07-15, rev 3):** all usability fixes applied and verified. **12/12
  DeskFeatureHarness assertions pass.** Full heuristic study (all 12 reference pages) in
  session artifact. All desk-tile Priority 1–4 items resolved: ScrollContainer cap (170px),
  per-tile × dismiss, first-use hint, hover tint on posting rows, foldout header contrast
  (GROUND bg / LINE2 border), tile label 12px, G/S keyboard shortcuts. The examine/weigh
  loop is live: `ReferencePanel.set_inspection_target(v)` refills the Glass / Scale tool
  pages per visitor (see INSPECTION-TOOLS.md). **Desk tiles ship:** clicking a
  tool tab or a Quest Board posting row emits `ReferencePanel.tile_requested`, which Main
  wires to `VisitorCard.add_tile` — placing a named reference tile on the main desk below
  the claim doc; tiles clear on visitor change. **Quest Board foldouts ship:** postings are
  grouped by `type` in collapsible sections (bounty / survey / retrieval / collection /
  rescue / standing_order); all 7 previously-untyped apparition/beast postings now carry
  `"type": "bounty"` in `references.json`. **A week of content + a procedural visit
  generator now ship** (`CONTENT-BANKS.md`): broadened banks + townee/adventurer directories
  + the `dues` axis + `ShiftGenerator.generate_shift(day)` (day 0 = curated, day > 0 =
  generated; self-check `7 days, 97 visits, 0 problems`). AGT.5 is mechanically settled
  (binary). **Rev-3 additions:** (a) curated visitor #17 `nessa-broom` — an amount-fail
  `item_check` (moonwort at 6 drams, over the apothecary's 2–4 dram cap; Glass passes /
  Scale condemns; `failure.axis: amount`); (b) richer Glass readings for three thin
  card/seal visitors (#2 hulbr-odd-eye, #4 doss-yellowknife, #5 ivy-threnody); (c) **floor
  beat** — after shift_complete the booth shows THE FLOOR: all owing townees are listed with
  an "Accept Xg" button; clicking clears their dues in `Deck.townees` (runtime only, JSON
  unchanged) so the next generated shift sees them as current. `Deck.pay_dues(id)` is the
  new method; new `Loc` chrome keys: `floor_head / floor_dues_intro / floor_no_dues /
  floor_accept_btn / floor_paid`.

---

## Data — the files that are the actual game

In `data/`, loaded and validated by the `Deck` autoload. Prototype canon; correct by
`id`. **Enemies are wild-magic apparitions / mana beasts by design** — nothing that
depopulates a living population. `visitors.json` (curated day 0) + `references.json` are
the originals below; the week-of-shifts banks (`townees.json`, `adventurers.json`,
`generation.json`) and the procedural generator are described in *Generator + directories +
dues* further down.

> **Inspection tools + standing-order limits + the binary desk:** the examine/weigh loop
> (the Glass and Scale reading `visitor.inspections`), the standing-order limit schema
> (`accept`/`total`), the additive `ReferencePanel.set_inspection_target(visitor)` method,
> and the `STRICT_BINARY` plan are all specified in **`INSPECTION-TOOLS.md`** — read it
> before touching the panel, verdict, or data.

> **Week-of-shifts expansion (banks + directories + generator + dues):** the reference
> banks broadened to a week's breadth, the two directories, the `dues` failure axis, and
> the procedural visit generator are specified in **`CONTENT-BANKS.md`** — read it before
> touching the generator, the banks, or the directory tabs. The "Generator + directories +
> dues" section below is the built architecture; `CONTENT-BANKS.md` is the design contract.

### `visitors.json` — the queue (16, in `order`)
Each visitor:

| field | meaning |
|---|---|
| `id`, `order`, `name` | identity + queue position |
| `affiliation` | `townee` \| `adventure` |
| `profession` | flavor job title |
| `task_type` | `item_check` \| `rank_gate` \| `quest_file` \| `completion_claim` \| `rank_up` \| `roster_change` \| `dungeon_drop` |
| `claim` | `{ summary, asserts{} }` — what they say at the counter |
| `truth` | `{ valid, stamp, binary, failure }` — the right answer + why (`failure.axis` ∈ identity/rank/unverifiable/claimant/authenticity/paperwork/duplicate/fieldability/season/reach). Some carry `flag_floor`, `roster_write`, or `quote`. |
| `checks[]` | the verification steps — each `{ consult, entry, compare, against, result }` resolves against `references.json`. This drives the ReferencePanel *and* is the source of the player-story. |
| `player_story`, `notes` | prose the design doc quotes; author intent |

The 16 span every `task_type` and eight distinct failure axes. Two intentional half-fails
(`odile-vantry` = `conditional`, `ivy-threnody` = `hold`) exist to pressure the
"is the desk strictly binary?" question (`AGT.5`); the `Session.STRICT_BINARY` dial
collapses them to `reject` for the two-stamp feel.

### `references.json` — the rulebook the desk checks against
Truth tables, one per `checks[].consult` target: `book` (item tells), `postings`
(gates + standing orders), `rank_ledger` (cards on file), `rankup_thresholds`, `archive`
(sealed completions), `cipher_table` (chapter transfer-card seals), `drop_table`
(dungeon drops: floor + season + bounty), `season` (current turn of year), `payout`
(the quote formula), `roster` (active parties — read by fieldability, reach, and
welcome/farewell writes). Every `entry` a visitor names resolves here; the Deck
validator guards it.

**Interlock worth preserving:** the roster has no cleric/water ward → `sister-coll`
unfieldable; deepest reach is Floor 6 → `ostler-bram`'s Floor-7 drop unreachable;
enrolling `perrick-vane` (water ward) would flip the shrine fieldable. Enrol-then-field
is the seam between desk and Floor.

---

## Generator + directories + dues — the week-of-shifts architecture

The desk now runs a **week**: `day 0` is the curated tutorial shift (`visitors.json`,
unchanged); `day > 0` is composed procedurally from the banks. Design contract:
`CONTENT-BANKS.md`. Built shape:

**Five banks** (all data; `references.json` extended additively, two new files):

- `references.json` — broadened to a week's breadth: **24 Book items** across 5 categories
  (`herb`/`beast_part`/`reagent`/`mineral`/`relic`), each with an authored `glass`
  examined-description (+ optional `forgery_glass`); **20 postings**, **6 chapter ciphers**
  (each with `glass`), **10 drops**, an enlarged archive (per-adventurer logbooks + 6
  completion tokens), one added earth-warded roster party.
- `data/townees.json` → **Townee Directory** (16 townees: `dues`/`owed`/`owns`/`blurb`).
- `data/adventurers.json` → **Adventurer Directory** (16 adventurers:
  `rank`/`dues`/`chapter`/`wards`/`logbook`/`blurb`) — the rank_gate / rank_up /
  roster_change actor pool.
- `data/generation.json` → generator config (visit-count range, `task_weights`,
  `invalid_rate` [+ per-day ramp], `failure_axis_weights`, per-task composition contract,
  walk-in `name_pools`, `season_schedule`).

**The generator — `scripts/gen/ShiftGenerator.gd`** (a `class_name` static utility, called
`ShiftGenerator.generate_shift(day)`; not an autoload, so no autoload-order coupling with
`Deck` — but it *is* a new `class_name`, so the global-class cache must be regenerated once
after adding it, same gotcha as `Loc`). Deterministic: `rng.seed = day`, so a week is 7
reproducible shifts. It reads the banks off `Deck` and **writes nothing**. Per visit it
picks a `task_type` (weighted) → an actor (directory draw, sampled without replacement per
shift, or a `name_pools` walk-in) → valid-vs-invalid (`invalid_rate`) → a failure axis
(weighted over the axes that task *and the available bank material* admit) → composes
`claim` / `checks[]` / `truth` / `inspections`, emitting the **exact `visitors.json`
schema** so the card/panel/verdict/scoreboard consume generated visits unchanged. The
**Glass reading is derived** per subject kind (`book_item` genuine/confusable/forgery ·
`transfer_seal` cipher `glass` · `completion_token`/`logbook` seal reading · `rank_card` /
`filing` decoy); the **Scale amount** is sampled inside / outside the claimed order's limit.

**The `dues` axis (+ `amount`).** `dues` and `amount` extend the `truth.failure.axis` enum.
A townee whose `dues` is `owing` cannot post → `quest_file` and `dungeon_drop` reject on
`dues` (short-circuiting before fieldability / the quote pipeline); an adventurer `owing`
guild dues fails `rank_gate` / `rank_up`, and `roster_change` admits a secondary `dues`
fail on a stale card-stamp. A dues-fail visit emits a `{consult: "townee_directory" |
"adventurer_directory", entry: <id>, …, result: "owing — <owed>g outstanding"}` check so
the panel deep-links to the row. `amount` is the item_check Scale fail (over/under a limit).

**Deck integration (`DeckLoader.gd`, additive).** Loads the three new banks; injects the two
directories into `references` under `townee_directory` / `adventurer_directory` (clean
`_tab` + id-row tables) so the ReferencePanel renders them as ordinary tabs and
`consult: "…_directory"` resolves like any table; exposes `Deck.townees` /
`Deck.adventurers` / `Deck.generation` / `Deck.day`; adds `Deck.load_day(d)` for a later
shift-select hub. `day == 0` loads `visitors.json`; `day > 0` calls the generator. The
validator gained light bank checks (dues enums; `owns`/`chapter`/`archive_id` resolution;
generation knob domains) and holds generated shifts to the **same** inspections +
required-field contract as the curated one. A debug-only boot self-check
(`_selfcheck_generated`) generates + validates all 7 days.

**ReferencePanel.** The two directory tabs render through the existing generic path — **no
frozen signature changed**; `CONSULT_TO_TAB` gained the two directory keys and `Loc` gained
`ref_tab.townee_directory` / `ref_tab.adventurer_directory` (+ `failure_axis.dues|amount`,
`dues_current|owing`). The adventurer directory *is* the expanded rank ledger.

**Day flow in the UI (`Main.gd`).** `Main` surfaces the week the data layer already
supported: a top-of-booth day strip (day label + a **Skip-tutorial** button shown only on
day 0) and a **Next-Day** button that appears under the shift ledger and walks `day →
day+1` up to `LAST_DAY = 7`, then locks as "the week is done." `_go_to_day(d)` reloads via
`Deck.load_day(d)` and re-opens the desk with `Session.start()` (banks are unchanged across
days, so only the queue reloads). This is Main-owned plumbing — no frozen component
interface changed; new strings are `Loc` chrome (`day_label*`, `skip_tutorial`, `next_day`,
`week_done`).

**Verified (both days, zero errors).** `--import`, then via the godot MCP: curated day 0
steps all 16 → 16/16; generated day 1 steps all 14 coherent visits → 14/14; the boot
self-check reports `7 days, 97 visits, 0 problems`. Ships defaulting to day 0 with
`DevHarness.enabled=false`.

---

## Architecture — the frozen interfaces

Two autoloads are the spine; four component scenes hang off them; `Main` is pure plumbing.
**These signatures are FROZEN.** Sub-agents implement *bodies*, never rename signals or
methods. Each component lives in its own `.tscn` + `.gd` pair so agents never share a file.

```
Deck (autoload, DeckLoader.gd)      Session (autoload, GameState.gd)
  .visitors  .references              .index  .score  .verdict_log
  .count() .get_visitor(i)            .start() .current() .submit(stamp) .advance()
  .load_day(d)  .pay_dues(id)         signals: visitor_changed, verdict_recorded,
  .ok  .load_errors                             shift_complete   ·  const STRICT_BINARY
  signal: loaded(ok)

                    Main.tscn / Main.gd  (integrator — builds layout, wires signals)
   ┌───────────────────────────────┬───────────────────────────────┐
   │ booth column                  │ reference column              │
   │  VisitorCard   show_visitor(v)│  ReferencePanel               │
   │                add_tile(...)  │    set_references(refs)        │
   │                clear_tiles()  │    focus(consult, entry)      │
   │  VerdictBar    ->stamp_chosen │    set_inspection_target(v)   │
   │  Scoreboard    set_progress() │    ->tile_requested(...)      │
   │                show_summary() │                               │
   └───────────────────────────────┴───────────────────────────────┘
```

Component contracts (method calls IN from Main, signals OUT to Main):

| Scene / script | Main calls | emits |
|---|---|---|
| `VisitorCard` | `show_visitor(v: Dictionary)`, `add_tile(tile_id, title, body, tint)` *(additive — places a reference tile below the claim)*, `clear_tiles()` *(additive — clears on visitor change)* | `papers_examined()` (optional) |
| `ReferencePanel` | `set_references(refs)`, `focus(consult, entry)`, `set_inspection_target(v)` *(additive — refills the Glass/Scale tool pages per visitor; INSPECTION-TOOLS.md §6)* | `tile_requested(tile_id, title, body, tint)` *(additive — fired when a tool tab or Quest Board row is clicked; Main wires to `add_tile`; INSPECTION-TOOLS.md §6)*  |
| `VerdictBar` | `set_enabled(on: bool)` | `stamp_chosen(stamp: String)` |
| `Scoreboard` | `set_progress(index, total, score)`, `show_summary(summary)` | — |

`stamp` ∈ `approve`\|`reject`\|`hold`\|`conditional`. `Main` maps `stamp_chosen` →
`Session.submit` → `Session.advance`; `Session.visitor_changed` → `clear_tiles` +
`show_visitor` + `set_progress` + `set_enabled(true)`; `shift_complete` → `show_summary`.

---

## Strings & localization — the `Loc` layer

Every user-facing string is centralized so a second locale is a *data* addition, not a
code edit. There are two layers, and the split is the rule the whole codebase obeys:

- **(a) Translatable — `scripts/loc.gd` (`class_name Loc`).** UI chrome ("AT THE
  COUNTER", "REFERENCE DESK", "SHIFT COMPLETE", the progress/summary templates, "no card
  on file", …) *and* the finite enum/slug vocabulary (affiliations, task types, verdict
  stamps, reference-table tab titles). All keyed inside `_LOCALES["en"]` (sub-maps
  `chrome` / `vocab` / `overrides`). Called statically like Palette: `Loc.t("key")`,
  `Loc.affiliation(a)`, `Loc.task_type(tt)`, `Loc.stamp_button(s)`, `Loc.stamp_past(s)`,
  `Loc.ref_tab(key)`.
- **(b) Content — `data/*.json`.** Visitor names, claim summaries, `player_story`,
  failure reasons. Procedural prose; it stays in the data bundle and is never hardcoded
  into scripts and never moved into `Loc`.

**Identifier vs display (the anti-leak rule).** JSON keys, enum values and slug ids
(`item_check`, `cistern-wisp-swarm`, `rank_order`) are *identifiers*: used for logic +
lookup, **never shown raw**. `Loc.humanize(slug)` is the **one** slug→Title-Case
implementation — the old per-component `_titleize` / `_pretty_value` /
`_affiliation_label` / `_stamp_name` / `_humanize` / `_tab_title` copies are gone; every
component routes through `Loc`. Never mutate an identifier to change its display: a
proper noun the generic humanizer gets wrong (hyphenated names, acronyms) gets an entry
in `_LOCALES["en"]["overrides"]`, keyed by the raw id. `checks[].entry` still resolves
against `references.json` by exact string, and the ReferencePanel's `rows`/`focus()`
lookups keep the raw ids — only the *rendered* title/label is humanized.

**Reference tab titles** come from `Loc.ref_tab(key)` (uniform: every top-level table,
including the array-valued `rank_order`, gets a friendly title; a missing key still
humanizes, never leaks the raw key). The long descriptive `_tab` string in the JSON stays
as the page caption (content).

**Adding a locale** = add one dictionary to `_LOCALES` (its own `chrome`/`vocab`/
`overrides`) and set `Loc.locale`. English is the only shipped locale; the humanizer is
the English auto-fallback, so a half-filled locale degrades to Title-Case, never to a raw
id. (Godot's native `tr()`+CSV was the alternative; a code-built UI with a dynamic slug
humanizer is cleaner as one `Loc` module.)

## Dev tooling — DevHarness & viewport captures

`scripts/dev/DevHarness.gd` (a Node on `scenes/Main.tscn`) is a **validation aid, not
shipped logic** — it exists so the desk can be checked without fighting the OS
screenshotter:

- **Capture:** writes a PNG of the whole viewport to `.captures/` (= `<project>/.captures/`,
  carries its own `.gdignore` so Godot never imports the shots; `.gitignore`d). Press **F12**
  any time for a manual shot; an auto-run also shoots one frame per visitor
  (`NN_<id>_<stamp>.png`) plus `99_summary.png`.
- **Auto-step:** when `Enabled`, it walks the whole shift on a `step_delay` timer, feeding
  each visitor a stamp by invoking the **same handler a real stamp-press fires**
  (`Main._on_stamp_chosen`) — so it exercises the true submit→advance path, not a shortcut.
  Default stamps = each visitor's correct verdict (respecting `STRICT_BINARY`); set the
  `actions` array to script a specific case.
- **Toggle auto-run/manual play:** edit `scenes/Main.tscn` on the `DevHarness` node and flip
  the line `enabled = false` (`false` = manual play + F12 only, `true` = auto-step + capture
  pass on startup). You can also untick/tick **Enabled** in the Inspector.

The `Session` verdict-log entry gained a `name` field (`{id, name, chosen, correct,
right}`) so the Scoreboard ledger shows the **authored** name ("Wrenna Sixpence"), not the
id-humanized one ("Wren Sixpence") — additive, no signature change.

## Sub-agent allocation — the build-out

Each agent owns a disjoint file set and honors the frozen contract above → no merge
conflicts, fully parallel. The spine (A, B) and integrator are mostly done; the value is
in C–G.

| # | Agent | Owns (only these files) | Job | Depends on |
|---|---|---|---|---|
| A | `data` | `data/*.json`, `scripts/autoload/DeckLoader.gd` | Extend fields the UI needs; keep the validator green | — |
| B | `session` | `scripts/autoload/GameState.gd` | Feedback timing, `STRICT_BINARY` feel, end-summary payload | A |
| C | `card` | `scenes/VisitorCard.tscn`, `scripts/components/VisitorCard.gd` | Present the visitor + their papers as documents on the counter | contract |
| D | `reference` | `scenes/ReferencePanel.tscn`, `scripts/components/ReferencePanel.gd` | The tabbed lookup surface from `references.json`; fast + legible = the flow | contract |
| E | `verdict` | `scenes/VerdictBar.tscn`, `scripts/components/VerdictBar.gd` | The stamp row + the satisfying stamp thunk | contract |
| F | `score` | `scenes/Scoreboard.tscn`, `scripts/components/Scoreboard.gd` | Mid-shift progress + the day's-end ledger summary | contract |
| G | `theme` | `theme/queue_theme.tres` (new), one `[gui]` line in `project.godot` | Parchment/approval-green palette as a shared Theme; components read it, never hardcode | contract |
| — | `Main` (integrator) | `scenes/Main.tscn`, `scripts/Main.gd` | Assemble + wire; run last / kept by the orchestrator | all |

**Palette (from the musing tokens):** parchment ground `#f1ecdc`, approval-green
`#2f6b4f`, wax-red `#9c3122`, brass `#8a6d1f`; both light and dark. Keep it consistent
with `../pitch.html`.

**Prime directive for every sub-agent (verbatim, per repo Rule 1):** *No inline
interpreter calls — no `python -c`, `node -e`, etc. If a helper is needed, write a file
under `scrap_scripts/<lang>/` anchored to the repo root and run that. Shell one-liners are
fine.* GDScript authored **inside** this Godot project is the deliverable, not a scrap
script — that rule is about ad-hoc interpreter invocations on the CLI, which stay banned.

---

## Invariants

- GDScript only; `gl_compatibility` renderer; both stay for the Web-embed path.
- One `.tscn` + one `.gd` per component; `Main` owns composition. Don't cross file lines.
- The Deck validator must stay green — every `checks[].entry` resolves in `references.json`.
- `.godot/` and `export/` are gitignored (`.gitignore`); never commit the import cache.
- This folder never reaches a public surface via the musing build — if that changes (a Web
  export embedded in the local site), gate it under Rule 6 and note it here.
- **`class_name` gotcha:** globals like `Palette` / `ThemeFactory` / `Loc` live in
  `.godot/global_script_class_cache.cfg`, which only the **editor** regenerates. Running the
  project via the MCP after adding a new `class_name` script fails with `Identifier "X" not
  declared`. Fix: regenerate the cache once with
  `godot --headless --path . --import` (or open the editor). The cache is gitignored, so a
  fresh clone needs this too before first run.

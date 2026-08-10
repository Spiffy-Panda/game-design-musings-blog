# CROKINOLE.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **Crokinole** musing (Rule 2). This is an
**HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored hub `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/crokinole/` (pattern documented in `../musing-tech-notes.md`).
This file and `README.md` stay internal.

**Mnemonic (Rule 8):** **`CRK`** — `CRK.1`–`CRK.6`, the six design claims on the hub
`index.html` (anchors `#crk-1` … `#crk-6`). **Append-only IDs**: new claims take new
numbers, nothing renumbers. Cite them from other pages, plans, and reviews.

## File map

| File | Role |
|------|------|
| `index.html` | Hub — the musing prose (published entry point): the bet (the board as a compiled ruleset), what the rebuild made explicit (the derived peg phase, the contact rule as the balancing mechanism, difficulty with nowhere to hide), claims `CRK.1`–`CRK.6`, card → The Board, ghost card → "The Curves" statistics candidate, and the approximations note (centre-based ring value, 12-disc singles). |
| `layouts.json` | **AUTHORED — the single source of truth for board geometry.** Four boards: `lane` (48×24, long sides rail / short ends ditch, two staggered fences), `courtyard` (32×32, every edge ditch, diamond `poly` zones, a post ring on the cardinals and diagonals), `gauntlet` (56×20, `enclosed: true` — every edge rail, a chevron per shooter), `terrace` (36×28, sides rail / ends ditch, a full-width crown stripe between two interleaved colonnades). Zone ladder crown 3 / mid 2 / outer 1 / apron 0. Copied verbatim into `site/musings/crokinole/`. |
| `board.html` | The simulator — one self-contained page: **the picket board**, four switchable layouts read from the generated `board-layouts` block (geometry authored in `layouts.json`), a disc-independent `BOARD`/physics constants block (1¼″ discs, 3/8″ posts, 1/960 s substeps, disc–disc restitution 0.95, disc–peg 0.60, rail 0.55), the twelve-disc `ROSTER` with its four effect hooks, a fixed-timestep rigid-disc solver, the rules (placement along a launch segment, the Engage-rule foul, the raised convex-polygon Crown with a per-face climb toll, rails vs ditch as layout data, the **board-full rule**, differential round scoring), drag-slingshot + keyboard input, an accessible text mirror, a seeded mulberry32 hand, the runtime layouts loader (`validateLayoutSpec` + `auditLayout`), and the opponent: two-stage candidate search over (disc, placement, angle, power) run through the *real* solver, scored by `AI_TERMS`, reported as Δ against leaving the board alone. |
| `emblem.svg` | Landing-row emblem: the board as a diagram — concentric rings, eight pegs on the 15 circle, quadrant dividers, the hole, and one disc mid-flight down the open-twenty lane; colors via `--m-*` tokens (`bg`, `ink`, `muted`, `line`, `accent`, `accent2`). |
| `README.md` | Human doc: what it is, how to run it, the controls, the design reading, what's approximated. |
| `build-musing.py` | Copies `*.html` → `site/musings/<slug>/` (never copies `.md`), **regenerating `board.html`'s `board-layouts` block from `layouts.json`** and copying `layouts.json` alongside. `--sync` also rewrites the repo copy's block. Non-zero exit if `layouts.json` is missing or unparseable. |

## Invariants

- Every page is self-contained — inline CSS/JS, no external assets, both themes via
  `prefers-color-scheme`. Each hand-authors the **site-wide breadcrumb** (`nav.crumbs`,
  rooted at the portfolio; see `../musing-tech-notes.md` "Navigation: the breadcrumb
  standard") in this musing's own tokens (`--accent` links, `--line2` separators,
  `--ink3` current). No `backdrop-filter` and no `background-attachment: fixed` — both
  stall headless capture.
- **`layouts.json` is authored; the `<script type="application/json" id="board-layouts">`
  block in `board.html` is GENERATED from it.** Never hand-edit the block, and never add a
  second copy of board geometry to the page. It is inlined rather than fetched because the
  page must open from `file://`, where fetching a sibling file is blocked. After editing
  `layouts.json` run `python crokinole/build-musing.py --sync`; a plain build warns loudly if
  the repo copy is stale and always publishes from `layouts.json`.
- **The layout contract** (schema, the `rect` vs `poly` zone kinds, the convex-polygon Crown
  plateau with one `lipV2` per face, `enclosed`, gates that may span picket groups) lives in
  the section-2 comment of `board.html` and is enforced by `validateLayoutSpec` +
  `auditLayout`. The runtime loader on the page runs both, so a pasted board gets exactly the
  checks the shipped four got.
- **`board.html` is the behaviour contract** for everything that is not geometry: the `BOARD` dimensions, the rules the
  simulator enforces, the physics constants, and the `AI_TERMS` scoring weights live
  there and nowhere else. The hub's prose quotes them (peg phase 22.5°, tremor 1.2°
  default up to 4°, ~160 candidate search width, 12 discs a side, centre-based ring
  value) — **keep the hub in sync with `board.html`, never fork the numbers.** New
  pages cite or amend those values.
- The **peg phase** is derived from the geometry, not quoted from a rule source. Both
  pages say so plainly; keep that candour if either is edited.
- **No network, no telemetry, no persistence**: everything is computed in-tab and
  nothing is stored. The hub's footer promises it.
- Registered in `../MUSING-CONFIG.json` (row + sublink). Everything in `*.html` deploys
  to Pages — the Rule 6 gate applies to every edit.
- Cross-musing links (if any come) use the shape `../<slug>/<page>.html`; folder name ==
  slug (lowercase) so repo and site resolve identically.

## History

Authored 2026-08-09 on Panda's brief: "a crokinole simulator, JavaScript, framed on the
website". `board.html` came first; this hub was written against the finished simulator so
the prose describes what it actually does. Decisions: `../DEV-LOG.md`.

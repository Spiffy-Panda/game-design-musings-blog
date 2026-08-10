# crokinole/ — the board is the ruleset

A 150-year-old dexterity game taken apart by rebuilding it. Crokinole keeps its entire
rulebook inscribed on one piece of plywood — the rings *are* the scoring table, the ditch
*is* the out-of-bounds clause — so simulating it forces every implicit rule to become an
explicit number. The design object under study is that translation: what a physical game
turns out to have been hiding. Authored 2026-08-09.

## How to run it

Open **`index.html`** in any browser — it's the hub with the musing prose (claims
`CRK.1`–`CRK.6`) and the link to the simulator. For the full site (landing page,
breadcrumb up to the musings index), run the preview server from the repo root:

```
python utils/python/serve_site.py
```

Both pages are single dependency-free `.html` files with light + dark themes; no build
step, no network. **`board.html`** is the simulator and opens straight from disk.

## How to play

- **Mouse / touch:** drag back from your disc and release — slingshot. Click elsewhere on
  your quadrant arc to slide the disc along the starting line.
- **Keyboard** (click the board first): `←` `→` aim · `↑` `↓` power · `A` `D` slide along
  the line · `Space` shoot · hold `Shift` for fine steps.
- **Controls panel:** *Hand tremor* is the machine's Gaussian release noise (0–4°, default
  1.2°) — the search is identical at every setting, only the hand changes. *Search width*
  is how many candidate shots it evaluates (40–320, default 160). *Seed* + *New match*
  restarts deterministically; there's also a checkbox to give yourself a shaky hand.
- **The rules panel** at the bottom of the board page lists every dimension, every rule
  enforced, the physics constants, and the approximations.

| Page | File | What it is |
|------|------|------------|
| The musing | `index.html` | The bet (the board as a compiled ruleset), what the rebuild made explicit, six claims `CRK.1`–`CRK.6`. |
| The simulator | `board.html` | The picket board: four switchable layouts, 12 named discs a side, contact rule enforced, seeded deterministic physics, and an opponent that shows its search and says which scoring term won. |
| The board data | `layouts.json` | **Authored.** The geometry of all four boards — the single source of truth. |
| Preserved | `regulation.html` | The original round regulation board, unmodified. |
| candidate | — | "The Curves" — drive the solver headlessly and plot open-twenty hit rate against tremor σ, takeout success by ring. |

## Which file is authored, and which is generated

- **`layouts.json` is authored.** Every board dimension lives here and nowhere else.
- **The `<script type="application/json" id="board-layouts">` block inside `board.html` is
  GENERATED from it.** Do not hand-edit that block.
- `build-musing.py` regenerates the block on every build and copies `layouts.json` next to the
  published page. It exits non-zero if `layouts.json` is missing or unparseable.
- After editing `layouts.json`, run **`python crokinole/build-musing.py --sync`** so the repo copy
  of `board.html` (which has to open straight from `file://`) carries the new geometry too. A plain
  build never writes to the source tree; it prints a loud `warning: … is STALE` instead, and the
  *published* page is always built from `layouts.json` regardless.

The block is inlined rather than fetched because the page must open from `file://`, where a `fetch`
of a sibling file is blocked by the same-origin rule.

## The four boards

| Board | Surface | Edges | What it changes |
|-------|---------|-------|-----------------|
| **Lane** | 48 × 24 | long sides rail, short ends ditch | Shot down the long axis. Two fences flank the crown, staggered so no straight line passes through both — you commit to one side. |
| **Courtyard** | 32 × 32 | all four ditch | Four seats, diamond (polygon) zones, and a ring of posts on the cardinals and diagonals: the straight-ahead shot at the crown is shut. Angle is the skill. |
| **Gauntlet** | 56 × 20 | all four rail | Fully enclosed — nothing is ever lost over the side, so the board only fills. A chevron faces each shooter. Needs the **board-full rule**: the round ends the moment the side to move has no legal placement, and scores as it stands. |
| **Terrace** | 36 × 28 | sides rail, shooting ends ditch | The crown is a full-width stripe rail to rail, guarded by two interleaved colonnades. A shot is a depth judgement, not an aim. |

Zones score **crown 3 · mid 2 · outer 1 · apron 0**, plus a sweep bonus for holding the crown alone.

## Trying a fifth board without a rebuild

The **Load a board** panel on `board.html` accepts a pasted or file-picked layouts JSON, validates
it against the same schema and the same structural audit the shipped four pass, and reports errors
in the panel rather than throwing. *Copy the current board in* seeds the box with the board in play,
which is the easiest starting point.

## Game-design reading

Crokinole is a rare case of a game whose rules are a description of an object, so the
interesting work is all in what *cannot* be read off the plywood. Three things surfaced
during the build. The peg **phase** — 22.5° off each quadrant line, two pegs straddling
its centre — is derived from geometry rather than quoted from any rule source, and it is
the sole reason an open twenty exists. The **contact rule** is not a restriction bolted on
top of the scoring but the same discs counted twice: what you score with is what your
opponent is obliged to shoot at. And the **hand** is the only randomiser, which means
digitising the game is entirely a question of choosing a noise model — here a Gaussian
tremor on release, which is honest and admittedly flat.

## What's approximated

- **Ring value is decided by the disc's centre.** The tournament rule is line-based (a
  disc touching a line takes the lower value), so this board is uniformly, slightly
  generous at every boundary.
- **Singles is 12 discs a side.** The general rule sets say 12; World Crokinole
  Championship singles plays 8.
- **The hole swallows a little more than geometry allows** — a sink-on-rest test within
  5/16″ of centre, standing in for the real lip that tips a leaning disc in.
- **No spin, no bevel, no thickness.** Discs are flat circles of equal mass.

## What this folder is — and isn't

- **A registered, published musing.** Listed in `../MUSING-CONFIG.json`; `build-musing.py` copies
  every `*.html` to `site/musings/crokinole/` (verbatim except `board.html`, whose board-layouts
  block is regenerated from `layouts.json`) and copies `layouts.json` alongside them.
  HTML-first: no `MUSING.md`; `CROKINOLE.md` is the Rule-2 agent-nav spec.
- **Published, so Rule 6/7 apply.** No third-party material: crokinole is a public-domain
  traditional game, the dimensions are facts, and the rules are restated in the page's own
  words. Nothing leaves the tab — no network calls, no telemetry, no storage.
- **Mnemonic (Rule 8):** `CRK`, declared canonically in `CROKINOLE.md`.
- **`board.html` is the behaviour contract.** Its `BOARD` constants and `AI_TERMS` weights
  are the single source of truth; the hub's prose quotes them and must stay in sync.

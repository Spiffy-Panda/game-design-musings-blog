# PLAN — crokinole (Crokinole)

**Status:** shipped v2 (2026-08-10) — hub + the picket board on four chosen layouts, geometry
authored in `layouts.json`. `regulation.html` preserves v0 unmodified.
**Folder:** `../crokinole/` (HTML-first musing; folder == slug). Nav spec: `../crokinole/CROKINOLE.md` (mnemonic `CRK`).

> **Desync noted 2026-08-10:** the v1 rebuild (round regulation board → rectangular *picket* board
> with a `LAYOUTS` table, the twelve-disc roster and its effect hooks, and the two-stage AI search)
> was never written into this plan; the v0 section below still describes the round board, which now
> lives only in `regulation.html`. Treat "Shipped (2026-08-09) — v0" as the history of that file,
> not of `board.html`.

## Shipped (2026-08-10) — v2: the four chosen boards

- [x] The four placeholder layouts dev 1 invented (`lane-24x40`, `arena-30x30`,
      `sidewinder-36x22`, `alley-18x44`) replaced by the four Panda chose: **lane** (48×24),
      **courtyard** (32×32), **gauntlet** (56×20), **terrace** (36×28). Dev 1's data contract,
      tuning fields and explicit peg lists kept; only the geometry and the identities changed.
- [x] **Geometry authored in `../crokinole/layouts.json`**, inlined into `board.html`'s
      `<script type="application/json" id="board-layouts">` block by `build-musing.py`
      (`--sync` for the repo copy), with `layouts.json` copied into the site output. Build exits
      non-zero on a missing or unparseable file. Pattern generalised into `musing-tech-notes.md`.
- [x] **A `poly` zone kind**, in the contract, the scorer, the renderer and the AI's zone
      reasoning — zones and the Crown plateau both compile to outward half-planes, and the Crown's
      climb toll is taken per *face* rather than per axis so the plateau can be a diamond.
- [x] **The board-full rule** for the enclosed gauntlet: the round ends the moment the side to move
      has no legal placement, and scores as it stands. `enclosed: true` is audited, not trusted.
- [x] Board selector with a one-line blurb per board; **runtime layouts loader** (paste or file)
      with schema validation + the structural audit, reporting errors in the panel.
- [x] Rules panel rewritten: the four boards, the authored-vs-generated split, the polygon zones,
      the board-full rule.
- [x] `window.__test` extended: `layoutList`, `raw`, `loadLayouts`, `validateLayout`, `zones` with
      `kind`/`verts`/`edges`, `crownOf` with faces and the chamfered deck, `surface`, `canPlace`,
      `freePlacement`, `enclosed`.

## Verified (Playwright over HTTP, 2026-08-10) — all four boards

- [x] Zero console errors or warnings on load; layout audit clean on all four.
- [x] 58 max-power shots per board plus max-power slams into packed clusters → **0** geometry
      violations (overlap / inside a peg / inside a rail / escaped / NaN).
- [x] Scoring recomputed independently from disc positions through the compiled half-planes
      matches the page on every board.
- [x] Rails bounce and ditches remove exactly as each layout declares; the gauntlet loses nothing
      over the side (all four segments rail — the audit enforces the `enclosed` claim).
- [x] The AI takes 20+ legal turns per board; its tag mix includes the new `gate` candidates.
- [x] Determinism: byte-identical replay from one seed after switching away through every other
      board and back.
- [x] Loader accepts a good file and rejects bad JSON, missing fields, a non-convex polygon and a
      ditchless-but-not-`enclosed` board, with named errors and no state change.
- [x] Board-full rule fires and ends the round (exercised on a deliberately jammable variant
      loaded at runtime).
- [x] `python utils/python/build_site.py --no-frontend` succeeds with the JSON inlined and copied.

### Defining traits, measured

- [x] **lane** — widest straight corridor through *both* fences **0.0005″** (the sampling step),
      against **1.375″** through either fence alone. Fence pitch moved 3.75 → 3.0 to get there;
      see DEV-LOG.
- [x] **courtyard** — every edge ditch; diamond zones are true polygons; the cardinal is shut and
      only **0.025″** of the 25″ launch line has a clean straight line to the crown. Straight
      shots that land at all (all post caroms) cover **7.5%** of the line, against **25.9%** on
      the spec's own ring — the weakest of the four traits, and the reason is in DEV-LOG.
- [x] **gauntlet** — every edge rail, nothing ever removed by falling off; **0** of the aim-0
      straight shots reach the crown across a full (placement × power) sweep, while 4,095 angled
      routes do, so the board is closed to the front door but playable.
- [x] **terrace** — crown zone half-width == board half-width (18″), covering the full 36″ rail to
      rail: the crown is a stripe, not a target.

## The idea

Panda's brief: *"a crokinole simulator, JavaScript, framed on the website."* The musing
around it: crokinole is the rare game whose **rulebook is mostly a description of an
object** — the rings are the scoring table, the ditch is the out-of-bounds clause, the hole
is the jackpot. Simulating it forces every implicit rule to become explicit, and the
places where the physical board was doing quiet work show up as code you have to write on
purpose. Claims `CRK.1`–`CRK.6` on the hub page.

The design centrepiece is the opponent. With no dice and no hidden information, the
variance in crokinole is entirely the player's own motor control — so digitising it means
**choosing a noise model**, and that choice *is* the difficulty design.

## Shipped (2026-08-09) — v0

- [x] `index.html` — hub: the bet, what building it revealed, claims `CRK.1`–`CRK.6` with
      `#crk-n` anchors, card → the Board, ghost card → the statistics page candidate.
- [x] `board.html` — the simulator, one self-contained file (~1,150 lines, zero deps):
  - [x] Board drawn to researched dimensions from one `BOARD` constants object (26"
        surface, 24" starting line, 16"/8" rings, 1⅜" hole, 1¼" discs, eight 3/8" pegs
        with centres on the 8" circle at 22.5° off each quadrant line).
  - [x] Deterministic fixed-timestep physics (1/960 s substeps): constant-deceleration
        friction, disc–disc impulses with positional de-overlap, immovable-peg collisions,
        the ditch as removal (no walls — crokinole discs don't bounce back).
  - [x] Rules: 12 discs a side alternating, legal placement on the starting line inside
        your own quadrant, the **contact rule** with the full penalty (shooter *and* every
        own disc it moved are ditched), the empty-board 15-ring requirement, the dead outer
        band, the 20-hole banked on sink, differential round scoring, match to 100.
  - [x] Input: pointer slingshot (drag back, release) **and** a full keyboard path
        (aim/power/fire) so the page is playable and headlessly verifiable with no mouse;
        numeric angle/power readout; `aria-live` status + a text mirror of board state.
  - [x] The opponent: 124–186 candidate shots per turn (targeted takeouts, open-twenty
        lanes, a coarse grid, then refinement around the winner) simulated through the
        *same* physics the player gets; nine named scoring terms; a Gaussian hand model
        (σ on release angle, exposed as the difficulty slider); seeded mulberry32 PRNG with
        the seed shown and settable.
  - [x] The opponent states its reason, reported as the term with the largest **delta vs.
        leaving the board alone** — see DEV-LOG, raw board value made "my discs are worth
        points" win every single time and explained nothing.
  - [x] Collapsible "Rules implemented" panel listing the dimensions, the rules, and every
        approximation, on the page itself.
- [x] `emblem.svg` + themed landing row (maple/burnt-orange, sans).
- [x] Registered in `MUSING-CONFIG.json`; spec `CROKINOLE.md`; human `README.md`;
      verbatim-copy `build-musing.py`.

## Verified (Playwright, 2026-08-09)

- [x] Zero console errors; **one** network request (the page itself) — nothing external.
- [x] Determinism: identical 6-shot traces across two runs of seed 4242, position hashes
      matching to 6 dp, re-confirmed after edits.
- [x] No tunnelling: ~21 shots including nine max-power slams into a packed cluster and a
      head-on peg strike → 0 geometry violations; a 32-frame mid-flight audit → 0. Analytic
      bound: 0.117" of travel per substep vs a 0.8125" disc–peg contact distance.
- [x] Opponent legality: 12/12 shots placed at exactly r = 12.000000 inside its own
      quadrant; fouls only from hand tremor, penalised correctly.
- [x] Full round + 3-round soak (72 shots, 2.06 s) to a match win; differential scoring
      correct; lead alternates.
- [x] Light + dark legible; no horizontal overflow at 380 px.

## Next candidates

- [ ] **The statistics page** (the hub's ghost card) — drive the deterministic sim
      headlessly to plot open-twenty hit rate against tremor σ, and takeout success by ring.
      Turns the hand model into curves; reproducible from a seed.
- [ ] Un-approximate ring scoring: the real rule is line-*touching* (a disc touching a line
      takes the lower value), not centre-in-ring. Two-line change (`r ± discR`), and it
      would make the page's honesty claim airtight.
- [ ] A second difficulty axis: **search width as a visible "how far it looks"** knob. The
      slider is already wired but narratively under-used, and tremor alone makes the
      opponent worse without making it play *differently* — the live tension in `CRK.3`.
- [ ] Doubles (4 discs a side, partners opposite) — changes the contact-rule calculus.
- [ ] Spin. The hand model is currently angle+power only; real players curve shots.

## Open questions

- Should the player's hand get noise too? v0 leaves it off (mouse aim is already imprecise
  and doubling the randomness reads as unresponsive), but it's the honest symmetry.
- 12 discs (general rule sets) vs 8 (WCC tournament singles). v0 implements 12; 8 makes a
  round shorter and sharper and may play better on screen.
- Is the ditch-on-centre-crossing approximation visible in play? A disc should arguably
  teeter on the lip. Nobody has complained yet, which may just mean nobody has looked.

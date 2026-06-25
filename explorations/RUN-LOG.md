# RUN-LOG — overnight explorables orchestration

Live state for the 2026-06-25 overnight run; the durable source of truth across
re-invocations. (The *why* is in `../DEV-LOG.md`; the plan is in
`../plans/PLAN-msl-explorations.md`.) Newest events on top.

## Parameters

- **Started:** 2026-06-25 03:17 PT. **Launch cutoff:** 07:00 PT (= 10:00 ET; tied to peak hours).
- **Cadence:** rolling ~3 Opus builders, top-up-on-completion. Stop early if the curated backlog is
  exhausted (quality over clock). Each page QA'd (identity grep + structure) and committed as it lands.
  **Not pushed.**

## Scoreboard — 7 / 12 ready

- **Ready + committed (7):** `solvency-cell`, `enemy-attack-schedule`, `utility-ai-fit`,
  `jumpgate-topology`, `market-clearing-cell`, `determinism-harness`, `contract-board`
  (+ scout `_research/jumpgate-webs.md`).
- **Building (3):** `glass-cockpit-instruments` (relaunched after an API timeout killed the first try),
  `front-as-fluid`, `prestige-reseeding`.
- **Queued (2):** `run-clock-integral`, `endgame-pressure` (+ Tier-C if time/ideas allow).

Notable: builders have each **caught and fixed a real bug** during headless self-verification
(enemy-schedule reseed-PRNG determinism; market-cell move-cap erasing thin-vs-deep; solvency-cell
deflation that didn't deflate). One infra hiccup: the first `glass-cockpit-instruments` agent died on a
stream idle-timeout with no file written — relaunched clean.

## Hygiene notes

- The `contract-board` agent added an `explorables-static` entry to the **tracked** `.claude/launch.json`
  (it assumed that file was gitignored) to browser-verify its page. **Reverted** — not committed. Later
  builders were told explicitly not to touch config. If Panda wants a `file://` preview entry for
  `explorations/`, add one deliberately.

## Backlog

**Tier A** — solvency-cell ✅ · enemy-attack-schedule ✅ · utility-ai-fit ✅ · jumpgate-topology ✅

**Tier B**
- ready — `market-clearing-cell` (amber) · HAND damped tâtonnement
- ready — `determinism-harness` (violet) · float-vs-i64, fixed order, seed→world, RERUN gate
- ready — `contract-board` (amber) · the board as a war-solvency readout over a run (12/12 verified)
- building — `glass-cockpit-instruments` (cyan) · M3 LOD ladder: sprites→ribbons→weather *(retry)*
- building — `front-as-fluid` (rose) · TIDE graph-Laplacian reaction-diffusion
- building — `prestige-reseeding` (violet) · loss re-seeds the map; gradients bend; the sideways win
- queued — `run-clock-integral` (amber) · the endogenous doomsday = ∫ uncovered shortage
- queued — `endgame-pressure` (rose) · scripted opening vs pure-pressure close

**Tier C (if clock + backlog allow):** lane-routing A*, twelve-good supply chain, seed-sharing/replay,
liquidity deflation spiral, risk-vs-reward contract pricing, prestige tree.

## Scout outputs

- ready — `_research/jumpgate-webs.md` (X4 / Freelancer / EVE-Niarja / Stellaris / ME relays /
  Elite contrast + a recommended backbone+local-cluster board). Fed `jumpgate-topology`.

## Event log (newest on top)

- ~04:25 PT — `contract-board` completed (12/12 verified); reverted its stray launch.json edit;
  relaunched `glass-cockpit-instruments` (first try timed out); launched `prestige-reseeding`. 7/12 ready.
- ~04:10 PT — committed `solvency-cell` + `determinism-harness` + `market-clearing-cell` (commit a2607f5).
- ~03:50 PT — solvency (favorite), market, determinism completed (each caught+fixed a real bug).
- ~03:41 PT — committed `utility-ai-fit` + `jumpgate-topology` + `enemy-attack-schedule` (commit d4ea75e).
- 03:17 PT — branch cut; wave 0 launched; base commit `a226a3f`.

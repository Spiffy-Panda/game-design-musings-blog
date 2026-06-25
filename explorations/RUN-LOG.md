# RUN-LOG — overnight explorables orchestration

Live state for the 2026-06-25 overnight run; the durable source of truth across
re-invocations. (The *why* is in `../DEV-LOG.md`; the plan is in
`../plans/PLAN-msl-explorations.md`.) Newest events on top.

## Parameters

- **Started:** 2026-06-25 03:17 PT.
- **Launch cutoff:** 07:00 PT (= 10:00 ET; the cutoff was tied to *peak hours*, peak ~09:00 ET,
  so the Eastern reading serves the stated reason). Stop *launching* new builders at the cutoff;
  let in-flight finish, then synthesize + summarize.
- **Cadence:** wave 0 = 3 Opus builders + 1 Sonnet scout; thereafter a rolling ~3 Opus builders,
  top-up-on-completion. Stop early if the curated backlog is exhausted (quality over clock).
- **Output:** `explorations/<slug>/index.html`, self-contained, `file://`-openable. Gallery:
  `explorations/index.html`. Each page QA'd (identity-leak grep + structure) and committed as it lands.
  **Not pushed.**

## Scoreboard — 6 / 12 ready

- **Ready + committed (6):** `solvency-cell`, `enemy-attack-schedule`, `utility-ai-fit`,
  `jumpgate-topology`, `market-clearing-cell`, `determinism-harness` (+ scout `_research/jumpgate-webs.md`).
- **Building (3):** `glass-cockpit-instruments`, `contract-board`, `front-as-fluid`.
- **Queued (3):** `prestige-reseeding`, `run-clock-integral`, `endgame-pressure` (+ Tier-C if time/ideas).

Notable: three builders each **caught and fixed a real bug** during headless self-verification —
the enemy-schedule reseed-PRNG determinism bug, the market-cell move-cap that erased thin-vs-deep,
and the solvency-cell deflation model that didn't actually deflate (fixed with a carrying cost).

## Backlog — status: queued / building / ready

**Tier A** — solvency-cell ✅ · enemy-attack-schedule ✅ · utility-ai-fit ✅ · jumpgate-topology ✅

**Tier B**
- ready — `market-clearing-cell` (amber) · HAND damped tâtonnement
- ready — `determinism-harness` (violet) · float-vs-i64 drift, fixed order, seed→world, RERUN gate
- building — `glass-cockpit-instruments` (cyan) · M3 LOD ladder: sprites→ribbons→weather
- building — `contract-board` (amber) · the board as a war-solvency readout over a run
- building — `front-as-fluid` (rose) · TIDE graph-Laplacian reaction-diffusion
- queued — `prestige-reseeding` (violet) · loss re-seeds the map; gradients bend
- queued — `run-clock-integral` (amber) · the endogenous doomsday = ∫ uncovered shortage
- queued — `endgame-pressure` (rose) · scripted opening vs pure-pressure close

**Tier C (if the clock + backlog allow):** lane-routing A*, twelve-good supply chain,
seed-sharing/replay, liquidity deflation spiral, risk-vs-reward contract pricing, prestige tree.

## Scout outputs

- ready — `_research/jumpgate-webs.md` (X4 / Freelancer / EVE-Niarja / Stellaris / ME relays /
  Elite contrast + a recommended backbone+local-cluster board). Fed `jumpgate-topology`.

## Event log (newest on top)

- ~04:10 PT — committed `solvency-cell` + `determinism-harness` + `market-clearing-cell`. Launched
  `glass-cockpit-instruments`, `contract-board`, `front-as-fluid`. 6/12 ready.
- ~03:50 PT — `market-clearing-cell` + `determinism-harness` completed (each caught+fixed a real bug).
- ~03:46 PT — `solvency-cell` (the favorite) completed; added a carrying-cost so deflation is real+recoverable.
- ~03:41 PT — committed `utility-ai-fit` + `jumpgate-topology` + `enemy-attack-schedule` (commit d4ea75e).
- ~03:33 PT — `utility-ai-fit` + `jumpgate-topology` + `enemy-attack-schedule` completed.
- 03:2x PT — scout `jumpgate-webs.md` completed (~3 min); scaffolding written (README, log, gallery, plan).
- 03:17 PT — branch `musing/msl-overnight-explorations` cut; wave 0 launched; base commit `a226a3f`.

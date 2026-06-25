# RUN-LOG — overnight explorables orchestration

Live state for the 2026-06-25 overnight run; the durable source of truth across
re-invocations. (The *why* is in `../DEV-LOG.md`; the plan is in
`../plans/PLAN-msl-explorations.md`.) Newest events on top.

## Parameters

- **Started:** 2026-06-25 03:17 PT.
- **Launch cutoff:** 07:00 PT (= 10:00 ET; the cutoff was tied to *peak hours*, peak ~09:00 ET,
  so the Eastern reading serves the stated reason). Stop *launching* new builders at the cutoff;
  let in-flight agents finish, then synthesize + summarize.
- **Cadence:** wave 0 = 3 Opus builders + 1 Sonnet scout at once; thereafter a rolling ~3 Opus
  builders, top-up-on-completion. Stop early if the curated backlog is exhausted (quality over clock).
- **Output:** `explorations/<slug>/index.html`, self-contained, `file://`-openable. Gallery:
  `explorations/index.html`. Each page QA'd (identity-leak grep + structure) and committed as it lands.
  **Not pushed.**

## Scoreboard

- **Ready + committed (4):** `utility-ai-fit`, `jumpgate-topology`, `enemy-attack-schedule`,
  plus the scout notes `_research/jumpgate-webs.md`.
- **Building (3):** `solvency-cell`, `market-clearing-cell`, `determinism-harness`.
- **Queued (7):** `glass-cockpit-instruments`, `contract-board`, `front-as-fluid`,
  `prestige-reseeding`, `run-clock-integral`, `endgame-pressure` + Tier-C if time/ideas allow.

## Backlog — status: queued / building / ready

**Tier A (priority — favorite + Panda's two additions + the audit)**
- building — `solvency-cell` (amber) · M1 favorite: two ledgers + solvency clamp + Σ-balances
- ready — `enemy-attack-schedule` (rose) · Panda #1: learnable broad-front opening book *(agent fixed a real reseed-PRNG determinism bug)*
- ready — `utility-ai-fit` (emerald) · honest audit; faction-as-directive-issuer (STRONG fit) vs enemy (POOR)
- ready — `jumpgate-topology` (cyan) · Panda #2: archetypes + the EVE-Niarja keystone-severance demo

**Tier B**
- building — `market-clearing-cell` (amber) · HAND damped tâtonnement on one market
- building — `determinism-harness` (violet) · float-vs-i64 drift, fixed order, seed→world
- queued — `glass-cockpit-instruments` (cyan) · M3 LOD ladder: sprites→ribbons→weather
- queued — `contract-board` (amber) · budget-constrained greedy board as economic readout
- queued — `front-as-fluid` (rose) · TIDE graph-Laplacian reaction-diffusion
- queued — `prestige-reseeding` (violet) · loss re-seeds the map; gradients bend
- queued — `run-clock-integral` (amber) · the endogenous doomsday = ∫ uncovered shortage
- queued — `endgame-pressure` (rose) · scripted opening vs pure-pressure close

**Tier C (only if the clock + backlog allow):** lane-routing A*, twelve-good supply chain,
seed-sharing/replay, liquidity deflation spiral, risk-vs-reward contract pricing, prestige tree.

## Scout outputs

- ready — `_research/jumpgate-webs.md` (X4 / Freelancer / EVE-Niarja / Stellaris / ME relays /
  Elite contrast + a recommended backbone+local-cluster board). Fed `jumpgate-topology`.

## Event log (newest on top)

- 03:41 PT — committed `utility-ai-fit` + `jumpgate-topology` + `enemy-attack-schedule` (QA: identity-clean,
  full HTML, agent self-verified). Gallery + this log refreshed. In-flight: solvency, market-clearing, determinism.
- 03:3x PT — launched `determinism-harness`; `enemy-attack-schedule` completed (caught+fixed a reseed determinism bug).
- 03:2x PT — launched `market-clearing-cell`; `utility-ai-fit` + `jumpgate-topology` completed.
- 03:2x PT — `jumpgate-topology` builder launched with scout findings embedded.
- 03:2x PT — scout `jumpgate-webs.md` completed (~3 min); scaffolding written (README, this log, gallery, plan).
- 03:17 PT — branch `musing/msl-overnight-explorations` cut off `musing/msl-approaches-react`;
  wave 0 launched (Sonnet scout + 3 Opus builders); base commit `a226a3f`.

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
  builders, replace-on-completion. Stop early if the curated backlog is exhausted (quality over clock).
- **Output:** `explorations/<slug>/index.html`, self-contained, `file://`-openable. Gallery:
  `explorations/index.html`. Each page committed as it lands; **not pushed**.

## Backlog — status: queued / building / ready / failed

**Tier A (priority — favorite + Panda's two additions + the audit):**
- building — `solvency-cell` (amber) · M1 favorite: two ledgers + solvency clamp + Σ-balances
- building — `enemy-attack-schedule` (rose) · Panda #1: learnable broad-front opening book
- building — `utility-ai-fit` (emerald) · honest audit; faction-as-directive-issuer
- building — `jumpgate-topology` (cyan) · Panda #2: lane-graph archetypes + the Niarja keystone demo

**Tier B (queued):**
- `market-clearing-cell` (amber) · HAND damped tâtonnement on one market
- `determinism-harness` (violet) · float-vs-i64 drift, fixed order, seed→world
- `glass-cockpit-instruments` (cyan) · M3 LOD ladder: sprites→ribbons→weather
- `contract-board` (amber) · budget-constrained greedy board as economic readout
- `front-as-fluid` (rose) · TIDE graph-Laplacian reaction-diffusion
- `prestige-reseeding` (violet) · loss re-seeds the map; gradients bend
- `run-clock-integral` (amber) · the endogenous doomsday = ∫ uncovered shortage
- `endgame-pressure` (rose) · scripted opening vs pure-pressure close

**Tier C (only if the clock and the backlog allow):** lane-routing A*, twelve-good supply chain,
seed-sharing/replay, liquidity deflation spiral.

## Scout outputs

- ready — `_research/jumpgate-webs.md` (X4 / Freelancer / EVE-Niarja / Stellaris / ME relays /
  Elite contrast + a recommended backbone+local-cluster board). Fed `jumpgate-topology`.

## Event log (newest on top)

- 03:2x PT — `jumpgate-topology` builder launched with the scout findings embedded.
- 03:2x PT — scout `jumpgate-webs.md` completed (~3 min); scaffolding written (README, this log).
- 03:17 PT — branch `musing/msl-overnight-explorations` cut off `musing/msl-approaches-react`;
  wave 0 launched (Sonnet scout + 3 Opus builders).

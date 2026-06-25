# PLAN — MSL technical "explorables" (overnight gallery)

A staging gallery of interactive HTML pages, each exploring one **technical aspect** of
*Minimalist Space Logistics* with strong info-visuals. Generated overnight (2026-06-25) by a
fleet of Opus agents. Lives in `explorations/` (not deployed); promote winners into the musing later.

Parent musing: `Minimalist-Space-Logistics/` (mnemonic `MSL`). Lineages: HAND/M1 (amber economy),
M2 (rose enemy/front), M3 (cyan rendering/topology), violet (cross-cutting), emerald (technique audit).

## Goal

Answer "what is the computer *doing* while you play?" one technical aspect at a time — each as a
*pokeable* model, not prose. Seeded by Panda's two additions (a learnable broad-front enemy schedule;
a jumpgate lane web) and an honest "does a utility AI fit?" audit, then fanned across the core sims.

## Approach

- One self-contained `.html` per aspect (inline CSS/JS/SVG/Canvas, no deps, `file://`-openable),
  in the approaches sub-site's console style (tokens copied from `approaches-app/src/styles/index.css`).
- One background Opus agent per page from a self-contained brief; a Sonnet scout for web research
  (jumpgate webs). Rolling ~3 builders; Rule 1 verbatim in every brief; identity gate enforced.
- Orchestration state in `explorations/RUN-LOG.md`; the *why* in `DEV-LOG.md`.

## Backlog

**Tier A — the priorities**
- [ ] `solvency-cell` (M1, amber) — two ledgers, `bid=min(price·qty,till)` clamp, Σ-balances conservation.
- [ ] `enemy-attack-schedule` (M2, rose) — broad-front opening book; conditions = time-since-start /
      time-since-last-op / prior-op fell-vs-held; coverage forks the timeline.
- [ ] `utility-ai-fit` (audit, emerald) — tunable response curves for the faction poster; honest
      per-system verdict (faction STRONG, enemy POOR, rivals shelved).
- [ ] `jumpgate-topology` (cyan) — lane-graph archetypes + the EVE-Niarja keystone/severance demo;
      recommended backbone+local-clusters board.

**Tier B**
- [ ] `market-clearing-cell` (HAND, amber) — damped tâtonnement: trade moves price, price heals.
- [ ] `determinism-harness` (APR.5, violet) — float-vs-i64 drift; fixed iteration order; seed→world.
- [ ] `glass-cockpit-instruments` (M3, cyan) — LOD ladder sprites→ribbons→weather; feed/starve readout.
- [ ] `contract-board` (R.4, amber) — budget-constrained greedy postings as a war-solvency readout.
- [ ] `front-as-fluid` (TIDE, rose) — graph-Laplacian reaction-diffusion; starved sectors rupture.
- [ ] `prestige-reseeding` (APR.3, violet) — loss re-seeds producers; downstream gradients bend.
- [ ] `run-clock-integral` (HAND.2, amber) — the endogenous doomsday = ∫ uncovered shortage.
- [ ] `endgame-pressure` (M2, rose) — scripted opening vs pure-pressure close.

**Tier C (only if clock + backlog allow):** lane-routing A*, twelve-good supply chain, seed-sharing/replay,
liquidity deflation spiral.

## Done criteria

- [ ] Each completed page committed (one commit per page) on `musing/msl-overnight-explorations`.
- [ ] `explorations/index.html` gallery links every ready page; `RUN-LOG.md` reflects final state.
- [ ] Marquee pages (solvency-cell, enemy-attack-schedule, jumpgate-topology) spot-rendered for proof.
- [ ] Morning `DEV-LOG.md` addendum with results; **not pushed** (Panda reviews + promotes, then pushes).

## Sync (Rule 3)

- New top-level working dir `explorations/` — indexed here and in `PLAN.md`; **intentionally not**
  wired into `build_site.py` (a desync to flag if anyone expects it deployed: it is *staging*).
- Promotion path: a winner becomes either a Markdown approach under `Minimalist-Space-Logistics/`
  or a React page in `approaches-app/` — at which point its nav spec + `MUSING-CONFIG`/hub update.

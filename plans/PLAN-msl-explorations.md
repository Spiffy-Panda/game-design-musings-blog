# PLAN — MSL technical "explorables" (overnight gallery)

A staging gallery of interactive HTML pages, each exploring one **technical aspect** of
*Minimalist Space Logistics* with strong info-visuals. Generated overnight (2026-06-25) by a
fleet of Opus agents.

**Status (2026-06-25): COMPLETE — all 16 built, committed, and published** under the musing at
`site/musings/minimalist-space-logistics/explorations/`; the landing-page card links to both the
Approaches and Explorations hubs.

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
- [x] `solvency-cell` (M1, amber) — two ledgers, `bid=min(price·qty,till)` clamp, Σ-balances conservation.
- [x] `enemy-attack-schedule` (M2, rose) — broad-front opening book; conditions = time-since-start /
      time-since-last-op / prior-op fell-vs-held; coverage forks the timeline.
- [x] `utility-ai-fit` (audit, emerald) — tunable response curves for the faction poster; honest
      per-system verdict (faction STRONG, enemy POOR, rivals shelved).
- [x] `jumpgate-topology` (cyan) — lane-graph archetypes + the EVE-Niarja keystone/severance demo;
      recommended backbone+local-clusters board.

**Tier B**
- [x] `market-clearing-cell` (HAND, amber) — damped tâtonnement: trade moves price, price heals.
- [x] `determinism-harness` (APR.5, violet) — float-vs-i64 drift; fixed iteration order; seed→world.
- [x] `glass-cockpit-instruments` (M3, cyan) — LOD ladder sprites→ribbons→weather; feed/starve readout.
- [x] `contract-board` (R.4, amber) — budget-constrained greedy postings as a war-solvency readout.
- [x] `front-as-fluid` (TIDE, rose) — graph-Laplacian reaction-diffusion; starved sectors rupture.
- [x] `prestige-reseeding` (APR.3, violet) — loss re-seeds producers; downstream gradients bend.
- [x] `run-clock-integral` (HAND.2, amber) — the endogenous doomsday = ∫ uncovered shortage.
- [x] `endgame-pressure` (M2, rose) — scripted opening vs pure-pressure close.

**Tier C — shipped**
- [x] `twelve-good-supply-chain` (R.6, amber) — the production DAG; trace a front shortage upstream.
- [x] `liquidity-deflation-spiral` (M1, amber) — money circulation + a healthy-vs-spiral phase view.
- [x] `risk-vs-reward-contract` (M2.4, rose) — payout ≠ EV; price the danger in the drop-off geometry.
- [x] `seed-sharing-replay` (APR.5, violet) — seed → run card + share code; replay a lost run.

_(Not built: `lane-routing-astar`; and `dead-reckoning-deck` — a planned 17th authored-content deck —
never launched when the classifier briefly went down. Clean future adds.)_

## Done criteria

- [x] Each completed page committed (one commit per page) on `musing/msl-overnight-explorations`.
- [x] `explorations/index.html` gallery links every ready page; `RUN-LOG.md` reflects final state.
- [x] Marquee pages (solvency-cell, enemy-attack-schedule, jumpgate-topology) spot-rendered for proof.
- [x] Morning `DEV-LOG.md` addendum with results; **not pushed** (Panda reviews + promotes, then pushes).

## Sync (Rule 3)

- `explorations/` is now **wired into the build + deployed**: MSL's `build-musing.py` copies the
  overview + each page into `site/musings/<slug>/explorations/`; the landing card links both hubs via
  `MUSING-CONFIG.json`'s `"links"`. Synced: `site/SITE.md`, this musing's nav spec, `musing-tech-notes.md`.
- Promotion (optional, later): a winner could still graduate into a Markdown approach or a React page in
  `approaches-app/` for first-class treatment — at which point its nav spec + hub update.

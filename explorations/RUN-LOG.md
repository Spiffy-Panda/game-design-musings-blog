# RUN-LOG — overnight explorables orchestration

Live state for the 2026-06-25 overnight run; the durable source of truth across
re-invocations. (The *why* is in `../DEV-LOG.md`; the plan is in
`../plans/PLAN-msl-explorations.md`.) Newest events on top.

## Parameters

- **Started:** 2026-06-25 03:17 PT. **Launch cutoff:** 07:00 PT (= 10:00 ET; tied to peak hours).
- **Cadence:** rolling ~3 Opus builders, top-up-on-completion. Stop early if the curated backlog is
  exhausted (quality over clock). Each page QA'd (identity grep + structure) and committed as it lands.
  **Not pushed.**

## Scoreboard — 9 / 12 ready (Tier A + B fully launched)

- **Ready + committed (7) + ready-uncommitted (2 → committing):**
  `solvency-cell`, `enemy-attack-schedule`, `utility-ai-fit`, `jumpgate-topology`,
  `market-clearing-cell`, `determinism-harness`, `contract-board`, `front-as-fluid`,
  `glass-cockpit-instruments` (+ scout `_research/jumpgate-webs.md`).
- **Building (3):** `prestige-reseeding`, `run-clock-integral`, `endgame-pressure`.
- **Queued:** Tier B is fully launched. Next is optional **Tier C** (time + ideas allowing).

Every builder has caught + fixed a genuine bug during headless self-verification (reseed-PRNG
determinism, move-cap erasing thin-vs-deep, deflation-that-didn't-deflate, an unquoted CSS key
`SyntaxError`, a stray `<Handle>` JSX artifact). Resilience: the first `glass-cockpit` agent died on
a stream idle-timeout (no file) — relaunched, succeeded.

## Hygiene / rule notes

- `contract-board` agent edited the **tracked** `.claude/launch.json` (assumed gitignored) to verify —
  **reverted**, not committed. Later prompts forbid config edits explicitly.
- `front-as-fluid` agent disclosed one `node -e` (with `require`) mid-task — a brush against Rule 1.
  No artifact, not repeated; later prompts call this out by name and require `node --check <file>` instead.

## Backlog

**Tier A** ✅ — solvency-cell · enemy-attack-schedule · utility-ai-fit · jumpgate-topology

**Tier B**
- ✅ `market-clearing-cell` · ✅ `determinism-harness` · ✅ `contract-board` · ✅ `front-as-fluid` ·
  ✅ `glass-cockpit-instruments`
- building — `prestige-reseeding` (violet) · loss re-seeds the map; the sideways equilibrium win
- building — `run-clock-integral` (amber) · the doomsday as ∫ uncovered shortage
- building — `endgame-pressure` (rose) · the opening→endgame transition; recall stops working

**Tier C — candidate next launches (distinct, high-value):** `twelve-good-supply-chain`,
`liquidity-deflation-spiral`, `risk-vs-reward-contract`, `seed-sharing-replay`, (maybe) `lane-routing-astar`.

## Scout outputs

- ready — `_research/jumpgate-webs.md` (X4 / Freelancer / EVE-Niarja / Stellaris / ME relays / Elite
  contrast + a recommended backbone+local-cluster board). Fed `jumpgate-topology`.

## Event log (newest on top)

- ~04:40 PT — `glass-cockpit` (retry) + `front-as-fluid` completed; committing both. Launched
  `endgame-pressure` (last Tier B). 9/12 ready; Tier A+B fully launched.
- ~04:25 PT — `contract-board` done (reverted its launch.json edit); `glass-cockpit` relaunched after
  a timeout; launched `prestige-reseeding` + `run-clock-integral`. Commit 2799923.
- ~04:10 PT — committed solvency + determinism + market (a2607f5).
- ~03:41 PT — committed utility-ai-fit + jumpgate-topology + enemy-attack-schedule (d4ea75e).
- 03:17 PT — branch cut; wave 0 launched; base commit a226a3f.

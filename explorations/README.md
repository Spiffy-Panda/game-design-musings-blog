# explorations/ — interactive technical "explorables" (staging)

A working gallery of **self-contained, interactive HTML pages**, each an *explorable
explanation* of one technical aspect of **Minimalist Space Logistics**. Generated overnight
(2026-06-25) by a fleet of Opus agents, one page each.

## How to read it

Open **`index.html`** in any browser — it's the gallery. Every page is a single, dependency-free
`.html` file under `explorations/<slug>/index.html`; they open straight from disk (`file://`),
no server and no build step. Each one has a live, pokeable model (sliders / buttons / drag) so
you can falsify your intuition by playing, not just reading.

## What this folder is — and isn't

- **Staging, not published.** `explorations/` is **not** wired into `utils/python/build_site.py`,
  so nothing here deploys to GitHub Pages. It's a holding pen: review, then *promote* the winners
  into the musing proper (a Markdown approach, or a React page in `approaches-app/`) in a later pass.
- **Committed, so Rule 6/7 still apply.** Even un-deployed, these files are in git. Every page was
  authored under the public-surface gate: no real names, no local filesystem paths, third-party
  game references kept brief and transformative.

## Provenance

- Each page was built by its own background agent from a self-contained brief (the game canon, the
  shared visual tokens lifted from `approaches-app/src/styles/index.css`, and a specific technical
  model to visualize). Rule 1 was passed verbatim into every brief.
- `_research/` holds scout notes (e.g. `jumpgate-webs.md`) that fed specific pages.
- Live orchestration state: **`RUN-LOG.md`**. The *why*: `../DEV-LOG.md`. The plan:
  `../plans/PLAN-msl-explorations.md`.

## Visual language

The dark "ops console" palette of the approaches sub-site, re-expressed as plain CSS so each page
is portable. Accents echo the lineages: **amber** = the economic reading (Invisible Hand / Two
Ledgers), **rose** = the enemy/front (Known War), **cyan** = rendering/topology (Glass Cockpit),
**violet** = cross-cutting, **emerald** = technique audits.

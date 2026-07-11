# MINIMALIST-SPACE-LOGISTICS.md — agent nav for this musing

Agent-facing map of the `Minimalist-Space-Logistics/` musing folder. Read this before
editing the musing; you shouldn't need to open another musing to learn the shape. (This
is the `<FOLDER-NAME>.md` half of the deliverable pair — see Rule 2 in `../CLAUDE.md`.
`MUSING.md` is the other half: the published content.)

## What this folder is

A single game-design musing — *Minimalist Space Logistics* — authored in Markdown and
rendered to a standalone HTML page by the site build. The published page is generated; the
source of truth is `MUSING.md`.

**Mnemonic:** `MSL` — this page's prefix for list-item handles (Rule 8 in `../CLAUDE.md`).
Reference items as `MSL.1`, `MSL.2`, … from outside; inside MSL scope the prefix is implicit
(`.1`, or the bare number). Sub-pages declare their own: `APR` (the approaches hub) and
`HAND` / `TIDE` / `DEAD` for the three original approach pages, plus the React mutation pages
`M1` (The Two Ledgers), `M2` (The Known War), `M3` (The Glass Cockpit) — so `HAND.3`, `TIDE.5`,
or `M1.4` resolve unambiguously from anywhere.

## Files

- `MUSING.md` — **the musing.** The published entry-point content. Edit this to change the
  page. Authored in the Markdown subset the renderer supports (see `../musing-tech-notes.md`).
- `MINIMALIST-SPACE-LOGISTICS.md` — this nav file. **Not published** (the build renders
  `MUSING.md` and the `approaches/` sub-pages, never this nav file).
- `build-musing.py` — this musing's build script. Renders `MUSING.md` → `…/index.html`
  **and** the three *original* Markdown approach pages under `approaches/` (depth 4) via the
  shared renderer (`utils/python/musing_render.py`). It does **not** build the hub or the
  mutation pages — those are React (see "Sub-pages"). Invoked by `utils/python/build_site.py`;
  also runnable standalone. It **additionally copies** the repo-root `../../explorations/` gallery
  (overview + each page; internal docs skipped) into `…/explorations/` — see *Sub-pages — explorations*.
- `approaches/` — the three **original** approach pages, as Markdown
  (`the-invisible-hand.md`, `the-tide-line.md`, `dead-reckoning.md`). The hub and the
  HAND-lineage mutation pages are **not** here — they live in `../../approaches-app/` (React).
  See "Sub-pages" below.
- `assets/` — *(optional, none yet)* images/files copied verbatim into the page output.

## How it builds

`MUSING-CONFIG.json` (repo root) lists this folder. `utils/python/build_site.py` reads
that, runs `build-musing.py --out site/musings/<slug>/`, and regenerates the landing-page
card from the config's `name` + `description`. The local server builds on startup; the
Pages workflow builds in CI. Full pipeline: `../musing-tech-notes.md`.

## Sub-pages — approaches

This musing's `approaches/` tree mixes **two** rendering paths (full how-to:
`../musing-tech-notes.md`).

**React app — `../../approaches-app/`** (Vite + Tailwind; the documented framework exception):
- `…/approaches/` — the **hub** (`Hub.tsx`, mnemonic `APR`): the original three + the
  mutations, the converged core (`APR.1`–`APR.5`), and the fork.
- `…/approaches/two-ledgers/` — **The Two Ledgers** (`M1`): money as a munition — faction
  treasury + station liquidity.
- `…/approaches/known-war/` — **The Known War** (`M2`): a scripted enemy opening over a lane
  graph; the front as consumer.
- `…/approaches/glass-cockpit/` — **The Glass Cockpit** (`M3`): design from the screen back;
  rendering a massive abstract front + a fleet.

**Markdown — `approaches/*.md`** (here, rendered by `build-musing.py`, depth 4):
- `the-invisible-hand.md` (`HAND`), `the-tide-line.md` (`TIDE`), `dead-reckoning.md` (`DEAD`)
  — the original three, kept as-is.

`utils/python/build_site.py` runs both: it renders the Markdown pages, then builds the React
app and copies it over `approaches/`. To add a Markdown approach, drop a `.md` here; to add a
React page, work in `../../approaches-app/` (see its `README.md`). The hub links to all six.

## Sub-pages — explorations

A **second** published sub-tree (new as of the overnight explorables run): a gallery of **16
self-contained, interactive HTML "explorables"** — one technical aspect of the game each (the
solvency cell, the jumpgate web, the enemy opening book, a utility-AI fit audit, the deflation
spiral, …). Unlike the approaches, these are **not** authored in this folder — they live at the
**repo root** in `../../explorations/`:

- `../../explorations/index.html` — the overview/hub (back-links to this musing + the approaches hub).
- `../../explorations/<slug>/index.html` — one folder per explorable (standalone; no shared assets).
- `../../explorations/{README.md,RUN-LOG.md,_research/}` — internal, **not** published (no `index.html`).

This musing's `build-musing.py` **copies** the overview + every folder that has an `index.html` into
`site/musings/<slug>/explorations/` (verbatim, not rendered). The landing-page card surfaces both hubs
via `../MUSING-CONFIG.json`'s `"links"` array (Approaches + Explorations). To change an explorable, edit
`../../explorations/<slug>/index.html`; to change the overview, edit `../../explorations/index.html`.

## Editing

- **Change the content:** edit `MUSING.md` (or an original `approaches/*.md`), then rebuild
  (restart `serve_site.py`, or run `python utils/python/build_site.py --drafts`).
- **Change the hub or a mutation page:** edit React in `../../approaches-app/src/pages/`
  (`Hub.tsx`, `TwoLedgers.tsx`, `KnownWar.tsx`, `GlassCockpit.tsx`), then rebuild.
- **Change the card** (name / blurb / visibility): edit this folder's entry in
  `../MUSING-CONFIG.json`.
- **Public-surface gate (Rule 6):** everything in `MUSING.md` is published. No dead names,
  real last names, private paths, or bulk third-party content.

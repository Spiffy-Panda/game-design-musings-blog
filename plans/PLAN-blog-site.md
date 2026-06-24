# PLAN — blog-site

The published "directory" site: a landing page that lists game-design musings, a
local preview server, and the GitHub Pages deploy. Forward-looking chat dump; tick
items as they land.

## Goal

A browsable static directory at <https://spiffy-panda.github.io/game-design-musings-blog/>
where each musing/exploration is a card linking to its own page. Maintainable by hand
now; data-driven later.

## Done (v1 — 2026-06-24)

- [x] `site/index.html` — landing page with intro + (empty) projects grid.
- [x] `site/style.css` — minimal responsive styling, system fonts, light/dark.
- [x] `site/projects/` — home for per-musing pages (empty placeholder).
- [x] `utils/python/serve_site.py` — stdlib preview server, anchored to repo root; `--port`/`--host`/`--open` (browser opt-in).
- [x] `.github/workflows/pages.yml` — deploy `site/` to GitHub Pages via Actions.
- [x] `.claude/launch.json` — `local-server` preview config for the Claude Code launch panel.
- [x] `site/README.md` + `site/SITE.md` — deliverable pair (human + agent spec).

## Done (v2 — 2026-06-24 · musing build framework)

- [x] Musings authored as top-level `<MUSE-SLUG>/` folders (`MUSING.md` content +
      `<FOLDER-NAME>.md` nav + `build-musing.py`), rendered into `site/musings/<slug>/`.
- [x] `MUSING-CONFIG.json` — registry the build reads (folder, slug, name, description, hidden).
- [x] `utils/python/build_site.py` — orchestrator: runs each `build-musing.py`, regenerates `index.html`.
- [x] `utils/python/musing_render.py` — pure-stdlib Markdown→HTML + page chrome (documented subset).
- [x] `serve_site.py` builds on startup (drafts on); `pages.yml` builds in CI (drafts off).
- [x] Generated output (`site/index.html`, `site/musings/`) gitignored; `site/projects/` retired.
- [x] First musing: **Minimalist Space Logistics**.
- [x] `musing-tech-notes.md` — the cross-musing "how"; `CLAUDE.md` + `README.md` define a musing.

## Done (v3 — 2026-06-24 · MSL approaches + multi-page musings)

- [x] Multi-page musing support: `build-musing.py` can render a sub-page tree; `musing_render.py`
      gained `back_href`/`back_text` so nested pages back-link to their parent (with depth-aware
      `css_href`/`home_href`). Documented in `../musing-tech-notes.md`.
- [x] **Minimalist Space Logistics → `approaches/` sub-tree**: a hub (`APPROACHES.md`) plus three
      divergent design pitches — *The Invisible Hand* (agent-based economy), *The Tide Line*
      (pressure-field front), *Dead Reckoning* (deterministic content deck) — each authored by a
      separate agent and ironed down to the simulation tech it implies.
- [x] `MUSING.md` links out to the approaches hub; nav spec records the `APR`/`HAND`/`TIDE`/`DEAD`
      mnemonics (Rule 8).

## Done (v4 — 2026-06-24 · approaches go React + HAND mutations)

- [x] Framework sub-site: `approaches-app/` (Vite + React 19 + Tailwind v4, multi-page,
      `base: "./"`). `build_site.py` builds it and copies it into `site/`; CI gained a Node
      step; `--no-frontend` for fast Markdown-only builds. Zero-dependency stance amended
      (two new rows in `../PROJECT-PITCH.md`).
- [x] Approaches **hub** rebuilt in React (`Hub.tsx`); retired the Markdown `APPROACHES.md`.
- [x] Three **HAND-lineage mutations** (React pages, one agent each): *The Two Ledgers* (M1,
      money / OQ4+OQ5), *The Known War* (M2, topology+front / OQ1+OQ3), *The Glass Cockpit*
      (M3, visuals / OQ2). All inherit the shared `_R` revisions + the 12-good canon.
- [x] Original three approaches (HAND/TIDE/DEAD) left as Markdown, per request.

## Next

- [ ] Light polish: favicon, richer page `<meta>` (per-musing description), `<title>` tweaks.
- [ ] Decide a tag/category scheme for musings (only once there are enough to warrant it).
- [ ] Maybe: live-rebuild / watch mode so the preview server picks up edits without a restart.
- [ ] **Minimalist Space Logistics:** pick an engine from the three approaches and build its
      disqualifying prototype (one-market clearing cell / 12-node field harness / a single hand).

## Resolved

- Per-musing pages are **authored in Markdown and rendered to HTML at build time** (not
  hand-authored). See `../musing-tech-notes.md` + the decisions table in `../PROJECT-PITCH.md`.
- Musings are **authored as top-level `<MUSE-SLUG>/` folders and rendered into `site/`**, not
  kept under `site/projects/` (now retired).

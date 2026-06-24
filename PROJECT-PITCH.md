# PROJECT PITCH — Game Design Musings

## What this is

A long-running, low-ceremony home for game-design musings and exploration: design
notes, mechanics experiments, post-mortems, and half-formed ideas worth keeping. Each
piece is surfaced through a small static site that acts as a **directory** — a landing
page of cards, one per musing, each linking to its own page. Published to GitHub Pages
so it's shareable by link.

The repo lives under a Godot project directory by accident of history; the content is
engine-agnostic and not limited to Godot.

## Shape it's taking

- **Content-primary.** The product is the writing, not a program. Structure favors
  prose deliverables over source code.
- **Static + cheap to publish.** A `site/` folder of HTML/CSS, plus a tiny pure-stdlib
  build step. The landing page and most musing pages stay zero-dependency; the *one*
  exception is the **Minimalist Space Logistics → approaches** sub-site, a React/Tailwind app
  (Vite) the same build step compiles into `site/` (see the decisions table). Previewed
  locally with a stdlib server that builds on startup, and deployed by a GitHub Actions
  workflow that runs the same build (now with a Node step).
- **Directory-first, generated.** The landing page is the spine: every musing is reachable
  from it. Musings are authored as top-level Markdown folders (`<MUSE-SLUG>/MUSING.md`) and
  rendered into `site/` by the build (`utils/python/build_site.py`), which also regenerates
  the index from `MUSING-CONFIG.json`. Lightweight tagging can come later.

## Elevator pitch

_TBD — refine once a few musings exist and a throughline emerges._

## Decisions

| Decision | Date | Why | Supersedes |
|----------|------|-----|------------|
| Repo shape = prose/KB + tooling; no `src/` / `CodeDocs/` tier | 2026-06-24 | Product is content; the only code is a preview server (tooling). Stand up the code-doc tier only if real source appears. | — |
| Publish via GitHub Actions Pages deploy (not deploy-from-branch) | 2026-06-24 | First-class path; keeps `site/` the single source of truth the local server also previews. | — |
| Local preview via stdlib `http.server` (zero deps) | 2026-06-24 | "Basic" server as asked; mirrors how Pages serves static files. | — |
| Landing page hand-authored for v1; generator deferred | 2026-06-24 | Robust with zero projects; generator is a clean follow-up once musings exist. | — |
| Site links are relative (not absolute) | 2026-06-24 | One set of files works locally (served at `/`) and on Pages (served under `/game-design-musings-blog/`). | — |
| Musings authored as top-level `<MUSE-SLUG>/` folders, rendered into `site/` at build time | 2026-06-24 | Keeps authoring in Markdown beside its build script; `site/` becomes generated output. Answers the prior open question (author under `site/projects/` vs render in). | "Keep everything in `site/projects/`" hand-authored model |
| Introduce a build step (`build_site.py` + per-folder `build-musing.py`), pure stdlib | 2026-06-24 | A generator was always the plan once musings existed; pure-stdlib keeps the zero-dependency promise so Pages needs no `pip install`. | "No build step (yet)" |
| Markdown via a small in-repo renderer (`musing_render.py`), not a pip library | 2026-06-24 | Zero-dependency is a core value; a documented subset covers hand-authored musings. Revisit if a musing needs full CommonMark. | — |
| Generated output (`site/index.html`, `site/musings/`) gitignored, built in CI | 2026-06-24 | Keep git focused on sources; Pages rebuilds from sources. Flip to committing output by un-ignoring + dropping the CI build step. | — |
| Approaches hub + HAND-lineage mutation pages built with a React + Tailwind app (Vite), in `approaches-app/` | 2026-06-24 | Those sub-pages wanted rich, designed, interactive layout the Markdown subset can't carry; a real front-end framework earns its keep there. `build_site.py` builds it and copies it into `site/`. | "no framework, no dependencies" — **scoped to the approaches sub-site only** |
| MSL landing page + the original three approaches stay stdlib Markdown renders | 2026-06-24 | Portability: the landing page must stay trivially portable / zero-dependency. The framework is confined to `approaches/` and below, deliberately. | — |

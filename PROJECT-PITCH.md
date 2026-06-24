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
- **Static + cheap to publish.** A `site/` folder of plain HTML/CSS, previewed locally
  with a tiny stdlib server and deployed by a GitHub Actions workflow. No framework, no
  build step (yet), no dependencies.
- **Directory-first.** The landing page is the spine: every musing is reachable from it.
  As the count grows, expect a generator and lightweight tagging.

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

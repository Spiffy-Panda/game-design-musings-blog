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
- [x] `utils/python/serve.py` — stdlib preview server, anchored to repo root; `--port`/`--host`/`--no-browser`.
- [x] `.github/workflows/pages.yml` — deploy `site/` to GitHub Pages via Actions.
- [x] `site/README.md` + `site/SITE.md` — deliverable pair (human + agent spec).

## Next

- [ ] Add the first real musing under `site/projects/<slug>/` and link it from the landing page.
- [ ] **Index generator** — `utils/python/build_index.py` that rebuilds `index.html`'s
      projects grid from per-project metadata (e.g. `site/projects/<slug>/project.json`),
      so adding a musing stops being a hand-edit of HTML. Promote from scrap once it works.
- [ ] Light polish: favicon, richer page `<meta>`, a simple per-project page template.
- [ ] Decide a tag/category scheme for musings (only once there are enough to warrant it).

## Open questions

- Per-musing pages: hand-authored HTML, or author in Markdown and render to HTML at build time?
- Keep everything in `site/projects/`, or author musings as deliverable folders elsewhere and
  render/copy into `site/` at build time?

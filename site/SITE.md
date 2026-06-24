# SITE.md — spec for the published site (`site/`)

Agent-facing spec for the static directory site. `site/` is now **generated output**: the
landing page and every musing page are built from sources elsewhere in the repo. Read this
to understand the output and the static parts; read `../musing-tech-notes.md` for the build
pipeline and how to author a musing.

## What this is

A static "directory" blog deployed to GitHub Pages
(<https://spiffy-panda.github.io/game-design-musings-blog/>) via
`.github/workflows/pages.yml`. The local preview server `utils/python/serve_site.py` builds
the site, then serves this exact folder.

The site is **built**, not hand-authored: `utils/python/build_site.py` reads
`../MUSING-CONFIG.json`, runs each musing's `build-musing.py`, and writes the output here.

## Structure

```
site/
  style.css       all styling; system-font stack, responsive grid, light/dark. TRACKED (source).
  index.html      landing page: header + <ul class="project-grid"> of cards. GENERATED.
  musings/        one subfolder per musing: musings/<slug>/index.html (+ assets, + optional
                  sub-pages; some framework-built, e.g. the React approaches/ tree). GENERATED.
```

- **`style.css` is the only tracked file the build doesn't touch** — edit it directly for
  site-wide styling.
- **`index.html` and `musings/` are generated** (and gitignored). Don't hand-edit them;
  they're overwritten on every build. Change a card via `../MUSING-CONFIG.json`; change a
  page via that musing's `MUSING.md`.

## Hard conventions

- **Relative links only.** The site is served at `/` locally but under
  `/game-design-musings-blog/` on Pages. Generated pages link to `../../style.css` and
  `../../index.html`; the renderer enforces this. Never a leading `/` or an absolute
  `…github.io/…` self-link.
- **One build step, two toolchains.** `build_site.py` + `musing_render.py` are pure stdlib
  (no `pip install`) and render the landing page + Markdown musings. The **one exception** is
  the `approaches/` sub-site for Minimalist Space Logistics: a React/Tailwind app
  (`../approaches-app/`) that `build_site.py` builds via `npm` and copies into `site/` (the
  Pages workflow has a Node step). New Python deps or another framework section: record in
  `../PROJECT-PITCH.md` and update `../musing-tech-notes.md`.
- **Public-surface gate (Rule 6).** Everything here is published. No dead names, real last
  names, private paths, secrets, or bulk third-party content. The sources for the gate live
  in the musing folders — review each `MUSING.md`, not the generated HTML.

## How a musing becomes a page

Authoring happens in a top-level `<MUSE-SLUG>/` folder, not under `site/`. The short version:

1. `<MUSE-SLUG>/MUSING.md` is the content; `<MUSE-SLUG>/build-musing.py` renders it.
2. `../MUSING-CONFIG.json` lists the musing (folder, slug, name, description, hidden).
3. `build_site.py` renders it to `site/musings/<slug>/` and adds its card to `index.html`.

Full checklist + the supported Markdown subset: `../musing-tech-notes.md`.

## Inventory (what exists now)

- `style.css` — base styling (header, card grid, `.prose` for musing pages, `.draft` badge). Tracked.
- `index.html` — generated landing page (one card per visible musing).
- `musings/<slug>/` — generated musing pages. Currently: `minimalist-space-logistics/`,
  whose `approaches/` sub-tree mixes a **React-built** hub + three mutation pages
  (`two-ledgers`, `known-war`, `glass-cockpit`) with three **Markdown** approach pages
  (`the-invisible-hand`, `the-tide-line`, `dead-reckoning`).

## Build & preview

- Local: `python utils/python/serve_site.py` (builds with drafts on, then serves).
- One-shot build: `python utils/python/build_site.py` (add `--drafts` to include hidden musings).
- CI: `.github/workflows/pages.yml` runs the build (no drafts) before uploading `site/`.
- Frontend: the `approaches/` React app is built automatically by `build_site.py` (needs
  Node/npm). `--no-frontend` skips it. See `../approaches-app/README.md`.

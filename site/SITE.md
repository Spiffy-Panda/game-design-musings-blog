# SITE.md — spec for the published site (`site/`)

Agent-facing spec for the static directory site. Read this before editing anything under
`site/`; you shouldn't need to open a prior project page to learn the shape.

## What this is

A static "directory" blog deployed to GitHub Pages
(<https://spiffy-panda.github.io/game-design-musings-blog/>) via
`.github/workflows/pages.yml`. The local preview server `utils/python/serve.py` serves
this exact folder.

## Structure

```
site/
  index.html      landing page: header, intro, <ul class="project-grid"> of cards
  style.css       all styling; system-font stack, responsive grid, light/dark via prefers-color-scheme
  projects/       one subfolder per musing: projects/<slug>/index.html (+ assets)
```

## Hard conventions

- **Relative links only.** The site is served at `/` locally but under
  `/game-design-musings-blog/` on Pages. Use `./style.css`, `./projects/<slug>/`,
  `../../style.css` — never a leading `/` and never an absolute `https://…github.io/…`
  self-link. This is what makes one set of files work in both places.
- **No build step, no dependencies.** Plain HTML + CSS. If/when this changes (a generator,
  a Markdown renderer), record it in `../PROJECT-PITCH.md` and update this spec.
- **Public-surface gate (Rule 6).** Everything here is published. No dead names, real last
  names, private paths, secrets, or bulk third-party content.

## How to add a musing

1. Create `site/projects/<slug>/index.html`. Copy an existing project page as a template
   (or the skeleton below). Link back to the landing page with `../../index.html` and the
   stylesheet with `../../style.css`.
2. Add a card to the projects grid in `site/index.html` — copy a `<li class="project-card">`
   and update title, blurb, and `href="./projects/<slug>/"`.
3. Remove the "no musings yet" placeholder card once the first real card is added.
4. Preview with `python utils/python/serve.py`, then commit (write a `../DEV-LOG.md` entry
   first — Rule 5).

### Project page skeleton

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title><!-- Title --> — Game Design Musings</title>
  <link rel="stylesheet" href="../../style.css">
</head>
<body>
  <main class="prose">
    <p><a href="../../index.html">← Back to all musings</a></p>
    <h1><!-- Title --></h1>
    <!-- content -->
  </main>
</body>
</html>
```

## Inventory (what exists now)

- `index.html` — landing page; projects grid currently holds a single "no musings yet" placeholder.
- `style.css` — base styling (header, card grid, `.prose` for project pages).
- `projects/` — empty except `.gitkeep`.

## Planned

- A generator (`utils/python/build_index.py`) to rebuild the projects grid from per-project
  metadata. Until then, the grid is hand-edited. See `../plans/PLAN-blog-site.md`.

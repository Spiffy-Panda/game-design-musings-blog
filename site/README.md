# site/ — the published directory site

The static site published to **GitHub Pages**:
<https://spiffy-panda.github.io/game-design-musings-blog/>

It's a landing page that acts as a **directory** to game-design musings, plus a
`projects/` folder where each musing's page lives. Plain HTML/CSS — no build step, no
dependencies. **LLM-authored.**

This folder is a **public surface**: everything here is world-readable once pushed. Keep
private data and identity details out of it (see Rule 6 in `../CLAUDE.md`).

## Preview locally

From the repo root:

```
python utils/python/serve_site.py
```

Serves this folder at <http://127.0.0.1:8000/>, the same way Pages serves it. (In Claude
Code, the `local-server` launch config does the same.)

## Files

- `index.html` — the landing page (intro + projects grid).
- `style.css` — shared styling.
- `projects/` — one subfolder per musing.

## Add a musing (kickoff)

Full spec: `SITE.md`. The scenario-specific part to paste when starting one:

> Add a new musing titled "&lt;TITLE&gt;" with slug `<slug>`. Create
> `site/projects/<slug>/index.html` (use an existing project page as the template,
> relative links only), then add a card for it to the projects grid in `site/index.html`.
> Keep it within the public-surface gate (Rule 6).

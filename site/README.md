# site/ — the published directory site (generated output)

The static site published to **GitHub Pages**:
<https://spiffy-panda.github.io/game-design-musings-blog/>

A landing page that acts as a **directory** to game-design musings, plus a `musings/`
folder with one rendered page per musing. **This folder is generated:** `index.html` and
`musings/` are built from `../MUSING-CONFIG.json` and each musing's `MUSING.md` by
`../utils/python/build_site.py`. Only `style.css` is a hand-edited source file. Plain
HTML/CSS, pure-stdlib build, no dependencies. **LLM-authored.**

This folder is a **public surface**: everything here is world-readable once pushed. Keep
private data and identity details out of it (see Rule 6 in `../CLAUDE.md`).

## Preview locally

From the repo root:

```
python utils/python/serve_site.py
```

It builds the site, then serves this folder at <http://127.0.0.1:8000/>, the same way Pages
serves it. (In Claude Code, the `local-server` launch config does the same.) Restart to
rebuild.

## Files

- `style.css` — shared styling. **Tracked** (the build doesn't touch it).
- `index.html` — generated landing page (one card per visible musing). Gitignored.
- `musings/<slug>/` — generated musing pages. Gitignored.

## Add a musing

Musings are authored in top-level `<MUSE-SLUG>/` folders, not here. Full spec:
[`SITE.md`](SITE.md) (the published output) and
[`../musing-tech-notes.md`](../musing-tech-notes.md) (authoring + build).

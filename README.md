# Game Design Musings

A home for miscellaneous game-design musings and exploration — design notes,
mechanics experiments, and half-formed ideas — published as a small browsable
directory (a static "blog"). The folder is named after a Godot project, but the
content is **not** Godot-specific.

> Published site: <https://spiffy-panda.github.io/game-design-musings-blog/>

## Quick start

Preview the site locally (Python 3, no dependencies):

```
python utils/python/serve.py
# or:  py utils/python/serve.py --port 9000
```

It serves `site/` at <http://127.0.0.1:8000/> — the same content GitHub Pages
publishes.

## Where things live

| Path | What |
|------|------|
| [`site/`](site/README.md) | The published static site (landing page + projects). Deploys to GitHub Pages. |
| [`utils/`](utils/README.md) | Durable tooling — the local preview server. |
| [`scrap_scripts/`](scrap_scripts/README.md) | Throwaway exploration scripts (mostly gitignored). |
| [`plans/`](PLAN.md) | Forward-looking plans, indexed by `PLAN.md`. |
| `PROJECT-PITCH.md` | Why this project exists + decisions table. |
| `DEV-LOG.md` | Append-only decision log (newest first). |

## Conventions (plain English)

- **One entry point per audience.** Humans start here; agents start at `CLAUDE.md`.
- **Plans, then design, then deliverables.** Forward-looking notes go in `plans/`; the
  long-arc rationale lives in `PROJECT-PITCH.md`; finished work surfaces in `site/`.
- **No inline interpreter calls.** Real scripts live in files under `utils/` (durable)
  or `scrap_scripts/` (throwaway), never as `python -c "..."`.
- **The site is a public surface.** Anything under `site/` is world-readable once
  pushed — keep private data and identity details out of it.
- **Log the "why" before committing.** Git says what changed; `DEV-LOG.md` says why.

The full agent rule set is in [`CLAUDE.md`](CLAUDE.md).

## How to add a musing

See [`site/SITE.md`](site/SITE.md) for the step-by-step: create `site/projects/<slug>/`,
add a card to the landing page, keep links relative.

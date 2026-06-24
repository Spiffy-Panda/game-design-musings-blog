# Game Design Musings

A home for miscellaneous game-design musings and exploration — design notes,
mechanics experiments, and half-formed ideas — published as a small browsable
directory (a static "blog"). The folder is named after a Godot project, but the
content is **not** Godot-specific.

> Published site: <https://spiffy-panda.github.io/game-design-musings-blog/>

## What's a musing?

A **musing** is one self-contained game-design exploration — a design note, a mechanics
experiment, a post-mortem, a half-formed idea worth keeping. Each lives in its own
top-level `<MUSE-SLUG>/` folder, is written in Markdown (`MUSING.md`), and is published as
a page in the site. The landing page is a directory of cards, one per musing.

The site is **built**: [`utils/python/build_site.py`](utils/python/build_site.py) reads
[`MUSING-CONFIG.json`](MUSING-CONFIG.json), runs each musing's `build-musing.py` to render
its `MUSING.md` into `site/musings/<slug>/`, and regenerates the landing page. The preview
server builds on startup; GitHub Pages runs the same build. How it all works lives in
[`musing-tech-notes.md`](musing-tech-notes.md).

## Quick start

Preview the site locally (Python 3, no dependencies):

```
python utils/python/serve_site.py            # prints the local URL
# or pick a port / auto-open a browser:
py utils/python/serve_site.py --port 9000 --open
```

It builds the site (renders every musing, regenerates the index), then serves `site/` at
<http://127.0.0.1:8000/> — the same content GitHub Pages publishes (Pages runs the same
build in CI). Restart it to rebuild. In Claude Code, the **local-server** launch config
(`.claude/launch.json`) starts the same server in the preview panel.

## Where things live

| Path | What |
|------|------|
| `<MUSE-SLUG>/` | One musing each — a game-design exploration authored in `MUSING.md` (+ its `build-musing.py`). Rendered into `site/`. |
| `MUSING-CONFIG.json` | Registry of musings the build reads (name, description, visibility). |
| `musing-tech-notes.md` | The *how* of musings — build pipeline, supported Markdown, gotchas. |
| [`site/`](site/README.md) | The published static site (landing page + rendered musings). **Generated output**; deploys to GitHub Pages. |
| [`utils/`](utils/README.md) | Durable tooling — the local preview server and the site build. |
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

Create a `<MUSE-SLUG>/` folder with `MUSING.md`, a `<FOLDER-NAME>.md` nav spec, and a
`build-musing.py` (copy an existing one — it needs no edits), then add an entry to
[`MUSING-CONFIG.json`](MUSING-CONFIG.json). Preview with `python utils/python/serve_site.py`.
Full checklist: [`musing-tech-notes.md`](musing-tech-notes.md); the published-output side
is in [`site/SITE.md`](site/SITE.md).

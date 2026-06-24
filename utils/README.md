# utils/ — durable tooling

Catalog of durable, named tooling. Scripts split by language under `utils/<lang>/`.
Every promoted script gets a row below. (Throwaway exploration lives in `scrap_scripts/`
and is mostly gitignored; promote a scrap script here the moment anything other than a
human or an agent depends on it — see Rule 1 in `../CLAUDE.md`.)

Every script anchors to the repo root via `__file__`, so it runs from any CWD.

## Catalog

| Script | Run | What it does |
|--------|-----|--------------|
| `python/serve_site.py` | `python utils/python/serve_site.py [--port 8000] [--host 127.0.0.1] [--open]` | Local static preview server for `site/`. Serves the exact content GitHub Pages publishes; stdlib only. Wired to the `local-server` launch config (`.claude/launch.json`). |

# scrap_scripts/ — throwaway exploration

Scratch scripts for one-off exploration. **Mostly gitignored** — only this README is
tracked (so the convention travels with the repo); the scripts themselves stay local.

## Why this exists

Rule 1 (`../CLAUDE.md`) bans inline interpreter calls (`python -c`, `node -e`, …). The
moment a command needs an `import` — or grows loops/variables/conditionals — it becomes
a file. Quick, disposable files land here.

## Naming & layout

- Split by language: `scrap_scripts/python/`, `scrap_scripts/bash/`, …
- Name files `<NN>_<slug>.<ext>`, where `<NN>` is a zero-padded counter that increments
  **per-language folder** (e.g. `python/01_poke_pages_api.py`).
- Anchor every script to the repo root: `Path(__file__).resolve().parents[N]` (Python)
  or the language equivalent. Never assume the CWD.

## Lifecycle & promotion

- Scratch scripts are disposable. Delete freely; they are not part of the product.
- **Promote** to `utils/<lang>/` the moment a script is depended on by anything other
  than (a) a human at the CLI or (b) an LLM agent — i.e. it builds an artifact,
  regenerates tracked content, or is run often enough to deserve a stable name. On
  promotion: drop the `NN_` prefix, give it a human name + header comment, and add a row
  to `utils/README.md`.

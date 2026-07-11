# utils/ — durable tooling

Catalog of durable, named tooling. Scripts split by language under `utils/<lang>/`.
Every promoted script gets a row below. (Throwaway exploration lives in `scrap_scripts/`
and is mostly gitignored; promote a scrap script here the moment anything other than a
human or an agent depends on it — see Rule 1 in `../CLAUDE.md`.)

Every script anchors to the repo root via `__file__`, so it runs from any CWD.

## Catalog

| Script | Run | What it does |
|--------|-----|--------------|
| `python/serve_site.py` | `python utils/python/serve_site.py [--port 8000] [--host 127.0.0.1] [--open]` | Local preview server for `site/`. **Builds the site on startup** (drafts on), then serves the exact content GitHub Pages publishes; stdlib only. Wired to the `local-server` launch config (`.claude/launch.json`). |
| `python/build_site.py` | `python utils/python/build_site.py [--drafts] [--no-frontend]` | The site build: reads `MUSING-CONFIG.json`, runs each musing's `build-musing.py`, regenerates `site/index.html`, **then builds the `../approaches-app/` React sub-site (via `npm`) and copies it into `site/`** (`--no-frontend` skips it; missing Node is non-fatal). `--drafts` also builds hidden musings. Run by `serve_site.py` and the Pages workflow. |
| `python/musing_render.py` | _(imported, not run directly)_ | Shared Markdown→HTML renderer + page chrome used by every `build-musing.py`. Pure-stdlib subset of Markdown — see `../musing-tech-notes.md`. |
| `python/build_exploration_video.py` | `python utils/python/build_exploration_video.py <slug> [--tts sapi\|piper] [--speed 1.6] [--keep] [--browser edge\|chrome]` | Renders one MSL exploration into a narrated ~60–90s explainer **video** (`explorations/_video/out/<slug>.mp4`). Reads a scene script (`explorations/_video/scenes/<slug>.json`); per scene it screenshots either an ops-console **slide** (authored HTML, incl. re-drawn SVG diagrams) or the **real page** (headless Edge/Chrome), synthesizes narration, and assembles Ken-Burns clips with `ffmpeg`. **TTS backends:** `sapi` (Windows `System.Speech`, via `../powershell/tts_sapi.ps1`) or `piper` (free local *neural* voice; auto-resolved from `%LOCALAPPDATA%\piper`, override with `PIPER_EXE`/`PIPER_VOICES`). `speed` (per-scene-script or `--speed`) sets narration tempo via `ffmpeg atempo`. **Zero pip.** Live JS instruments don't paint in one-shot capture, so their diagrams are re-drawn in the slides — see `../DEV-LOG.md`. |
| `powershell/tts_sapi.ps1` | `powershell -File utils/powershell/tts_sapi.ps1 -Text "…" -Out out.wav [-Voice "Microsoft Zira Desktop"] [-Rate 0]` | Synthesizes one narration line to a WAV via Windows SAPI (`System.Speech`). Helper for `build_exploration_video.py`; zero external deps. |

> **Not in `utils/`:** the Minimalist Space Logistics *approaches* sub-site is a React/Tailwind
> app at `../approaches-app/` (its own `README.md`). It's the one framework-built part of the
> site; `build_site.py` compiles it and copies it into `site/` as part of the normal build.

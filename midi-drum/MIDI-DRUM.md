# MIDI-DRUM.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **MIDI Drum Coach** musing (Rule 2). This is an
**HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored hub `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/midi-drum/` (pattern documented in `../musing-tech-notes.md`).
This file and `README.md` stay internal.

**Mnemonic (Rule 8):** **`MDC`** — `MDC.1`–`MDC.5`, the five design claims on the hub
`index.html` (anchors `#mdc-1` … `#mdc-5`). **Append-only IDs**: new claims take new
numbers, nothing renumbers. Cite them from other pages, plans, and reviews.

## File map

| File | Role |
|------|------|
| `index.html` | Hub — the musing prose (published entry point): the bet (instrument-as-controller, legible director, notation-as-UI), claims `MDC.1`–`MDC.5`, card → the Coach, ghost card → the Fill Director candidate. |
| `coach.html` | The tool — one self-contained page: Web MIDI connect (GM drum map → 8 lanes, device picker, hot-plug, unsupported/denied fallbacks), **Map pads** MIDI-learn remapper (click a lane, hit the drum: note→lane bindings override GM, per-pad display, double-click clears a lane, reset button; localStorage `mdc:map`), on-screen + keyboard pads feeding the same hit pipeline, 13-pattern step-grid library (16th grids + one 12-cell triplet shuffle, levels 1–5, families), synthesized WebAudio kit + click (lookahead scheduler), practice mode (count-in → loop → ±30/±70/±120 ms judging → miss/stray → signed rush/drag bias), and the director: rules R1–R5, suggestion cards with a spoken "why", free-listen stats, latency-calibration slider (localStorage `mdc:calib`). |
| `emblem.svg` | Landing-row emblem: drum head as a 16-step sequencer ring carrying the money beat, crossed sticks; colors via `--m-*` tokens. |
| `README.md` | Human doc: what it is, how to run it, the design reading. |
| `build-musing.py` | Verbatim copy of `*.html` → `site/musings/<slug>/` (never copies `.md`). |

## Invariants

- Every page is self-contained — inline CSS/JS, no external assets, both themes via
  `prefers-color-scheme`. Each hand-authors the **site-wide breadcrumb** (`nav.crumbs`,
  rooted at the portfolio; see `../musing-tech-notes.md` "Navigation: the breadcrumb
  standard") in this musing's own tokens (`--orange` links, `--line2` separators,
  `--ink3` current).
- **`coach.html` is the behavior contract**: the GM note map, the judging windows
  (±30/±70/±120 ms), the step-value encoding (0 off · 1 ghost · 2 hit · 3 accent), and
  director rules R1–R5 live there. Future pages (the Fill Director) cite or amend those
  values — never fork them. The hub's prose quotes the windows; keep them in sync.
- **No network, no telemetry**: hits are processed in-tab; the only persistence is
  `localStorage` (`mdc:calib` latency offset, `mdc:map` note→lane bindings). Keep it that
  way — the footer promises it.
- Web MIDI requires a secure context + a user-gesture permission request; Safari doesn't
  ship it. The pads/keyboard path must always keep the page fully demonstrable with zero
  hardware — never gate a feature on a connected device.
- Registered in `../MUSING-CONFIG.json` (row + sublink). Everything in `*.html` deploys
  to Pages — Rule 6 gate applies to any edit.
- Cross-musing links (if any come) use the shape `../<slug>/<page>.html`; folder name ==
  slug (lowercase) so repo and site resolve identically.

## History

Authored and registered 2026-07-12 (Panda's brief: "connects to a MIDI device and
provides suggested rhythms"; scoped to a v0 that works kit-free). Plan:
`../plans/PLAN-midi-drum.md` (next candidates: Fill Director, accent judging,
MIDI-learn mapping, auto-calibration). Decisions: `../DEV-LOG.md`.

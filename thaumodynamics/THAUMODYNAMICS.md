# THAUMODYNAMICS.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **Thaumodynamics (MDYN)** musing (Rule 2). This is an
**HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored gallery `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/thaumodynamics/` (pattern documented in `../musing-tech-notes.md`).
This file and `README.md` stay internal.

**Mnemonic (Rule 8): `THAU`** — stable handles: `THAU.1` = monograph, `THAU.2` = worksheet,
`THAU.3` = bout.

## File map

| File | Role |
|------|------|
| `index.html` | Gallery/hub — the musing's landing page (published entry point). |
| `thaumodynamics.html` | `THAU.1` — the monograph: Hearth/Loom fields, sector + Exchange laws, anima, worked protocols, materials, constants (§9 is the in-world constants table other pages must agree with). |
| `mdyn101-worksheet3.html` | `THAU.2` — the problem set, with blank / student-attempt / answer-key toggle. |
| `ashfield-bout.html` | `THAU.3` — the duel chronicle (slide deck, marshals' calculations per exchange). |
| `README.md` | Human doc: reading order, provenance (private artifact URLs), history. |
| `build-musing.py` | Verbatim copy of `*.html` → `site/musings/<slug>/` (never copies `.md`). |

## Invariants

- Every page is self-contained — no external assets, both themes inline. Keep it that way
  when editing. Each page hand-authors the **site-wide breadcrumb** (`nav.crumbs`, rooted at
  the portfolio; see `../musing-tech-notes.md` "Navigation: the breadcrumb standard") using
  this musing's own tokens (`--storm` links, `--line2` separators, `--ink3` current). The
  page body renders standalone from disk; only the "Game Design Musings" crumb is
  site-relative (resolves on the served site / preview, not a raw `file://` open).
- The three pages share one set of in-world constants; the worksheet's numbers match the
  monograph's plates deliberately. Change a constant in one page → re-audit the other two.
- Registered in `../MUSING-CONFIG.json` (card + sublinks). Everything in `*.html` deploys
  to Pages — Rule 6 gate applies to any edit.
- Sibling system: `../logical-magic/` (LoMa) links here as `../thaumodynamics/index.html`;
  that relative shape works both in-repo and under `site/musings/` — don't rename the
  folder or slug without fixing both sides.

## History

Authored 2026-07-10 in a Claude Code session, imported the same day as a staging folder
(see `DEV-LOG.md`), promoted to a registered musing 2026-07-10 when the landing page grew
cards for all projects. No plan file; decisions live in `DEV-LOG.md`.

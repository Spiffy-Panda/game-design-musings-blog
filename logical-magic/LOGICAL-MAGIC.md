# LOGICAL-MAGIC.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **Logical Magic (LoMa)** musing (Rule 2). This is an
**HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored gallery `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/logical-magic/` (pattern documented in `../musing-tech-notes.md`).
This file and `README.md` stay internal.

**Mnemonic (Rule 8): `LOMA`** — the stable handles `LOMA.1`–`LOMA.7` are the seven design
claims in `pitch.html` §7 (anchors `#loma-1` … `#loma-7`). Cite them from other pages,
plans, and reviews.

## File map

| File | Role |
|------|------|
| `index.html` | Gallery/hub — the musing's landing page (published entry point); carries ghost cards for planned pages. |
| `pitch.html` | Page I — the system pitch: premise (world = model; warrant in *strokes* → settlement in *flips* of grace), quantifier ladder, three audited pleas (Emberfield / Long Wish / Quarter-Day Breach), vessels (monads), the Mint (second order), the six Limits, `LOMA.n` claims, **§8 tuning table**. |
| `README.md` | Human doc: reading order, design summary, consistency contract. |
| `build-musing.py` | Verbatim copy of `*.html` → `site/musings/<slug>/` (never copies `.md`). |

Planned pages (tracked in `../plans/PLAN-logical-magic.md`): II "LOMA 101 · Problem Set
One" (worksheet w/ toggle), III "The Assize of Bells" (breach trial as dueling
derivations), candidate IV "The Grimoire" (interactive lemma cache).

## Invariants

- Every page is self-contained — no external assets, both themes inline. Keep it that way
  when adding pages. Each page hand-authors the **site-wide breadcrumb** (`nav.crumbs`,
  rooted at the portfolio; see `../musing-tech-notes.md` "Navigation: the breadcrumb
  standard") using this musing's own tokens (`--teal` links, `--line2` separators, `--ink3`
  current). The page body renders standalone from disk; only the "Game Design Musings" crumb
  is site-relative (resolves on the served site / preview, not a raw `file://` open). Note
  `pitch.html` also has a separate in-page section-jump `nav` — that is not the breadcrumb.
- **`pitch.html` §8 is the constants contract**: stroke rate, grace tide, and every
  headline price. Future pages cite or amend that table — never fork the numbers (the
  MDYN "constants agree across pages" discipline).
- Registered in `../MUSING-CONFIG.json` (card + sublink). Everything in `*.html` deploys
  to Pages — Rule 6 gate applies to any edit.
- Cross-links to the sibling system use the shape `../thaumodynamics/<page>.html`, which
  resolves identically in-repo and under `site/musings/`. New pages joining the set:
  top-level `<name>.html` in this folder + a live card in `index.html` + a sublink row in
  the registry entry if card-worthy.

## History

Pitched and authored 2026-07-10 (pitch page first, per Panda's brief: FOL/SOL core,
monadic advanced tier, MDYN-grade grounded calculation, no field equations); registered
the same day when the landing page grew cards for all projects. Plan:
`../plans/PLAN-logical-magic.md`. Decisions: `../DEV-LOG.md`.

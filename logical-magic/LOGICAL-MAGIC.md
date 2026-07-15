# LOGICAL-MAGIC.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **Logical Magic (LoMa)** musing (Rule 2). This is an
**HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored gallery `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/logical-magic/` (pattern documented in `../musing-tech-notes.md`).
This file and `README.md` stay internal.

**Mnemonics (Rule 8):** two handle series, both **append-only IDs** (new items take new
numbers, nothing renumbers):

- **`LOMA`** — `LOMA.1`–`LOMA.7`, the seven design claims in `pitch.html` §7 (anchors
  `#loma-1` … `#loma-7`).
- **`LVIG`** — `LVIG.1`–`LVIG.3`, the three everyday-life vignettes in `vignettes.html`
  (anchors `#lvig-1` … `#lvig-3`); distinct from Space Feudal's `VIG.n`. New vignettes
  number from `LVIG.4`.

Cite them from other pages, plans, and reviews.

## File map

| File | Role |
|------|------|
| `index.html` | Gallery/hub — the musing's landing page (published entry point); three live cards + the Grimoire ghost. |
| `pitch.html` | Page I — the system pitch: premise (world = model; warrant in *strokes* → settlement in *flips* of grace), quantifier ladder, three audited pleas (Emberfield / Long Wish / Quarter-Day Breach), vessels (monads), the Mint (second order), the six Limits, `LOMA.n` claims, **§8 tuning table**. |
| `loma101-worksheet1.html` | Page II — "LOMA 101 · Problem Set One": three-way toggle (blank / student M. Sedge / Reader's key, `#ws[data-mode]` gating); Section A MC ×5, Section B audits ×3 (B1 Emberfield pricing, B2 the winter wish, B3 Bellhall post-repeal), Section C theory ×2; 40 marks. All numbers cite pitch §8. |
| `assize-of-bells.html` | Page III — "The Assize of Bells": Crown v. Fen as a 10-slide trial-duel (arrow keys / dots / `#sN` deep links). Canon established: the Rule of Sound Warrant (courts reject ⊥-tainted derivations), "no writ, no wall," conviction by ash-gap. Ledger: 38 strokes, 0 flips. |
| `vignettes.html` | Companion page — "Everyday Records": the three everyday-life vignettes `LVIG.1`–`LVIG.3` (Transcription Nights / The Letter Kept / Crack and Splint), each with its handle, one-line abstract, and body; anchors `#lvig-1`–`#lvig-3`. Numbers honor pitch §8; the Space Feudal `loom.html` vignette-page role. Written via `VIGNETTE-HANDOFF.md`. |
| `VIGNETTE-HANDOFF.md` | Internal, reusable chat handoff for everyday-life vignettes (`LVIG.n` handles): system digest, canon list, constants, guardrails, resource links. Never published. |
| `README.md` | Human doc: reading order, design summary, consistency contract. |
| `build-musing.py` | Verbatim copy of `*.html` → `site/musings/<slug>/` (never copies `.md`). |

Remaining candidate (tracked in `../plans/PLAN-logical-magic.md`): IV "The Grimoire"
(interactive lemma cache — the LOMA.2 loop as a toy). The everyday-life vignettes, once a
candidate, landed 2026-07-12 as the companion page `vignettes.html` (written via
`VIGNETTE-HANDOFF.md`, on the Space Feudal vignette-page precedent) — so the Grimoire is
now the only remaining candidate.

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

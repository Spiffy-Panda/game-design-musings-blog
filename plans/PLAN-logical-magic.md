# PLAN — logical-magic (LoMa)

**Goal:** design **[Lo]gical [Ma]gic (LoMa)** — a magic system grounded in first- and
second-order logic, with advanced practice dipping into monads and other abstract
CS/math (game semantics, fixed points, categoricity, the classical limit theorems).
LoMa produces the classic magic effects (fire, wishes, wards, scrying) *by fiat* —
unlike MDYN/thaumodynamics it owes nothing to field equations — but it must mirror
MDYN's **detailed, grounded calculations**: costs are countable, proofs are shown,
numbers agree across pages.

**Core conceit (v1):** the world is a first-order model; a spell is a sentence the
world does not yet satisfy; casting = supplying a consistency **warrant** (a proof,
paid in *strokes*) after which the world **settles** into a nearest model of the
sentence (paid in *flips* of atomic facts, drawn from local *grace*). The world
picks among minimal settlements adversarially ("the Miser's Law") — the monkey's
paw as a theorem, not a trope.

**Folder pattern:** mirrors `thaumodynamics/` — a top-level `logical-magic/`
folder of self-contained HTML pages + gallery `index.html` + `README.md`.
Started as unregistered staging (2026-07-10, morning); **promoted the same day**
to a registered **HTML-first musing**: verbatim-copy `build-musing.py`, nav spec
`LOGICAL-MAGIC.md`, registry row + landing-page card. Every `*.html` deploys —
Rule 6 applies. Mnemonic (Rule 8): **`LOMA`**.

## Checklist

- [x] `logical-magic/pitch.html` — the pitch page: premise, quantifier ladder,
      three fully-worked pleas (Emberfield / the Long Wish / the Quarter-Day
      Breach), vessels (monads), the Mint (second order), the six Limits,
      design payoffs (`LOMA.1`–`LOMA.7`), tuning constants, roadmap.
- [x] `logical-magic/index.html` — gallery card + planned-page ghosts.
- [x] `logical-magic/README.md` — staging note, mnemonic, page table.
- [x] Page II — **done 2026-07-12**: `loma101-worksheet1.html` ("LOMA 101 ·
      Problem Set One") — blank / M. Sedge's attempt / Reader's key toggle,
      40 marks, every number cited from pitch §8.
- [x] Page III — **done 2026-07-12**: `assize-of-bells.html` ("The Assize of
      Bells") — Crown v. Fen as a 10-slide trial-duel; establishes the Rule of
      Sound Warrant, "no writ, no wall," and conviction by ash-gap; 38 strokes,
      0 flips. (Open question resolved: yes, game semantics is the spine.)
- [x] Vignette pipeline opened — `VIGNETTE-HANDOFF.md` (2026-07-12): reusable
      chat handoff for everyday-life vignettes (`LVIG.n`); prompts + prose happen
      in a separate chat, canon + constants stay defined here.
- [x] Vignette companion page — **done 2026-07-12**: `vignettes.html` ("Everyday
      Records") — the three finished vignettes `LVIG.1`–`LVIG.3` (Transcription
      Nights / The Letter Kept / Crack and Splint), integrated from the handoff
      chat as a verbatim-copy HTML page; gallery card + registry sublink + nav spec
      + handoff canon synced. Mnemonic **`LVIG`** declared (append-only; next is
      `LVIG.4`). The Grimoire (IV) stays the only remaining candidate.
- [x] Decide promotion: **done 2026-07-10** — registered as an HTML-first musing
      (verbatim-copy `build-musing.py`, gallery `index.html` as entry, nav spec
      `LOGICAL-MAGIC.md`), same day as `thaumodynamics/`, when the landing page
      grew cards for all projects. Pattern: `musing-tech-notes.md`.

## Open questions

- Tuning constants (stroke rate, grace tide, creation flip-cost) are declared
  placeholders on the pitch page — one table to change, numbers must stay
  consistent across future pages (MDYN discipline).
- Whether the duel page uses game semantics (∀ = falsifier's move) as its
  scoring spine — current lean: yes.

# logical-magic/ — the LoMa set

**[Lo]gical [Ma]gic (LoMa)** — an invented magic system built on first- and second-order
logic, with the advanced practice dipping into monads, game semantics, fixed points, and
the classical limit theorems. LoMa produces the classic magical effects (fire, wishes,
wards, scrying, true names) *by fiat* — deliberately **no field equations** — but keeps
the Thaumodynamics discipline of detailed, grounded calculation: every spell is audited
in two integer currencies (**strokes** = proof labor, **flips** = facts changed at
settlement), derivations are printed, and one tuning table keeps numbers consistent
across pages. Authored 2026-07-10.

## How to read it

Open **`index.html`** in any browser — it's the gallery. Every page is a single
dependency-free `.html` file that opens straight from disk (`file://`), no server and no
build step, with light + dark themes.

| Page | File | What it is |
|------|------|------------|
| I · The pitch | `pitch.html` | The system in one page: premise (world = model, spell = sentence, warrant → settlement), the quantifier ladder (circles = alternation depth), three fully-audited pleas (Emberfield / the Long Wish / the Quarter-Day Breach), vessels (monads) for ritual magic, the Mint (second-order enchantment), the six Limits (Gödel, Tarski, Löb, Rice, compactness, Löwenheim–Skolem), design payoffs `LOMA.1`–`LOMA.7`, tuning constants, roadmap. |
| II · The pedagogy | `loma101-worksheet1.html` | "LOMA 101 · Problem Set One" — MC (seats & circles), three priced audits (Emberfield two ways / the winter wish / Bellhall after the repeal), theory shorts; blank / M. Sedge's attempt / Reader's key toggle, mirroring MDYN 101 Worksheet 3. |
| III · The application | `assize-of-bells.html` | "The Assize of Bells" — Crown v. Fen: the Bellhall breach tried as a duel of derivations (Rule of Sound Warrant, "no writ, no wall," the old well, the ash-gap conviction); 10-slide chronicle, Clerks' stroke ledger per exchange, 38 strokes / 0 flips. |
| Companion · everyday lives | `vignettes.html` | "Everyday Records" — three everyday-life vignettes `LVIG.1`–`LVIG.3` (Transcription Nights / The Letter Kept / Crack and Splint): a finished name-scroll, a fae treaty kept too well, a mend that cracks clean. Verbatim-copy companion page (the Space Feudal `loom.html` role); every number honors pitch §8. |
| IV · candidate | — | "The Grimoire" — interactive lemma cache (the LOMA.2 progression loop as a toy). |
| (internal) | `VIGNETTE-HANDOFF.md` | Reusable chat handoff for everyday-life vignettes (`LVIG.n`): system digest, canon, constants, guardrails, resources. Not published. |

Game-design reading: the set is an exercise in **abstraction-first worldbuilding** — take
mathematical logic literally, price it in countable units, and let the theorems be the
conservation laws. The monkey's paw becomes minimal-model revision, dungeon doors become
writs with derivations as keys, duels become evaluation games (∀ is the falsifier's move),
and progression is proof brevity (grimoires are lemma caches; spell slots are pre-paid
warrants). Sibling set: `../thaumodynamics/` (MDYN) — same grounded-calculation bet, made
with field equations instead.

## What this folder is — and isn't

- **A registered, published musing** (promoted 2026-07-10, same day it was staged, when
  the landing page grew cards for every project). Listed in `../MUSING-CONFIG.json`;
  `build-musing.py` copies every `*.html` verbatim to `site/musings/logical-magic/`.
  **HTML-first musing:** no `MUSING.md` — the gallery `index.html` is the published entry
  point, and `LOGICAL-MAGIC.md` is the Rule-2 agent-nav spec. Pattern documented in
  `../musing-tech-notes.md`.
- **Published, so Rule 6/7 apply with teeth.** Every `*.html` here deploys to GitHub
  Pages. Authored under the public-surface gate: the mathematics is real and standard;
  every person, college, casebook, and incident is fictional; no local paths, no identity
  terms. The `.md` files stay internal (never copied).
- **Mnemonic (Rule 8):** `LOMA`, declared canonically in `LOGICAL-MAGIC.md`. The pitch
  page's design-payoff claims carry the stable handles `LOMA.1`–`LOMA.7` (anchors
  `#loma-1` … `#loma-7`); cite them from other pages and reviews.
- **Plan:** `plans/PLAN-logical-magic.md` tracks the page roadmap and open tuning
  questions.

## Consistency contract

`pitch.html` §8 is the **tuning table** — stroke rate, grace tide, and every headline
price (Emberfield 13 strokes / 905 flips; the airtight wish 19 strokes / ≈40 flips; the
breach 8 strokes / 0 grace; Mending 22 strokes / 12 flips; Keen 11 strokes + 1 grace/day).
Companion pages must cite that table or amend it there — never fork the numbers (the MDYN
"constants agree across pages" discipline).

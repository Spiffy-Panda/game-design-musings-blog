# thaumodynamics/ — the Coupled Sector set

A three-page, self-contained HTML set building out one invented magic system —
**Thaumodynamics**, a Maxwell-mirror field theory of fire and lightning — and then using it
twice: as pedagogy and as combat-sport worldbuilding. Authored 2026-07-10 in a Claude Code
session (originally scoped to another workspace; moved here, where game-design material
belongs).

## How to read it

Open **`index.html`** in any browser — it's the gallery. Every page is a single dependency-free
`.html` file that opens straight from disk (`file://`), no server and no build step. All three
share one hand-mixed design system (light + dark) and one set of in-world constants — the
numbers agree across pages.

| Page | File | What it is |
|------|------|------------|
| I · The system | `thaumodynamics.html` | Monograph: Hearth/Loom fields, four sector laws, two Exchange couplings, anima, worked spell protocols, materials, constants. Interactive quench chart. |
| II · The pedagogy | `mdyn101-worksheet3.html` | "MDYN 101, Worksheet 3": MC + calculation + short-answer, with a three-way toggle — blank handout / a student's imperfect attempt / worked answer key. |
| III · The application | `ashfield-bout.html` | Slide-deck chronicle of a duel between the two arts under an invented dueling code (seals, ferns, bout to three); in-world commentary + marshals' calculations per exchange. |

Game-design reading: the set is an exercise in **mechanics-first worldbuilding** — derive the
magic from field equations, then let the constraints generate the pedagogy, the sport, its
scoring rules, and the tactics (the duel's finale is a straight consequence of the physics).

## What this folder is — and isn't

- **A registered, published musing** (promoted 2026-07-10 from staging, when the landing
  page grew cards for every project). Listed in `../MUSING-CONFIG.json`; `build-musing.py`
  copies every `*.html` verbatim to `site/musings/thaumodynamics/` — the hub-page path
  anticipated at staging time, with one deviation: **HTML-first musings carry no
  `MUSING.md`** (the gallery `index.html` is the published entry point). The Rule-2
  agent-nav spec is `THAUMODYNAMICS.md`; the pattern is documented in
  `../musing-tech-notes.md`.
- **Published, so Rule 6/7 apply with teeth.** Every `*.html` here deploys to GitHub
  Pages. Authored under the public-surface gate: all names are fictional, no local paths,
  no identity terms. The `.md` files stay internal (never copied).
- **Mnemonic (Rule 8):** `THAU` — `THAU.1` = monograph, `THAU.2` = worksheet,
  `THAU.3` = bout. Declared canonically in `THAUMODYNAMICS.md`.

## Provenance

Each page also exists as a private claude.ai artifact (same content, hosted):

- Monograph: <https://claude.ai/code/artifact/e27d5dc9-354a-41cf-a896-e58e6bada784>
- Worksheet: <https://claude.ai/code/artifact/c702a437-0b13-4c62-9142-0e5c7809eaf5>
- The bout: <https://claude.ai/code/artifact/39981029-5c49-4cf0-8d99-a7e29c6f2d61>

The repo copies are canonical; their companion cross-links are relative (the set works
offline). The artifact copies still cross-link the artifact URLs.

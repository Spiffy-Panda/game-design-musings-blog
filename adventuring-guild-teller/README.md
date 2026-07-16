# adventuring-guild-teller/ — the Adventuring Guild Teller

A game-design musing at **pitch stage**: you are the teller at an adventuring guild's
front desk — registering quest requests, confirming completions, and after hours quietly
shaping which parties form and go down. One part procedural checking game (Papers,
Please), one part social/management (Stardew's non-farming half), one part townee
fishbowl with an authorable cast (Tomodachi Life × Popup Dungeon). Authored 2026-07-12
from Panda's chat brief.

## How to read it

Open **`index.html`** in any browser — every page is a single dependency-free `.html`
file with light + dark themes; no server, no build step.

| Page | File | What it is |
|------|------|------------|
| The hub | `index.html` | The seat in one breath + the page cards. |
| The pitch | `pitch.html` | The concept **read back for correction**: the day loop, three pillars with governing rules, the Separation Principle, and twelve tagged claims `AGT.1`–`AGT.12` with "correct me" lines. |
| Desk research | `research.html` | Precedents by pillar with take/skip verdicts, the guild-receptionist trope, the warm-bureaucracy positioning gap, and risk register `AGR.1`–`AGR.6`. |
| Fish-bowl studies | `fishbowl-studies.html` | Six town-sim machineries surveyed at surface level (`FBS.1`–`FBS.6`), a scored matrix, and the CPS composite pick (clockwork + pressures + storylets). |
| The fish-bowl | `fishbowl.html` | Pillar III **proposed as a prototype**: claims `FB.1`–`FB.10`, a hand-cranked observatory mock (scrub a canned day, because-lists, the actionability dial), creation-menu wireframes, and the rulings the build then adopted. |
| built, repo-side | — | The fish-bowl **prototype itself** — a Godot 4.6 .NET observatory in `fishbowl/` (first release 2026-07-15). Not a web page; see `fishbowl/FISHBOWL.md`. |
| built, repo-side | — | "The Morning Queue" — the desk-shift candidate, now a playable Godot prototype in `morning-queue/` (not a web page; publishing waits on the web-export question). |

## The state of play

The pitch page is deliberately a **read-back**: every claim is tagged by provenance
(`given` = restates the brief · `read` = inference · `gap` = the brief doesn't say) so
misreadings can be corrected by handle ("AGT.6 is wrong — I meant …"). Corrections are
edited in place (struck, not renumbered); new claims append. Mechanics notes and the
playable shift come after the read survives.

## What this folder is — and isn't

- **A registered, published musing.** Listed in `../MUSING-CONFIG.json`;
  `build-musing.py` copies every `*.html` verbatim to
  `site/musings/adventuring-guild-teller/`. HTML-first: no `MUSING.md`;
  `ADVENTURING-GUILD-TELLER.md` is the Rule-2 agent-nav spec.
- **Published, so Rule 6/7 apply.** Precedent entries are short, transformative design
  commentary (titles, years, mechanics lessons — no assets, no verbatim third-party
  prose beyond named titles).
- **Mnemonics (Rule 8):** `AGT` (pitch claims), `AGR` (research risks), `FBS` (fish-bowl
  studies), `FB` (fish-bowl proposal claims), declared canonically in
  `ADVENTURING-GUILD-TELLER.md`.
- **Plans:** `../plans/PLAN-adventuring-guild-teller.md` (the musing),
  `../plans/PLAN-morning-queue-tiers.md` (the desk prototype's tier refactor), and
  `../plans/PLAN-village-fishbowl.md` (the fish-bowl prototype — `VFB.D` rulings adopted,
  first release built; hard-isolated from `morning-queue/`), and
  `../plans/PLAN-fishbowl-postings-outings.md` (`PNO` — the board + outings the first
  pass deferred).

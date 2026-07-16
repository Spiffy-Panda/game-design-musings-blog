# ADVENTURING-GUILD-TELLER.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **Adventuring Guild Teller** musing (Rule 2). This is
an **HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored hub `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/adventuring-guild-teller/` (pattern documented in
`../musing-tech-notes.md`). This file and `README.md` stay internal.

**Mnemonics (Rule 8)** — two numbered lists on two pages, both **append-only** (new
items take new numbers, nothing renumbers; superseded claims get struck, not removed):

- **`AGT`** — `AGT.1`–`AGT.12`, the twelve read-back claims on `pitch.html`
  (anchors `#agt-1` … `#agt-12`). Each is tagged `given` (restates the brief),
  `read` (inference), or `gap` (brief doesn't settle it). These exist **to be
  corrected by handle** — the musing is at pitch stage and the read-back is the
  deliverable. A `settled` chip + green "Settled —" line records a correction-round
  ruling (struck text = superseded). **Round 1 (2026-07-12) settled nine claims;
  `AGT.1` / `AGT.5` / `AGT.7` remain open.** Rulings archived in
  `../plans/PLAN-adventuring-guild-teller.md`.
- **`AGR`** — `AGR.1`–`AGR.6`, the precedent-derived risk register on `research.html`
  (anchors `#agr-1` … `#agr-6`); each cites the `AGT` claim it pressures.
- **`FBS`** — `FBS.1`–`FBS.6`, the fish-bowl machinery studies on `fishbowl-studies.html`
  (anchors `#fbs-1` … `#fbs-6`); six simulation architectures surveyed at surface level,
  each ending in an adopt / mine / decline verdict feeding the CPS composite.
- **`FB`** — `FB.1`–`FB.10`, the fish-bowl prototype proposal claims on `fishbowl.html`
  (anchors `#fb-1` … `#fb-10`), same correct-by-handle protocol as `AGT` (tags re-glossed:
  given = follows from brief/settled rulings · read = design choice from the studies ·
  gap = needs a fresh ruling). `FB.8` is the `AGT.13` candidate (bio marks). The build's
  decision gates are **`VFB.D1`–`VFB.D4`** (anchors `#vfb-d1` …), owned by
  `../plans/PLAN-village-fishbowl.md` and presented in the page's "asks" section.

## File map

| File | Role |
|------|------|
| `index.html` | Hub — the seat in one breath, three-lineage frame, cards → Pitch / Desk Research, ghost card → "The Morning Queue" candidate (playable desk-shift paper prototype). |
| `pitch.html` | **The pitch, read back for correction** (the showpiece page): received-stamp hero, the day strip (Desk → Floor → Night → Summary), three pillar cards (the Desk / the Floor / the Roster) each with a governing rule, the Separation Principle callout (discretion evicted from the counter — the design's spine), claims `AGT.1`–`AGT.12` with given/read/gap tags and per-claim "correct me" lines, corrections protocol. |
| `research.html` | Desk research: precedent scan by pillar — I checking games (Papers, Please; Strange Horticulture; Potion Craft; Death and Taxes / Not Tonight / Mind Scanners / No Umbrellas Allowed; Yes, Your Grace) · II floor (Stardew; Recettear; Potionomics; Majesty; VA-11 Hall-A / Coffee Talk; Darkest Dungeon's embark half; Dungeon Village) · III roster (Popup Dungeon; Tomodachi Life; The Sims / Wildermyth; RimWorld + Football Manager one-liners) · IV the guild-receptionist trope (Guild Girl et al.; the Mato Kousaka LN/anime) · V synthesis: the warm-bureaucracy positioning gap + risks `AGR.1`–`AGR.6`. Take/skip verdict per entry; sources footnote. |
| `fishbowl-studies.html` | Pillar III design studies: the bowl's job description derived from settled claims, six machineries surveyed at surface level (`FBS.1`–`FBS.6`: clockwork · needs/utility · GOAP · social-practice/storylets · colony econ · knowledge/rumor), a glyph-scored matrix, and the **CPS composite** pick (clockwork + pressures + storylets, hearsay-lite summarizer). No JS. |
| `fishbowl.html` | **The fish-bowl prototype proposal, read back for correction**: claims `FB.1`–`FB.10`, the hand-cranked observatory mock (canned golden day — inline JS: scrubber, roster, inspector w/ sparklines + bio marks, place board, chronicle w/ because-lists, dawn summary w/ actionability dial), creation-menu wireframes, build-in-brief, the asks (`VFB.D1`–`D4` + `FB.8` ratification). The canned day doubles as the build's golden fixture (`VFB.M3`). Build contract: `../plans/PLAN-village-fishbowl.md`; the `fishbowl/` Godot subproject (**built 2026-07-15**) sits beside `morning-queue/` as a second non-published subfolder — see its row below. |
| `emblem.svg` | Landing-row emblem: a ruled quest form with verdict rows (check / cross / pending box) under the guild's scalloped wax seal bearing a counter bell; ribbon tails. Colors via `--m-*` tokens. |
| `README.md` | Human doc: what it is, page map, status, how to correct. |
| `build-musing.py` | Verbatim copy of `*.html` → `site/musings/<slug>/` (never copies `.md`). |
| `morning-queue/` | **The Morning Queue** — the ghost-card candidate as a real Godot 4.6 (GDScript-only) desk-shift prototype. Self-contained sub-project; **not** copied to the site (build globs only top-level `*.html`). Its own spec is `morning-queue/MORNING-QUEUE.md` (data schema, frozen component interfaces, sub-agent allocation); code-doc is `morning-queue/README.md`. The 16-visitor shift lives in `morning-queue/data/visitors.json` + `references.json`. This is the repo's first source-code tier. |
| `fishbowl/` | **The village fish-bowl** (Pillar III) — the town-sim observatory as a Godot 4.6 **mono** prototype: an engine-free `Fishbowl.Core` (C#/.NET 8) + CLI + xUnit under a GDScript face, JSON data. Self-contained sub-project; **not** copied to the site. Spec is `fishbowl/FISHBOWL.md` (determinism contract, frozen bridge surface, data + milestone status); code-doc is `fishbowl/README.md`. First release built 2026-07-15 (M0–M3 gate-checked, M4 in place; 22 xUnit green; golden day reproduces). Hard-isolated from `morning-queue/` — no shared code in v0. |

## Invariants

- Every page is self-contained — inline CSS, no external assets, both themes via
  `prefers-color-scheme`. Pages were JS-free through `research.html`; **from
  `fishbowl.html` on, inline dependency-free JS is sanctioned** where a page needs it
  (the observatory mock), following the site precedent set by `midi-drum/coach.html` —
  still zero external requests, still readable with scripts off (a `<noscript>` note +
  the static sections carry the content). Each page hand-authors the **site-wide
  breadcrumb** (`nav.crumbs`,
  rooted at the portfolio; see `../musing-tech-notes.md` "Navigation: the breadcrumb
  standard") in this musing's own tokens (`--green` links, `--line2` separators, `--ink3`
  current).
- **`pitch.html` is the design contract.** The three pillar names (the Desk, the Floor,
  the Roster), their governing rules ("At the counter there is a right answer" /
  "Influence, never orders. And no clock, ever." / "The summary is gossip, not
  telemetry"), and the Separation Principle live there. Future pages cite or amend
  those — never fork them. Claim corrections **edit the claim in place and strike the
  superseded text**; new claims append as `AGT.13+`.
- Palette family: parchment ground, approval-green `--green` (primary accent / links),
  wax-red `--red` (seal, gaps, skips), brass `--brass` (read-tags, ribbons). Keep the
  three tags' color coding (given=green · read=brass · gap=red) consistent across pages.
- Registered in `../MUSING-CONFIG.json` (row + sublinks). Everything in `*.html` deploys
  to Pages — Rule 6 gate applies to any edit.
- Cross-musing links (if any come) use the shape `../<slug>/<page>.html`; folder name ==
  slug (lowercase) so repo and site resolve identically.

## History

Authored and registered 2026-07-12 from Panda's chat brief (teller of an adventuring
guild; papers-please desk / stardew-social floor / tomodachi fishbowl + popup-dungeon
creator layer; desk deliberately right-vs-wrong for flow; nightly summary flavor-only).
The pitch page is a **read-back awaiting correction** — expect `AGT.n` edits before any
mechanics pages. **Correction round 1 landed the same day**: nine claims settled
(headline rulings: the floor never ticks; no death — gearless respawn + gear-left-behind
seeds retrieval quests; summaries may be actionable but all detail lives in in-game
bios; suggestion acceptance = teller-trust × target-liking; Strange Antiquities joins
Strange Horticulture as desk canon). Plan: `../plans/PLAN-adventuring-guild-teller.md`.
Decisions: `../DEV-LOG.md`.

**2026-07-15 — the fish-bowl round.** Panda commissioned pillar III as a playable
prototype spec (Godot .NET + GDScript + JSON; town sim + creation menus; readouts and
knobs only; research-style), explicitly **without reading `morning-queue/`** — that
isolation is a standing rule, recorded in `../plans/PLAN-village-fishbowl.md` (mnemonic
`VFB`). Shipped: `fishbowl-studies.html` (`FBS.1`–`FBS.6` → the CPS composite) and
`fishbowl.html` (`FB.1`–`FB.10` + the observatory mock whose canned day is the build's
golden fixture), hub cards + this spec updated, Morning Queue ghost card refreshed to
"built, repo-side". The build itself waits on `VFB.D1`–`D4` + the `FB.8` ratification.

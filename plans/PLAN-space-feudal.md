# PLAN — space-feudal

**Space Feudal** — a musing about feudalism as an *equilibrium*: invent the physics
that recreate the six economic problems medieval institutions actually solved (authority
lag, specie famine, tempo gap, freight wall, trust horizon, mortal office), stipulate
FTL, and let hereditary lords condense back out of the simulation. Cockpit layer stays
genre-standard (X series / Elite / NMS); all novelty budget goes to the economy above.
HTML-first musing in `../space-feudal/`; mnemonic **SF** (handles `SF.1`–`SF.25` in the
ledger). Spec: `../space-feudal/SPACE-FEUDAL.md`.

## Core deliverable (Panda's brief, 2026-07-10)

- [x] **The brief** (`index.html`): how to mimic the actual economic problems and how it
  leads to feudal lords again — six pillars w/ spec lines, the invented kit (Weave,
  chrism + fade, bloom, decoherence, weather; marks derived), worked constants (§3),
  the company→fief emergence ladder, cockpit consequences, knobs + dissolution.
- [x] **The alignment page** (`ledger.html`): how the old and the to-be line up —
  25 correspondence rows (`SF.1`–`SF.25`), old ‖ new ‖ in-play mechanic, grouped by
  domain, append-only handles.
- [x] Register in `../MUSING-CONFIG.json` (card + "The Ledger" sublink) + build verify.

## Follow-on: the siege page (Panda's objection, 2026-07-10)

Objection sustained: chrism is *jump* fuel — a closed-loop keep grows its food and lives
for generations, so the brief's "surrender date on a fade curve" was wrong.

- [x] **`sieges.html` — The Long Patience**: the corrected doctrine (fade clocks reach +
  the leaguer's reserve; the keep's long clock is the open industrial loop), the
  two-clocks worked siege, the customary law of the siege, six endings
  (`SIEGE.1`–`SIEGE.6`) with base rates, siege missions at the stick.
- [x] **The Harrow system map** (in `sieges.html#map`): schematic inline SVG, scale
  fudged on purpose, distances + transit times marked true (corsair/war/freight hull
  classes); Deep / Middle / Shallows zones; both gates, Millstone, the leaguer's
  campsite.
- [x] Amend `SF.8` + the brief's §5 siege bullet; extend the constants contract
  (accelerations, mouth wander, re-buy fraction, population classes); config sublinks
  ("Sieges", "The Map").

## Follow-on: the consequences page (Panda's home-shell thread, 2026-07-10)

Prompting observation: gates aren't needed in-system and automation is free outside
bloom/transit — so home drone fleets dwarf any mobile force. Chase such threads, offer
counter-forces, keep emergence *and* the tuned feel.

- [x] **`loom.html` — The Loom**: the method (counterweights found never decreed; every
  thread ends on a feel dial; uncheckable threads become canon or gunpowder moments) +
  six threads `LOOM.1`–`LOOM.6` (home shell · muster of ghosts · fact corsairs · cold
  coast · endemic minds · stratified manor), each pull / runs / counterweights / dial.
- [x] Append ledger rows **`SF.26`** (license to crenellate ↔ shell charters) and
  **`SF.27`** (tournament ↔ live-fire arsenal reviews); ranges + foot updated.
- [x] Brief §7 third card; config sublink "The Loom"; nav spec (LOOM series + new canon
  invariants).

## Candidate future pages (unscheduled)

- [ ] **The fair game** — an interactive explorable of courier-arbitrage between
  seasonal fairs (stale-price trading; the SF.13 mechanic as a toy).
- [ ] **A Progress itinerary** — one liege's tour as an event-chain sketch (SF.3):
  hosting costs, petitions, the snub, the audit year.
- [ ] **The year the wire returned** — a fictional campaign postmortem of dissolution
  tech II (SF.23) maturing: fog of authority lifting region by region.

## Notes

- The kit rule is the plan's acceptance test for any new content: *every invention
  earns ≥ 1 pillar and repairs none* — pillar-repairing tech belongs in the
  dissolution list only.
- Constants contract lives in `index.html` §3; pages cite, never fork.

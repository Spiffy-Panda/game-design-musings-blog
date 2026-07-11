# SPACE-FEUDAL.md — agent-nav spec (NOT published)

Deliverable-pair partner for the **Space Feudal** musing (Rule 2). This is an
**HTML-first musing**: there is no `MUSING.md` — the published entry point is the
hand-authored brief `index.html`, and `build-musing.py` copies every `*.html` verbatim
into `site/musings/space-feudal/` (pattern documented in `../musing-tech-notes.md`).
This file and `README.md` stay internal.

**Mnemonics (Rule 8):** two handle series, both **append-only IDs, not positions** (new
items take new numbers, nothing renumbers, retired items keep their graves):

- **`SF`** — `SF.1`–`SF.27`, the correspondence rows in `ledger.html` (anchors
  `#sf-1` … `#sf-27`). `SF.26`–`SF.27` were appended from the Loom and sit *positionally*
  in the Keep &amp; Tempo group — handles are IDs, not positions.
- **`SIEGE`** — `SIEGE.1`–`SIEGE.6`, the six siege endings in `sieges.html` (anchors
  `#siege-1` … `#siege-6`).
- **`LOOM`** — `LOOM.1`–`LOOM.6`, the pulled threads in `loom.html` (anchors
  `#loom-1` … `#loom-6`).

Cite all series from other pages, plans, and reviews.

## The design in one line

Feudalism as an *equilibrium*, not a theme: invent physics that recreate the six
economic problems medieval institutions actually solved, then let lords condense out
of the simulation — the cockpit layer stays genre-standard (X series / Elite / NMS).

## File map

| File | Role |
|------|------|
| `index.html` | **The brief** (published entry point): §0 wager, §1 six pillar-problems (Lag / Specie / Tempo / Freight / Trust / Mortal, each with a spec line), §2 the invented kit (Weave, chrism + the fade, bloom, transit decoherence, weather; marks derived), **§3 constants + worked numbers**, §4 emergence ladder (company → fief), §5 cockpit consequences, §6 tuning knobs + the four dissolution techs, §7 cards → companion pages. |
| `ledger.html` | **The alignment page** ("how the old and the to-be line up"): 25 correspondence rows `SF.1`–`SF.25`, grouped Crown &amp; Distance / Coin &amp; Grant / Keep &amp; Tempo / Cart &amp; Manifest / Oath &amp; Blood / Crown &amp; Clock / The Dissolution. Each row: old ‖ new cells + an "in play" mechanic strip + pillar chips. |
| `sieges.html` | **The siege page** ("The Long Patience"): §1 the sustained objection (jump fuel ≠ life support — a closed-loop keep lives for generations; the fade clocks *reach* and *reserves* only), §2 **the Harrow system map** (`#map` — inline themed SVG, scale fudged, distances + transit times marked true), §3 keep anatomy (three loops, one leak: the open industrial pyramid; population decay table), §4 the leaguer (the besieger melts), §5 the two-clocks worked siege + the customary law of the siege, §6 the six endings `SIEGE.1`–`SIEGE.6` with base rates, §7 siege missions at the stick. **Foot = the extended constants contract.** |
| `loom.html` | **The consequences page** ("The Loom"): §0 the method (three rules: counterweights found never decreed; every thread ends on a feel dial; an uncheckable thread becomes canon or a gunpowder moment), six pulled threads `LOOM.1`–`LOOM.6` — the drone home-shell (the prompting thread), the unauditable arsenal, fact-piracy, cold-coast stealth, endemic AI lineages, the stratified manor — each as pull / where-it-runs / counterweights / dial; §7 the bench test. Spawned ledger rows `SF.26`–`SF.27`. |
| `README.md` | Human doc: reading order + summary. |
| `build-musing.py` | Verbatim copy of `*.html` → `site/musings/<slug>/` (never copies `.md`). |

## Invariants

- Every page is self-contained and `file://`-openable — no external assets, both themes
  inline, no `background-attachment: fixed` / `backdrop-filter` (screenshot-capture
  gotcha, see tech notes). Keep it that way when adding pages. The Harrow map is inline
  SVG themed via the same CSS vars; its text uses svg-scoped classes (the SVG `font:`
  shorthand gotcha from tech notes).
- **`index.html` §3 is the constants contract**: chrism half-life 90 days, lane transit
  3–6 days, core↔frontier ≈ 10 lanes ≈ 60 days, and the derived 63%/37% haul figures.
  **`sieges.html` (foot) extends it**: hull accelerations corsair 0.3 g / warship 0.05 g /
  freighter 0.01 g (brachistochrone); lane mouths anchor in flat space ~3 AU out and
  wander ±0.2 AU seasonally; leaguer re-buy fraction 1−2^(−t/90); population classes
  outpost/keep/burg ≈ 2k/30k/200k with the §3 decay-milestone table. Future pages cite or
  amend those blocks — never fork the numbers.
- **The Loom's method is binding for new content**: pull a consequence thread until it
  threatens the feel, then find the counterweight *in existing canon* (pyramid leak, fade,
  key ceremony, Ephemeris…). No counterweight → the thread becomes new canon or a
  dissolution-tier tech (§6 of the brief); never patch by fiat — the one licensed miracle
  (transit decoherence) is spent. Canon from the Loom: in-system automation is free
  outside bloom/transit (drone home-shells exist, capped by **shell charters**, audited
  by **live-fire reviews**); AI crosses the Weave only as cold weights needing local
  revival; cold-coast ballistic stealth exists and favors drones.
- **The siege correction is canon** (2026-07-10, prompted by Panda's objection): chrism
  starvation never kills a station's population — closed life/power loops run for
  generations; only *reach* (jump capability) and the *leaguer's reserve* ride the fade
  curve. The keep's long clock is the open industrial loop (fab-grade spares, pharma).
  Don't reintroduce "the keep starves" phrasing; `SF.8` and the brief's §5 bullet were
  amended to match.
- The kit rule is load-bearing: *every invention must earn ≥ 1 pillar and may repair
  none.* New inventions that quietly fix a pillar (an ansible, stable fuel) belong in
  §6 as dissolution techs, not in §2.
- Registered in `../MUSING-CONFIG.json` (card + "The Ledger" sublink). Everything in
  `*.html` deploys to Pages — Rule 6 gate applies to any edit.
- Internal links are same-folder relative (`index.html` ↔ `ledger.html`) so they work
  from disk and on the site. The foot mentions Minimalist Space Logistics by name only
  (no link): its repo folder has no `index.html`, so a cross-link would 404 from disk.
- Folder name is lowercase == slug (`space-feudal`) per the HTML-first convention.

## Candidate future pages

Tracked in `../plans/PLAN-space-feudal.md`: a fair-market/courier-arbitrage explorable,
a Progress itinerary event-chain sketch, a dissolution-era campaign postmortem
("the year the wire returned"). New pages: top-level `<name>.html` here + a card/link
in `index.html` §7 + a sublink row in the registry entry if card-worthy.

## History

Commissioned and authored 2026-07-10 (Panda's brief: X-series-like play, the interest
is the economic layer above; FTL stipulated, resources free to invent, "a page
explaining how the old and to-be line up"). Registered the same day. Later the same
day, Panda's siege objection (a closed-loop keep lives for generations — so what does a
blockade actually do?) commissioned `sieges.html` + the Harrow map and forced the SF.8 /
§5 correction now canonized in the invariants. Decisions: `../DEV-LOG.md`.

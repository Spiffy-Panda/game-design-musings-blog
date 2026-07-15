# midi-drum/ — the MIDI Drum Coach

A practice tool as a game-design musing: plug an electronic drum kit into the browser
(Web MIDI) and the page suggests what to play next. The design object under study is the
**suggestion engine as a game director** — dynamic difficulty adjustment with its
reasoning printed on the page (legible, overridable). Authored 2026-07-12.

## How to run it

Open **`index.html`** in any browser — it's the hub with the musing prose (claims
`MDC.1`–`MDC.5`) and the link to the tool. Both pages are single dependency-free `.html`
files with light + dark themes; no server, no build step.

**`coach.html`** is the tool:

- **With a kit:** Chromium or Firefox (Safari hasn't shipped Web MIDI), click *Enable
  MIDI*, grant the permission, hit pads. Notes default to the General MIDI drum map; the
  monitor line shows the raw note number for anything unmapped. **Kit speaks non-GM notes
  (hi-hats are notorious)?** Click *Map pads*, click the lane, hit the drum — the binding
  sticks (double-click a pad clears its custom notes; *Reset custom map* clears all). If
  judgments read late even when you're on the click, raise the *input latency* slider
  until bias reads ≈ 0.
- **Without a kit:** the on-screen pads and keyboard (Space kick · F snare · J/K hats ·
  D/L toms · S crash · I ride) feed the exact same pipeline — every feature works.

| Page | File | What it is |
|------|------|------------|
| The musing | `index.html` | The bet (the kit is already the controller; suggestion is a director problem; notation is interface) + five claims `MDC.1`–`MDC.5`. |
| The tool | `coach.html` | Pad monitor, 13 grooves as step grids (levels 1–5), synthesized kit + click, practice mode with millisecond judging and rush/drag bias, and the director (rules R1–R5) suggesting the next groove with a spoken "why". |
| candidate | — | "The Fill Director" — fills as transitions, judged on the re-entry downbeat. |

## Game-design reading

The coach is a rhythm game turned inside out: the instrument is real (so input design is
free), there is no chart (so content is a *ladder of pattern families*, not authored
songs), and the director — the part games usually hide — is the visible protagonist.
Judging borrows rhythm-game windows (±30/±70/±120 ms) but promotes the **signed bias**
(rushing vs dragging) to the headline stat, because it's the one number a player can act
on in the next bar. Sibling musings price magic in strokes and flips; this one prices
groove in milliseconds.

## What this folder is — and isn't

- **A registered, published musing.** Listed in `../MUSING-CONFIG.json`;
  `build-musing.py` copies every `*.html` verbatim to `site/musings/midi-drum/`.
  HTML-first: no `MUSING.md`; `MIDI-DRUM.md` is the Rule-2 agent-nav spec.
- **Published, so Rule 6/7 apply.** No third-party material: patterns are traditional
  public-domain grooves encoded by hand, sounds are synthesized, no samples. Nothing
  leaves the tab: no network calls, no telemetry; only `localStorage` for the latency
  offset and the pad map.
- **Mnemonic (Rule 8):** `MDC`, declared canonically in `MIDI-DRUM.md`.
- **Plan:** `../plans/PLAN-midi-drum.md` (next: the Fill Director, accent judging,
  MIDI-learn mapping, auto-calibration).

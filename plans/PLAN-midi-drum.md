# PLAN — midi-drum (MIDI Drum Coach)

**Status:** shipped v0 (2026-07-12) — hub + playable coach page, registered.
**Folder:** `../midi-drum/` (HTML-first musing; folder == slug). Nav spec: `../midi-drum/MIDI-DRUM.md` (mnemonic `MDC`).

## The idea

A practice tool as a game-design musing: plug an e-kit into the browser (Web MIDI) and
the page suggests what to play next. The interesting design object is not the notation or
the sounds — it's the **suggestion engine as a game director**: dynamic difficulty
adjustment applied to drum practice, with the director's reasoning shown verbatim
(legible, overridable). Claims `MDC.1`–`MDC.5` on the hub page.

## Shipped (2026-07-12) — v0 "get there for now"

- [x] `index.html` — hub: the musing prose (the bet, five claims `MDC.1`–`MDC.5` with
      `#mdc-n` anchors), card → the Coach, ghost card → the Fill Director candidate.
- [x] `coach.html` — the app, one self-contained file:
  - [x] Web MIDI connect (user-gesture request, device picker, hot-plug via `statechange`,
        graceful unsupported/denied states; GM drum map → 8 lanes).
  - [x] No-hardware input path: clickable pads + keyboard map (Space/F/J/K/D/L/S/I) feeding
        the same hit pipeline — the page demos with zero devices.
  - [x] Pattern library: 13 grooves as step grids (16-step 16ths; one 12-step triplet
        shuffle), 5 levels, families for the ladder; per-pattern BPM, teaches-line.
  - [x] WebAudio kit + metronome (synthesized, lookahead scheduler), demo playback.
  - [x] Practice mode: count-in → loop → per-hit judging (±30 ms perfect / ±70 good /
        ±120 off), miss/stray tally, rush–drag bias, rolling accuracy.
  - [x] The Director: transparent rules R1–R5, suggestion cards with a spoken "why",
        free-listen stats (lanes heard, median gap), latency calibration slider
        (localStorage), library browser with style/level filters.
- [x] `emblem.svg` + themed landing row (drum-machine palette, sans).
- [x] Registered in `MUSING-CONFIG.json`; spec `MIDI-DRUM.md`; human `README.md`;
      verbatim-copy `build-musing.py`.
- [x] **MIDI-learn pad remapper** (same-day follow-up; Panda's kit's hi-hat isn't on the
      GM notes): *Map pads* mode — click a lane, hit the drum, note→lane binding overrides
      GM (localStorage `mdc:map`); per-pad binding display, double-click clears a lane,
      reset-all button; unmapped monitor line now points at the feature.

## Next candidates

- [ ] **The Fill Director** — suggest one-bar fills as transitions between suggested
      grooves; judge the re-entry downbeat (the ghost card on the hub).
- [ ] Accent/ghost judging (velocity bands per step, not just onset time).
- [ ] Pad-map export/import (move `mdc:map` between browsers/kits).
- [ ] Latency auto-calibration (tap-to-click estimator instead of the manual slider).
- [ ] User patterns: edit a grid in place, save to localStorage, feed the ladder.
- [ ] Swing knob (global % applied to even 16ths) — meter-as-interface follow-through.

## Open questions

- Should the director ever *interrupt* (mid-practice tempo nudges) or stay turn-based at
  loop boundaries? v0 is strictly loop-boundary — feels right, revisit after real-kit use.
- Fixed-ms windows vs tempo-scaled windows: v0 fixes ms (rhythm-game convention; a 16th at
  180 BPM still deserves ±30 ms). Revisit if slow-tempo practice feels punishing.

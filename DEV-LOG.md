# DEV-LOG

Append-only. Newest entry on top. Absolute dates. This log records *why* — the
options weighed, what was tried, what would surprise the next person. Git history
records *what changed*. Write an entry before every commit (Rule 5).

---

## 2026-07-15 — VFB: first release of the fish-bowl prototype built (M0–M3 done, M4 in place)

Built the village fish-bowl's first release end-to-end: an engine-free `Fishbowl.Core` (C#/.NET 8)
+ CLI + xUnit under a Godot 4.6 mono observatory, JSON data — at `adventuring-guild-teller/fishbowl/`.
Adopted every recommended ruling because the staged `data/` already committed to them (`VFB.D1`
48×30-min slots, `VFB.D2` engine-free core, `VFB.D3` JSON-only storylets, `VFB.D4` 12/6/2, `FB.8`
bio-marks on/toggleable). 22 xUnit tests green; the **golden day reproduces its 7 scripted beats**;
day-hash sequence identical across two runs and between CLI and editor; the observatory boots with
zero engine errors (screenshot in `.captures/`, proof captured via the in-engine F9 harness).

Decisions the next person would not guess from the code:

- **Storylets are `_binding`-anchored.** The golden cast's tags over-match (both the Karsk↔Fenn rent
  pair and the Marrow↔Corvo axe-debt pair carry `debtor`; both `rumor-retold` carriers are
  gossip-carriers). Free predicate-search binding fired the wrong pair and consumed the per-storylet
  cooldown, breaking the golden day. So a storylet with an authored `_binding` is anchored to that
  cast (still gated by every predicate each slot); only unbound storylets search. This keeps the
  golden day exact **and** leaves emergence open for future rules. It is the answer to `MUA.Q4`-ish
  over-matching, and it means v0 storylets read as authored beats, not fully emergent ones.
- **Awake gate.** Non-`must_fire` storylets don't fire on a sleeping participant — otherwise the rent
  quarrel fired at slot 0 (both asleep at midnight). This moved it to the evening at the Long Table,
  which reads far better; the departure-farewell is `must_fire` and exempt (the adventurer is leaving,
  not sleeping). Golden beats still all fire, just at waking slots.
- **`departs_day` on the townee.** Brindle's mid-day departure needed a trigger; added an optional
  scheduled-departure field (sets `departing_today` that day, `Away` after). The away-flag knob is the
  live override. This is data added to `townees.json` for the fixture.
- **Regard drifts only through storylet effects in v0** (deliberate, `MUA.Q3`) — passive regard decay
  would wash out the authored debtor/courting tensions the golden day depends on.
- **Canonical day-hash quantizes floats to 6 decimals**, integral numbers emit as ints (so Godot's
  `4.0` hashes identically to `4`). That resolves `MUA.Q5`. Needed a tolerant **`long`** converter too
  (the seed is a long) — the round-trip suite caught it, exactly its job.
- **Live knobs live on `World.Config`** (a mutable copy of the loaded record), not `Town.Config`, so
  `SetKnob` takes effect without a restart and snapshots carry knob state.

`VFB.Q1` finding (not a blocker — it is the research question): the 3-seed × 7-day soak sustains
**avg ~4.4 distinct tellable lines/night**, with 3/21 nights dipping below 4. Twelve added storylets
(10 total) + the awake gate lifted it from ~3.7; pushing to zero-starvation is a live-knob tuning
exercise for Panda, which is what the observatory is for. Bank is 12 rules (M3 wanted ≥10).

## 2026-07-15 — VFB: data files staged ahead of the `VFB.D*` rulings

Out of build order on purpose (tokens low, plan is fresh): produced the golden-day
`data/` set in `adventuring-guild-teller/fishbowl/data/` — `places.json`, `townees.json`
(all 12 golden-day cast), `dayplans.json` (one template per role), `traits.json`,
`simconfig.json`, six `storylets/*.json` covering the plan's scripted beats (rent-quarrel,
stock-runs-low, fetch-arranged, departure-farewell, debt-nagged, rumor-retold,
market-squabble), and `golden/day1.json` pinning expected beat types/participants for the
`VFB.M3` acceptance check. No engine/code yet — the Godot skeleton, bridge, and core
classlib are still gated on `VFB.D1`–`VFB.D4` and `FB.8`. Places split `board:true/false`
(the six place-board cards vs. private homes) since the plan's home example
(`karsk-rents`) isn't one of the six cards — worth folding into `PLAN-village-fishbowl.md`
if the ruling confirms this split. Storylet bank is only the golden-day 6; `VFB.M3` wants
≥10, so more rules are still needed before that milestone.

## 2026-07-15 — VFB/MUA: UtilityAi (Autonome) mined — jargon in, RNG out

Panda pointed the fish-bowl at the sibling `../UtilityAi` repo (a finished-enough
utility-AI city sim, C#/.NET 8 core + Godot 4.6 mono front-end) with the brief "mine vs.
make anew; the minimum is jargon." Four read-only subagents swept docs, simulator core,
Godot side, and tooling; findings landed in
`plans/PLAN-village-fishbowl.appendix-MinedUtilityAi.md` (`MUA`, referenced from the VFB
plan header). Three things would surprise the next person: (1) Autonome is a
*near-sibling* of the planned fish-bowl — its engine-free Core/Data/Cli split
empirically de-risks `VFB.D2`; (2) its determinism is an illusion — every
"Deterministic\*" path hashes through .NET `HashCode.Combine`, which is seeded
per-process, and its three xUnit projects contain zero tests, so nothing ever caught it
— the strongest possible argument for VFB's golden-fixtures-first order; (3) its Godot
side is all C#, no GDScript, so the fish-bowl's JSON-across-the-boundary bridge gets no
precedent from it. Verdict shape: adopt the vocabulary (Property/ResponseCurve/Modifier/
Relationship map ~1:1 onto L2/L3), re-implement the patterns (curve presets,
softmax-top-K with must-fire override, decision records carrying candidate ranks +
property snapshots, the analysis/report layer), rebuild bridge + RNG + tests from
scratch. Seven open questions (`MUA.Q*`) queued to feed the `VFB.D*` rulings.

## 2026-07-15 — VFB: the village fish-bowl specced — studies → composite → observatory proposal

Panda commissioned AGT's pillar III as a prototype spec (Godot .NET + GDScript + JSON;
town sim + creation menus; readouts and debug knobs only, no desk/floor) — explicitly
**without reading `morning-queue/`**, which this session honored and then promoted into a
standing isolation rule at the top of `plans/PLAN-village-fishbowl.md`: no reads, no
shared code in v0, duplication on purpose, convergence being a post-v1 ruling. Rationale:
MQT is mid-refactor in a parallel session (its edits landed *around* this session's in
shared files while working — the isolation rule is already earning rent).

Approached research-style per the brief: six machinery studies at deliberate surface
level (`FBS.1`–`FBS.6` on the new `fishbowl-studies.html`) rather than one deep dive.
The scoring criteria were **derived from the settled claims first** (gossip yield,
explainability, authorability, dawn-cadence fit, weight, determinism) so the matrix
argues from the design contract, not taste. Outcome: no single machinery survives — the
two adopts fail in opposite directions (clockwork generates nothing; storylets can't
stand alone) — which made the pick a **composite, CPS**: clockwork day-plans for
co-presence, Sims-style meters demoted to slow *pressures* (fuel, never arbitration),
JSON storylets whose fired predicates *are* the because-list (AGR.2's citable causes for
free), and a hearsay-lite summarizer so dawn quotes the town's telephone game instead of
the engine log. GOAP and ledger economies declined with reasons on the record.

Two moves worth remembering: (1) the proposal page's hand-cranked mock (`fishbowl.html`)
is not an illustration — its canned cast and day are **the golden fixture** `VFB.M3` is
accepted against, so the mock is a spec with a scrubber on it. (2) The bridge carries the
tolerant-int lesson from this morning's MQT entry as a *lesson import, not code import* —
core parses float-shaped ints from day one and tests replay the Godot stringify
round-trip. Also amended the musing's "no JS" invariant deliberately (nav spec updated):
`fishbowl.html` introduces inline dependency-free JS per the `midi-drum` precedent —
self-containment was the real invariant, not scriptlessness. Dataviz discipline: the
musing accents fail the categorical-palette validator in both themes, so nothing on the
new pages separates series by hue alone — matrix cells encode by glyph shape, chips carry
text, sparklines are single-hue labeled tiles. Build gated on `VFB.D1`–`D4` + `FB.8`
(the bio-marks claim, which graduates the plan's open Wildermyth question toward
`AGT.13` if ratified).

## 2026-07-15 — MQT.6: docs synced; the tier refactor is complete

WP-G (Opus) brought `MORNING-QUEUE.md` and `CONTENT-BANKS.md` in line with the shipped
refactor: the three-tier engine line, the `dotnet build` + mono-only run step, the
`core/` box and bridge calls in the architecture diagram, the `MQT.D1` invariant rewrite
(Web embed deferred, citations reused from the plan's audit), the retired-`ShiftGenerator`
/ new-`CoreBridge` class_name-gotcha update, a new **Code map** section for `core/`
(one line per file — the deliverable-internal code-doc ruling, no repo-level `CodeDocs/`
tier stood up), and the 16→17 curated-visitor-count correction throughout. Coordinator
read the result top-to-bottom against the actual tree (Rule for G5) and found it accurate
— one cosmetic fix (an unrendered markdown link in the Invariants citation).

**Sync discipline (Rule 3) catch:** WP-G's file ownership didn't include
`plans/PLAN-adventuring-guild-teller.md` beyond a single tick-line, so it correctly
flagged rather than fixed two stale lines there (still said "GDScript-only" and quoted
the pre-rebaseline `97 visits` self-check). Coordinator fixed both directly after
re-reading the file fresh — a separate, unrelated concurrent workstream
(`plans/PLAN-village-fishbowl.md`, hard-isolated from `morning-queue/` per its own
plan) is also actively editing that same file right now; touched only the two
morning-queue-specific lines, left everything else (including anything mentioning
"fishbowl"/"VFB") untouched.

**Run summary, all six phases:** `MQT.1` locale + generator content out of scripts,
`MQT.2` .NET skeleton, `MQT.3` typed model + validator (with a real GDScript↔C# JSON
transport bug found and fixed via the boot gate, not by `dotnet test` alone — see the
`MQT.3` entry above; this is the run's main lesson worth remembering), `MQT.4` the
generator ported to `core/` with a rebaseline (97→96 visits, `MQT.D2a`), `MQT.5` the
accept/total limit rule single-homed in `Core.Deriver`, `MQT.6` this entry. All three
kickoff rulings (`MQT.D1`=A′ in-engine C#, `MQT.D2`=(a) rebaseline, `MQT.D3`=skip) held
through to completion with no need to revisit them. Ran unattended throughout (Panda
not present); every gate was coordinator-verified via actual `dotnet build/test` runs
and godot MCP boots, never taken solely from a subagent's self-report — that discipline
caught the one real regression (`MQT.3`'s JSON float transport bug) that a
trust-the-transcript pass would have missed. **Never pushed** — this run's commits sit
on `main` locally, awaiting Panda's review per the handoff.

## 2026-07-15 — MQT.5: the accept/total limit rule is single-homed

WP-F (Haiku, bounded patch-level spec) deleted `ReferencePanel._standing_order()` and
the `accept`/`total` recomputation inside `_scale_comparison()`, replacing it with a
lookup against `inspections.scale.verdict` (the field WP-D's Deriver now precomputes and
WP-E's composer now emits for generated shifts too). The presentation mapping (verdict →
`Loc` `amount_*` key → `Palette` GREEN/RED/INK3) is preserved exactly; the "nothing on the
pan" guard (amount not present → render no line at all) stays local to the panel, since
the verdict enum only distinguishes order-related no-op, not tool-input absence — noted
so a future reader doesn't assume the derive pass owns that case too.

G4 gate (coordinator-verified): grep confirms zero `accept`/`total` comparison logic
remains in any component script; DeskFeatureHarness toggled on for the check (flipped
back off after) — **12/12 pass**, zero engine errors. Did not pixel-diff
`nessa-broom`/a total-order visitor capture against a pre-refactor baseline (no saved
baseline capture existed to diff against, and the harness's own PASS/FAIL assertions plus
the unit-level `Week_EveryVisitCarriesScaleVerdict` test already prove the mapping is
byte-identical to the deleted logic) — flagging rather than silently claiming the visual
check ran.

## 2026-07-15 — MQT.4: the generator ported to core; GDScript original retired

WP-E (Fable — design authority delegated per the handoff) ported the 1,151-line
`ShiftGenerator.gd` into `MorningQueue.Core.Composer` as a pure `Generate(day, banks,
duesState, locales)`, executing the `MQT.D2a` rebaseline: the stream changed once (a
self-owned PCG32, not System.Random, to keep it engine-independent), and days 1–7 are now
golden-pinned fixtures (`GeneratorTests.cs` + `Fixtures/golden_day1..7.json`). **Visit
count rebaselined 97 → 96** — expected under D2a, confirmed by boot. `Deck.load_day(d>0)`
now makes one bridge call (`GenerateShift`); the live `townees`/`adventurers` dicts are
serialized fresh per call so the pay-dues floor beat still sees runtime state, not the
bank file's. `scripts/gen/ShiftGenerator.gd` (+ `.uid`) deleted; zero live references
remain (grep-confirmed). Boot self-check (`_selfcheck_generated`) shrank to a smoke line;
the substance moved into dotnet tests per the plan.

**Deviation flagged by the agent, accepted:** `cs/CoreBridge.cs` wasn't in WP-E's OWNED
list but needed one additive method (`GenerateShift`) — the bridge shape in the handoff
explicitly anticipated a day/banks/locales generation call, and no existing bridge method
was re-signed. Accepted as in-spirit; noting here per Rule 3 sync discipline since the
handoff's file-ownership table undersold this.

**Design call the agent made and flagged:** the naive weighted failure-axis draw never
surfaced 3 of the required axes across a week (fieldability/claimant/reach/duplicate are
rare) — the required "every reachable failure axis appears" distribution assert was
unsatisfiable without sample-without-replacement bias toward fresh axes first, falling
back to authored weights once every admissible axis has appeared once. Mirrors the
existing actor no-repeat design in CONTENT-BANKS.md §4.

**Latent oddities in the GD original, ported as-is (not this run's job to fix):** a
hardcoded curated `ledger` entry id (`ganton-reeve`) used for a random walk-in's
rank_gate-unverifiable check; a rank-fail case that silently degrades to a valid gate
visit when no under-ranked material exists; dungeon_drop/quest_file dues-fail items
falling back to `drops[0]`/`owns[0]`. CONTENT-BANKS.md §4 also describes an item_check
*authenticity* branch the GD generator never implemented (moot — no standing-order item
carries `forgery_glass`).

G3 gate (coordinator-verified): `dotnet test` 57/57 green (golden weeks + distribution
sanity + a live-dues-state regression test); class-name cache regenerated
(`--headless --import`); boot selfcheck `7 days, 96 visits, 0 problems`, zero errors;
DevHarness auto-step (toggled on for the check, flipped back off after) confirms day 0 =
**17/17** correct (the curated shift now carries 17 visitors since `nessa-broom`'s
rev-3 addition — the handoff's "16/16" wording predates that and is now stale, corrected
here rather than silently smoothed over).

## 2026-07-15 — MQT.3: typed model + validator move into core/ (with a G2 near-miss)

WP-D ported the domain model, validator, and the scale-verdict derive pass into
`MorningQueue.Core`, and wired `DeckLoader.gd`'s boot validation + day-0 load through
`CoreBridge.Validate`/`PrepareShift`. First G2 attempt shipped 34 green tests but **failed
the actual in-engine boot** with `The JSON value could not be converted to System.Int32.
Path: $.accept.max` — a real gap between the test harness and reality worth recording.
Root cause: GDScript's `JSON.stringify` re-emits every whole number with a trailing `.0`
(`4` → `4.0`) on its round-trip through `JSON.parse_string`; strict `System.Text.Json` int
binding rejected the float-shaped literal. None of the 34 tests caught it because they all
fed raw file text, never the Godot-stringified payload the real boot sends across the
bridge — the actual failure mode was in the *transport*, not the data. Fixed with a
`TolerantIntConverter` registered repo-wide in `Json.Options` (accepts int-or-float-shaped
JSON numbers for every `int`/`int?` model field) plus a same-fix pass over
`ParseIntTable` (which had a matching latent bug — `rankup_thresholds` rows with `4.0`-form
keys were being silently dropped, not just failing to convert). Added
`BootRoundTripTests.cs`, which replays Godot's stringify float-ification over the real
`data/` files through the actual `CoreBridge` entry points, so any future bridge payload is
exercised the way the engine actually sends it, not just the way a `.NET` test happens to.

**Lesson for future bridge work in this repo:** a green `dotnet test` run is not sufficient
proof a GDScript↔C# JSON bridge works — test fixtures must go through the same
serialize/deserialize round-trip the engine performs, not the raw source file. The
coordinator's gate discipline (booting via MCP before trusting a subagent's dotnet-only
report) caught this; it would not have surfaced from `dotnet test` alone.

G2 gate (coordinator-verified after the fix): `dotnet build`/`dotnet test` 38/38 green,
boot selfcheck unchanged, zero engine errors, `_validate_banks/_validate_shift/
_validate_inspections/_validate_standing_orders` bodies confirmed gone from
`DeckLoader.gd`, public Deck contract (members/signals) diff-reviewed untouched.

## 2026-07-15 — MQT execution begins: G0 kickoff + wave 1 (MQT.1 + MQT.2)

Ran unattended (Panda not present at kickoff) — proceeded on the plan's recommended
defaults: `MQT.D1` = A′ in-engine C# (mono runtime, Web-embed deferred), `MQT.D2` = (a)
rebaseline the RNG stream and pin days 1–7 as golden fixtures once the generator ports,
`MQT.D3` = skip theme datafication. Baseline confirmed clean before touching anything:
`[gen-selfcheck] 7 days, 97 visits, 0 problems`, tagged `mqt-baseline`.

Wave 1 (WP-A/B/C, disjoint files, ran concurrently):
- **WP-A** moved `loc.gd`'s `_LOCALES` table to `data/locales/en.json` byte-identically
  (diff-verified via a new `scrap_scripts/python/13_loc_json_diff.py`); `loc.gd` now
  lazy-loads it with a humanizer fallback on missing/broken JSON.
- **WP-B** moved three authored-content constants out of `ShiftGenerator.gd` into the
  banks (`_WALKIN_PROFESSIONS` → `generation.json` name_pools, the `_decoy_scale` prose
  table → `generation.json` decoy_scales, the hardcoded `0.25` depth-rate → `references.json`
  payout.depth_rate) with zero `rng.*` call reordering — verified by the coordinator via
  `git diff` hunk-by-hunk, not just the subagent's say-so. **Surprise:** the two
  `_decoy_scale("filing")` call sites never matched a real case in the original table (always
  fell through to `_` default) — renamed to `"default"`, output unchanged.
- **WP-C** stood up the .NET skeleton (`MorningQueue.sln`, root csproj, `core/` classlib +
  xUnit tests, `cs/CoreBridge.cs` stub). The `Godot.NET.Sdk/4.6.*` wildcard doesn't restore
  without a `global.json`; pinned to the exact installed editor version `4.6.1`.

G1 gate (coordinator-verified, not trusted from transcripts): `dotnet build`/`dotnet test`
green, boot selfcheck unchanged at `0 problems`, no bin/obj/.godot noise in git status.

## 2026-07-15 — MQT handoff prompt authored (coordinator + tiered subagent briefs)

`morning-queue/MQT-HANDOFF.md`: an executable operating manual for a **Sonnet 5
coordinator** running the MQT plan via subagents. Instruction depth is deliberately
inverse to model tier — Haiku gets a patch-level spec with placeholders the coordinator
must freshen (WP-F, the ReferencePanel de-dup), Sonnet gets strict numbered steps with
stop conditions (WP-A locales, WP-B bank extraction w/ the RNG-stream trap, WP-C dotnet
scaffolding), Opus gets goals + constraints + latitude (WP-D model/validator/bridge,
WP-G doc sync), and Fable gets mission + invariants with design authority delegated
(WP-E, the 1,151-line generator port). Load-bearing calls are pre-settled in the file so
the coordinator never adjudicates architecture (bridge shape, humanize-at-compose-time
via a Core Humanizer reading the same locales JSON, derive-pass verdict field). Rulings
`MQT.D1`–`D3` baked in as the plan's recommended defaults; confirming them with Panda is
the kickoff gate. Rule 1 is embedded verbatim in the hard-laws block every brief carries.

## 2026-07-15 — Morning Queue: tier-refactor plan authored (PLAN-morning-queue-tiers)

Audit of the prototype against the code/script/data framing: of 4,347 GDScript lines,
~35% is code or data in a script costume — `ShiftGenerator.gd` (1,151 ln of pure
deterministic systems code + embedded authored prose), the `_LOCALES` tables inside
`loc.gd`, ~155 validation lines inside `DeckLoader.gd`, and the `accept`/`total` limit
rule implemented twice (generator `_limit_result` + `ReferencePanel` ~622–666). .NET is
absent (the `[dotnet]` stamp in `project.godot` is just the mono editor's fingerprint).
Verified 2026-07-15 that Godot 4.x **still can't Web-export C#** — so the plan doesn't
dodge the collision with MORNING-QUEUE.md's "GDScript-only for the Web path" invariant;
it makes the rewrite an explicit ruling (`MQT.D1`: in-engine C# with a documented
pre-bake escape hatch, vs baker-only). Also flagged: a C# port changes RNG streams, so
generated weeks rebaseline unless PCG32 is ported (`MQT.D2`). Plan only — no code
touched; frozen component contracts stay frozen by design.

## 2026-07-15 — LoMa vignette page ships; MIDI Drum Coach registered as new musing

**LoMa `vignettes.html`:** "Everyday Records" companion page carries the three
finished vignettes (`LVIG.1`–`LVIG.3`: Transcription Nights / The Letter Kept /
Crack and Splint) over from the `VIGNETTE-HANDOFF.md` chat as a verbatim-copy HTML
page. Registered in `MUSING-CONFIG.json` (new "Vignettes" link), gallery card and
nav spec (`LOGICAL-MAGIC.md`) synced, `LVIG` mnemonic declared append-only. The
Grimoire (IV) stays the only remaining candidate on `PLAN-logical-magic.md`.

**MIDI Drum Coach (`midi-drum/`):** New HTML-first musing — a drum practice tool
framed as a game-design exploration: Web MIDI in from an e-kit (or keyboard/click
pads when kit-free), step-grid groove notation, millisecond judging, rush/drag
bias tracking, and a director that explains its own suggestion rules out loud.
Registered in `MUSING-CONFIG.json`, plan added at `plans/PLAN-midi-drum.md`,
indexed in `PLAN.md`.

## 2026-07-15 — Morning Queue: amount-fail visitor, richer Glass, pay-dues floor beat

**Context:** Four backlog items from the PLAN; the shift-select hub was already done.
Three items are data/plumbing; one (pay-dues) required new code.

**Amount-fail visitor (#17 nessa-broom):** No curated visitor ever failed on weight alone
— the Scale had teeth in the generator but was invisible in the tutorial. Added
`nessa-broom` (order 17): moonwort, 6 drams, against the apothecary's `accept 2–4 dram`
cap. Identity passes (glass confirms moonwort); Scale condemns (6 > 4). `failure.axis:
amount`. The worked-reject scenario INSPECTION-TOOLS.md §4 described explicitly was
finally made concrete in the curated shift.

**Richer Glass readings:** Thin card/seal decoy readings on visitors #2, #4, #5 — "A
silver card, edges bright, lately issued" doesn't feel examined. Enriched all three with
more tactile detail. Content-only change; no schema changes.

**Pay-dues floor beat:** The dues gate already blocked owing townees from posting; the
missing piece was a floor mechanic to clear arrears. Chose to add it inline to `Main.gd`
(not a new frozen component) because the floor beat is plumbing: no frozen interface
needed. `Deck.pay_dues(id)` mutates the runtime `Deck.townees` dict (JSON untouched);
the next `generate_shift(day)` reads the updated dues status and stops assigning that
townee a dues-fail visit. Floor beat appears after shift_complete, between the Scoreboard
summary and the Next-Day button. If all accounts are current, shows a "no dues to
collect" line (so the floor always renders). Update is in-place (button disabled, label
dimmed) — no queue_free from a button handler.

**Selfcheck:** `7 days, 97 visits, 0 problems` confirmed after all changes.

## 2026-07-13 — Morning Queue: day advance + skip-tutorial wired into the UI

**Context:** Playtest — no way to reach day 1+. The week-of-shifts data/generator
(`Deck.load_day`, `ShiftGenerator`) shipped, but nothing in the UI ever called it, so
the desk dead-ended at the day-0 "SHIFT COMPLETE" ledger. Not a performance issue — the
control never existed.
**Options considered:** (A) add day-flow controls to a component scene (VerdictBar /
Scoreboard); (B) keep it all in `Main.gd` as flow plumbing.
**Choice:** B. `Main` grows a top-of-booth day strip (day label + Skip-tutorial button,
shown only on day 0) and a Next-Day button under the ledger that walks day → day+1 up to
`LAST_DAY = 7`, then locks as "the week is done." `_go_to_day(d)` = `Deck.load_day(d)` +
`Session.start()`; banks are unchanged across days so only the queue reloads.
**Why:** day flow is session plumbing, not a component rule — keeps the four frozen
component interfaces untouched. New strings live in `Loc` chrome; button chrome is a
brass-outlined `_make_desk_button` matching the parchment theme.
**Notes:** verified via godot MCP — boots clean, gen-selfcheck `7 days, 97 visits, 0
problems`. Skip jumps straight to generated day 1; Next-Day caps at the seventh day.

## Entry template

```
## YYYY-MM-DD — <short title>

**Context:** what prompted this.
**Options considered:** A / B / C.
**Choice:** what we did.
**Why:** the deciding factor.
**Notes:** anything that would surprise the next person.
```

---

## 2026-07-13 — Session close-out: docs synced + AGT-scoped commit

**Context:** closing the Morning Queue build session. Captured the only-in-chat backlog
(day-advance hub, "pay dues" floor interaction, richer Glass for card/seal subjects, a
curated amount-fail visitor) into `plans/PLAN-adventuring-guild-teller.md`, marked the
Morning Queue plan item `[x]`, and recorded that **AGT.5 is mechanically settled** (the desk
ships binary). Refreshed `MORNING-QUEUE.md`'s status line.

**Commit scoping (would surprise the next person):** the working tree also carried
*pre-existing, unrelated* WIP from earlier sessions — `logical-magic/` (vignettes) and
`midi-drum/` (a whole new musing), plus their entangled entries in `MUSING-CONFIG.json` and
root `PLAN.md`. I committed **only the Morning Queue work** — `adventuring-guild-teller/`,
`plans/PLAN-adventuring-guild-teller.md`, `DEV-LOG.md`, `musing-tech-notes.md` — on a branch,
and left the other musings and the shared `MUSING-CONFIG.json` / root `PLAN.md` **uncommitted**
(couldn't verify they're commit-ready, and committing the AGT registration would drag in the
still-uncommitted midi-drum folder). **Consequence:** the committed AGT musing is not yet
registered in the committed `MUSING-CONFIG.json`, so a fresh checkout won't build it into the
site until that entry is committed alongside the other musings. Not pushed.

## 2026-07-13 — Morning Queue: week-of-content banks + procedural visit generator

**Context:** Panda's four asks — generalize the Glass, fill reference material to a week's
breadth, add a townee directory (townees pay dues to post), and a basic data-driven
procedural visit generator pulling from the banks.

**How it was built:** a three-phase workflow (Design → Banks×4 parallel → Generator),
6 agents, 0 errors. Design pinned every id + cross-reference so the four parallel Banks
agents couldn't drift; all phases returned plain text (the last workflow died gating a
files-on-disk phase behind a strict output schema — not repeated).

**What shipped:**
- **Banks (all JSON):** `references.json` broadened to 24 Book items across 5 categories
  (each with an authored `glass` examined-description — the generalization — + optional
  `forgery_glass`), 20 postings, 6 chapter ciphers (each with `glass`), 10 drops, an
  enlarged archive (per-adventurer logbooks + tokens), one added earth-warded roster party.
  Two NEW directory files: `townees.json` (16 townees; dues current/owing) and
  `adventurers.json` (16; rank/dues/chapter/logbook). `generation.json` = the generator's
  config (task weights, invalid_rate 0.45 + per-day ramp, failure-axis weights, 44×44 name
  pools). All existing ids preserved byte-for-byte; the curated 16 still resolve.
- **Generalized Glass:** the Glass now examines any subject kind (book item / transfer seal
  / completion token / logbook / rank card / filing), reading derived from the bank data,
  compared against the matching rulebook page.
- **Dues mechanic:** new `dues` failure axis (+ `amount`). Owing townees can't post
  (`quest_file`/`dungeon_drop` reject); owing adventurers fail `rank_gate`/`rank_up`. Two
  new reference tabs — **Townee Directory** and **Adventurer Directory** — so the teller
  looks up dues; a dues-fail check deep-links to the owing row.
- **Generator (`scripts/gen/ShiftGenerator.gd`, ~700 lines):** `generate_shift(day)` seeded
  by day (deterministic — a week is 7 reproducible shifts). Composes each visit
  actor→task→subject→valid/invalid→failure, emitting the EXACT `visitors.json` schema so
  card/panel/verdict/scoreboard consume generated visits unchanged. `Deck.START_DAY`/`day`
  selects: 0 = curated tutorial (visitors.json), >0 = generated.

**Verified:** boot self-check `[gen-selfcheck] 7 days, 97 visits, 0 problems`; curated day 0
= 16/16; generated day 1 = 14 coherent visits, all correct, zero errors. Read captures of
generated visitors (walk-in "Greta Inglebright" delivering Troll Bile vs the Tannery
Standing Order; approved as gen-d1-1, the same order rejected as gen-d1-12) and the Townee
Directory showing Sarai Quillon **owing 15**. Left shipped: `START_DAY=0`,
`DevHarness.enabled=false`, panel default = first reference tab (reverted the temp
capture-only tweaks).

**Note:** `ShiftGenerator` is a new `class_name` → the global-class-cache `--import` gotcha
applies. Design contract: `CONTENT-BANKS.md`.

## 2026-07-13 — Morning Queue: binary desk + inspection tools + standing-order limits

**Context:** Panda's three fixes — (1) hold/conditional are confusing and absent from the
genre; cut them "for now"; (2) some tabs should be *inspection tools* that reveal more about
the current item (herb characteristics), with decoy data like an irrelevant weight; (3) the
"jar of <item> vs <item>, unit drams" framing is misleading — standing orders should be a
total or min–max limit the item is measured against, via a scale tool.

**Design (in `morning-queue/INSPECTION-TOOLS.md`):** the Papers-Please loop split in two —
the **Rulebook** (static reference tables: what a thing *should* be) vs **Inspection Tools**
(visitor-scoped: what *this* item actually is). Two tools ship: **The Glass** (examine —
the item's tells) and **The Scale** (weigh — the measured amount). Every visitor carries
both readings; a hidden `relevant` flag marks which one decides (never shown), so most
readings are decoys — e.g. a rank-card weighed at "2 drams," or Pell's yarrow at a clean "1
sprig" (right amount, wrong plant). Standing orders became `accept {min,max,unit}` or
`total {needed,unit}`; an `item_check` is now two independent checks — identity (Glass vs
Book) and amount (Scale vs limit). Binary via the existing `STRICT_BINARY` dial = true
(reversible; the two former half-fails already carry `binary: reject`).

**How it was built:** a two-phase workflow — Model+Data, then Implement.

**What broke and the lesson:** the Model+Data agent did ALL its file work (data rewrite,
validator, the full design doc) and then failed to emit its **StructuredOutput** return,
hitting the retry cap (5) — which aborted the whole workflow before the Implement phase.
The *work* was fine and on disk; the *typed return* was the failure. **Lesson: for a phase
whose real deliverable is files on disk (not a data payload the next phase consumes), don't
gate it behind a strict output schema.** Recovery: verified the data booted clean, then ran
the Implement phase as a plain Agent (free-text report, no schema) — it completed cleanly.

**Verified (via the DevHarness capture loop):** binary desk shows only APPROVE/REJECT;
`ivy-threnody`/`odile-vantry` judge as reject; 16/16, zero errors/warnings. Read the Glass
capture ("Silver underside, five lobes, cold to the touch" under "Examine — what the item
actually is") and the Scale capture ("The jar settles at 3 drams." + green "within the
order's limit"). New additive method `ReferencePanel.set_inspection_target(visitor)`, wired
in `Main._on_visitor_changed`; no frozen signature changed.

**Note:** used a throwaway `_select_tab("glass"/"scale")` default-tab tweak to make the
harness auto-capture a tool page (it captures the default tab; tools aren't the default),
then reverted it. Left `DevHarness.enabled=false` (Panda's resting default).

## 2026-07-13 — Morning Queue: localization prep + a viewport-capture dev harness

**Context:** display text was inconsistent (Title-Cased in some places, raw slugs like
`rank_order` / `item_check` leaking in others) because the data stores identifiers and the
UI prettified some but not all — and the prettifier was copy-pasted across three
components. Separately, validating via the OS screenshotter was painful (other windows get
masked over the game).

**Localization prep (spawned agent):** centralized every user-facing string into one
`scripts/loc.gd` (`class_name Loc`) — the six duplicated formatters collapsed into a single
`humanize()` + a keyed `chrome`/`vocab`/`overrides` table, called statically like Palette.
Two layers, explicit: (a) translatable UI chrome + the finite enum/slug vocabulary → `Loc`;
(b) procedural content (names, summaries, player_story) → stays in the JSON. Identifiers are
never mutated — `checks[].entry` still resolves by exact string; only the *rendered* label
is humanized, with an `overrides` table for proper nouns the humanizer gets wrong. en is the
only locale; a second is one added dictionary. Chose a `Loc` module over Godot's native
`tr()`+CSV because the UI is 100% code-built and needs a *dynamic* slug humanizer a static
CSV can't express.

**DevHarness (`scripts/dev/DevHarness.gd`):** a validation aid on Main.tscn that (1)
captures the whole viewport to `.captures/*.png` — a folder readable directly, no OS
screenshotter, no window-masking — and (2) auto-steps the shift on a timer by invoking the
same handler a stamp-press fires, shooting one frame per visitor + the summary. Toggle via
the **Enabled** checkbox on the node (on by default so a bare `run_project` yields a full
capture set; untick to play manually). This is the edit→run→read loop Panda asked for.

**Consistency fix the harness immediately surfaced:** the end-of-shift ledger showed "Wren
Sixpence" (id `wren-sixpence` humanized) while the card showed the authored "Wrenna
Sixpence". Fixed additively — `Session` verdict-log entries now carry `name`, and the
Scoreboard prefers it. No frozen signature changed.

**Verified:** headless import + boot = zero errors; a full auto-stepped run scored 16/16 and
wrote 17 captures; read the summary PNG back to confirm the ledger now reads "Wrenna
Sixpence" and every reference tab is Title-Case ("Rank Ladder", not `rank_order`).

**Notes:** `.captures/` carries a `.gdignore` (Godot skips importing the shots) and is
`.gitignore`d. `Loc` is a new `class_name`, so the same class-cache gotcha applies — the
agent ran `--import` to register it.

## 2026-07-13 — Morning Queue: components built via workflow + playtest-verified

**Context:** with the scaffold + frozen contracts in place, built out the four UI components
+ theme.

**Choice:** fanned out one agent per component (card / reference / verdict / score / theme)
in a Workflow, each owning a disjoint file pair against the frozen interfaces, each
self-reviewing after building. Then integrated (wired `ThemeFactory.build()` into Main) and
playtested in Godot myself.

**Why the file-per-component split:** the frozen contract (methods/signals in
MORNING-QUEUE.md) + one `.tscn`+`.gd` per component meant five agents wrote in parallel with
zero merge conflicts. The self-review stage caught three real bugs the build missed: an Array
routed to a Dictionary-typed param in ReferencePanel (would crash on `set_references`), a
`get_node` result needing an `as ColorRect` cast in VerdictBar, and a base-class shadow.

**Verified:** booted maximized, stamped APPROVE on visitor 1 (Wren) → advanced to visitor 2
(Odd-Eye), score 0→1, verdict log `wren-sixpence -> approve : right`; headless smoke still
16/16. Screenshotted the running desk.

**Notes (would surprise the next person):** `class_name` globals (`Palette`,
`ThemeFactory`) live in `.godot/global_script_class_cache.cfg`, which ONLY the editor
regenerates — running via the MCP right after adding a new `class_name` script died with
`Identifier "Palette" not declared`. Fix: `godot --headless --path . --import` once (or open
the editor) to rebuild the cache; it's gitignored so a fresh clone needs it too. Both stamp
models ship behind `Session.STRICT_BINARY` (default false = four verdicts) per Panda's
"build both, decide by playtest" call on AGT.5.

## 2026-07-12 — The Morning Queue: first Godot/code tier + 16-visitor data

**Context:** Panda greenlit building the Morning Queue (the ghost-card candidate) — one
desk shift as a playable prototype — and asked for the 16 visitors coded to JSON, a Godot
project stood up, and a wireframe good enough to allocate sub-agents against.

**Options considered (project home + engine target):**
- (A) Pure GDScript + `gl_compatibility` renderer, under `adventuring-guild-teller/morning-queue/`.
- (B) C#/.NET (the installed editor is a `.mono` build).
- (C) A top-level `src/`-style tier, standing up `CodeDocs/` + `CODE-DESIGN.md` per Rule 2.

**Choice:** (A). Project lives inside the musing folder as a self-contained Godot 4.6
project; `build-musing.py` globs only top-level `*.html`, so the subfolder never reaches
Pages. Documented it with a project `README.md` (code-doc, following the `approaches-app`
precedent) + `MORNING-QUEUE.md` (agent-nav spec: data schema, frozen interfaces,
sub-agent allocation) rather than standing up the repo-wide `CodeDocs/` tier for one
prototype.

**Why:** the `.mono` editor **cannot Web-export** (.NET has no Web target); GDScript can.
Keeping it GDScript-only on the Compatibility renderer preserves the option to embed a Web
build in the **local** site later (not Pages — Godot 4 Web needs COOP/COEP headers Pages
can't serve; this matches Panda's "local not github pages" framing). Following the
`approaches-app` README-as-code-doc precedent keeps the zero-dependency-elsewhere posture
without a premature tier.

**Data shape:** two files — `visitors.json` (the queue) and `references.json` (the
rulebook every `check` resolves against), so the desk is actually *verifiable*, not
narrated. 16 visitors span all seven task types (incl. two new ones Panda added:
welcome/farewell roster changes, and multi-gate dungeon-drop commissions with a payout
calc) and eight failure axes. Two half-fails (`hold`/`conditional`) deliberately pressure
the "is the desk strictly binary?" question (AGT.5) via a `Session.STRICT_BINARY` dial.

**Corrections folded from chat:** all enemy targets are wild-magic apparitions / mana
beasts (no rats/vermin — nothing that depopulates); Sister Coll's fieldability check is
"no cleric/water-warded party *registered active*," independent of when anyone returns.

**Verified:** headless smoke drive scored 16/16 on correct stamps and 0 on a wrong stamp,
no load errors — the autoloads, flow state machine, and scoring work end-to-end. The four
component scenes are functional stubs with live plumbing, awaiting build-out per the
allocation table in `MORNING-QUEUE.md`.

**Notes:** renamed a `Session.log` member (shadowed GDScript's built-in `log()`). If the
site ever embeds a Web export, that artifact becomes a public surface — gate it under
Rule 6 then.

---

## 2026-07-12 — AGT correction round 1: nine claims settled

**Context:** Panda ruled on the pitch read-back by handle (AGT.2/3/4/6/8/9/10/11/12);
AGT.1/.5/.7 remain open.
**Choice:** folded rulings in place per the append-only protocol — a `settled` chip plus
a green "Settled —" line per claim, superseded text struck (never renumbered); the
research page got matching "Settled" notes on AGR.1/2/3/6, and the Bad Viking entry
became the Horticulture + Antiquities pair (2022 / Sept 2025, web-verified). Rulings
archived in `plans/PLAN-adventuring-guild-teller.md`.
**Why this shape:** provenance stays auditable — the original given/read/gap tag and
text remain visible under each ruling, so the page records the *dialogue*, not just the
outcome.
**Notes:** the rulings that most change the design: (1) the floor **never ticks** — no
time limits or affection decay; pillar II's rule gained "And no clock, ever." (2) no
death — gearless respawn at the dungeon entrance, and gear left behind seeds retrieval
quests, turning failure into desk content (AGR.1) and settling tone (AGR.6). (3)
summaries may be actionable, but every stat lives in in-game bios — anti-homework moves
from summary-tuning to bio UX (AGR.3). (4) suggestion acceptance = teller-trust ×
target-liking, giving refusal legibility a mechanism (AGR.2). One judgment call: the
full-town ruling arrived addressed to AGT.2 but answers AGT.9's questions — filed under
AGT.9, flagged in chat.

## 2026-07-12 — New musing: Adventuring Guild Teller (pitch stage, read-back format)

**Context:** Panda's brief — a guild-teller game: papers-please desk / stardew-social
floor / tomodachi fishbowl with a popup-dungeon creator layer; asked for a landing page,
a dressed-up pitch page containing *my breakdown of the brief so misconceptions can be
corrected*, and a functional desk-research page.
**Options considered:** (a) Markdown musing; (b) HTML-first set (thaumodynamics/LoMa/MDC
pattern); (c) one long pitch page, no hub.
**Choice:** (b) — `adventuring-guild-teller/`: hub + `pitch.html` + `research.html`,
verbatim-copy build. The pitch is a **read-back**: twelve claims `AGT.1`–`AGT.12`, each
tagged by provenance (given = restates brief · read = my inference · gap = brief silent)
with a per-claim "correct me" line — MDC's claims pattern, pointed at correction instead
of assertion. The research page gives every precedent a take/skip verdict and ends in
risk register `AGR.1`–`AGR.6`, each risk citing the claim it pressures.
**Why:** the ask was literally "so I can correct any misconceptions" — provenance tags +
stable append-only handles make corrections cheap and precise. Two mnemonics (AGT/AGR)
because Rule 8 scopes handles per page.
**Notes:** (1) The design spine I read into the brief — discretion *evicted* from the
desk, the inverse of Papers, Please's dilemma injection — is claim AGT.5, flagged as
inference, not fact. (2) Research facts were web-spot-checked; caught that Potionomics'
Quinn is the ingredient *vendor* (hero-adventuring is a separate befriend-and-send
system) before publishing the wrong version. (3) Stakes policy (adventurer death/injury)
deliberately left unset as Panda's call — AGT.12 flags it, AGR.6 records why it decides
the game's tone.

## 2026-07-12 — MDC follow-up: MIDI-learn remapper (Map pads)

**Context:** Panda's kit's MIDI spec doesn't put the hi-hat on the GM notes the coach
expects (42/44/46) — the plan's "MIDI-learn pad mapping" candidate got pulled forward the
same day.
**Options considered:** (a) a static note-number table you type into; (b) MIDI-learn
(click the lane, hit the drum); (c) per-kit presets.
**Choice:** (b). *Map pads* mode in `coach.html`: arming a lane binds the next incoming
note-on to it (`USERMAP` overrides GM; `laneFor()` resolves user-first), bindings render
on each pad, double-click clears a lane, one button resets all; persisted as
`localStorage["mdc:map"]`. The unmapped monitor line now says "use Map pads."
**Why:** learn-by-hitting needs zero knowledge of the kit's manual and works for any
number of zones; presets are a maintenance treadmill; typing note numbers is (a) with
extra steps. Binding *consumes* the hit (no sound-through into judging) and plays the
lane's voice once as confirmation — you hear what you just taught it.
**Notes:** keyboard/click pads never bind (only MIDI notes reach the learn branch), so
the feature is inert without hardware; verified by calling the same `handleNote()` the
MIDI callback uses. A GM note re-bound to a different lane stops counting for its GM
lane (`n in USERMAP` guard). Export/import of the map stays on the plan.

**Context:** Panda's brief — "a webpage musing that connects to a MIDI device and provides
suggested rhythms; let's get there for now." First musing whose deliverable is a *tool*
(Web MIDI in, WebAudio out), not prose or a static explorable.
**Options considered:** (a) Markdown musing + separate app page; (b) HTML-first hub +
self-contained app page (the thaumodynamics/logical-magic pattern); (c) app-only, prose in
a sidebar.
**Choice:** (b). `midi-drum/` — hub `index.html` carries the musing (claims `MDC.1`–`MDC.5`:
instrument-as-controller, *legible director*, notation-as-UI, judge-the-gap, practice-as-core-loop);
`coach.html` is the tool: GM-mapped pad monitor, 13 grooves as step grids (16ths + one
12-cell triplet shuffle, levels 1–5 in families), synthesized kit + click on a lookahead
scheduler, practice judging at ±30/±70/±120 ms with signed rush/drag bias, and a
director whose five rules (R1–R5) print the reason each suggestion fired.
**Why:** the game-design content *is* the director — DDA you can audit and override — so
the prose page frames exactly that and the tool demonstrates it. HTML-first because the
page must open from disk and stay dependency-free like its siblings.
**Notes:** (1) The whole input path is device-optional: on-screen pads + keyboard feed the
same `hit()` pipeline, so the page demos with zero hardware — also how it was verified
(machine-timed hits injected through the real pipeline: 100%/±0 ms run → R2 escalation;
50 ms-early run → all "good", −50 ms bias, R3 "you're rushing" + slower-BPM card).
(2) The embedded preview browser auto-denies `requestMIDIAccess`, so the granted path
needs a real browser + kit — untested until Panda plugs in; denied/unsupported paths are
exercised and graceful. (3) A manual input-latency slider (localStorage `mdc:calib`)
stands in for auto-calibration, deferred to the plan. (4) Judging windows are fixed-ms,
not tempo-scaled — rhythm-game convention; revisit if slow practice feels punishing.

**Context:** The reusable vignette handoff (`VIGNETTE-HANDOFF.md`) had done its job in a
*separate* chat — three finished everyday-life vignettes (`LVIG.1`–`LVIG.3`: Transcription
Nights, The Letter Kept, Crack and Splint) came back as Markdown. This session did the
deferred "separate step" the handoff always named: integrate them into the published set.
**Choice / what was built:** a new hand-authored `logical-magic/vignettes.html`
("Everyday Records") — self-contained, both themes, LoMa tokens, the site-wide breadcrumb,
the three vignettes at stable anchors `#lvig-1`–`#lvig-3`, each with its `LVIG.n` handle,
italic abstract, and body. Prose is **verbatim** from the handoff chat (typographic
conversion only — curly quotes, em-dashes, `<em>`; no rewriting, per the brief). Wired per
the HTML-first new-page checklist: a fourth live gallery card, a "Vignettes" registry
sublink, the nav-spec file-map row, the plan tick — plus the **`LVIG`** mnemonic declared
(Rule 8, append-only; next is `LVIG.4`).
**Why this shape:** it mirrors Space Feudal's `loom.html` *role* (several self-contained
vignettes on one companion page, one citable handle apiece) without copying its skin — LoMa
keeps its own palette, leaning on the gilt "grace/settlement" accent the everyday pieces
turn on (a mend is 12 flips; a vale lays down 10,000/day). Framed the card as a
**companion**, not a numbered chapter, so the "IV · candidate — the Grimoire" ghost stays
untouched.
**Notes:** numbers were checked against pitch §8 (Mending 22 strokes / 12 flips, 4
strokes/s, 10,000 flips/day, ~10⁶-fact true names) — the vignettes only *spend* constants,
never mint them. Doc resync ran slightly past the brief's file list: refreshed the stale
root `PLAN.md` "Next: worksheet + application pages" line (those shipped earlier today) and
added the `README.md` page-table row, so the index and human doc match the built set.

## 2026-07-12 — LoMa completes the THAU trio: worksheet + trial-duel + a vignette handoff

**Context:** Panda asked for three things in sequence: a reusable handoff doc so a
*separate* chat can write everyday-life LoMa vignettes (that chat proposes the
abstracts/prompts and the prose; this repo supplies the system and canon), then the two
pages that make LoMa match the thaumodynamics set — the problem set and the duel.
**Recon surprise:** this worktree was sitting on a stale local `main` (4e2fe00) with three
*uncommitted* DEV-LOG entries from some other session's Space Feudal **vignette** work
(VIG.6–8, "companion page V", a LOOM.6 split ruling + "two-system atlas") — whose files
exist neither here nor on `origin/main`. Preserved them in `git stash`
("stranded DEV-LOG entries: Space Feudal vignette session"), then fast-forwarded to
`origin/main` (3469a42, PR #2 merged + breadcrumbs + themed landing rows + Space Feudal).
🐼Panda: that stash is yours to reconcile — the vignette session's files live elsewhere.
**Choice / what was built:** (1) `logical-magic/VIGNETTE-HANDOFF.md` — self-contained
briefing (system digest as lived experience, glossary, the §8 constants verbatim, canon
cast incl. the new pages', texture inventory, guardrails, `LVIG.n` handle convention —
distinct from SF's `VIG.n`). (2) `loma101-worksheet1.html` — LOMA 101 Problem Set One,
mirroring MDYN 101's blank/student/key toggle; student is M. Sedge; all numbers cite
pitch §8 (the Ascent's forgotten survey strokes and the 226 s/226 min unit slip are the
teaching errors). (3) `assize-of-bells.html` — Crown v. Fen as a 10-slide trial-duel
mirroring the Ashfield Bout's deck; new canon minted deliberately: the **Rule of Sound
Warrant** (courts run relevance logic — ⊥-tainted derivations establish nothing at the
bar, even though stones explode), **"no writ, no wall"**, conviction by **ash-gap**
(the 8-stroke hole), total ledger 38 strokes / 0 flips. Both new pages carry the
breadcrumb standard and the LoMa tokens; gallery cards went live; registry sublinks,
README, nav spec, and plan synced.
**Why this shape:** the worksheet and trial reuse only mechanics the pitch already
claims (LOMA.1/.4/.5/.6) — the point of the set is that the courtroom and the classroom
fall out of the same ten rules, the way MDYN's worksheet and bout fall out of its field
equations.
**Notes:** the Assize deliberately resolves the pitch's Plea 03 aftermath and the
worksheet's B3 cites the repeal — the three pages now form one continuity. The vignette
chat should return `LVIG.n` Markdown; integration as a companion page is a later,
separate step.

## 2026-07-11 — Landing page: one themed row per musing, with hand-drawn SVG emblems

**Context:** Panda asked that the landing directory give each musing its own full-width
line, themed after its content, with an "img" on the left that helps the feel — reference:
the LoMa proof-circle seal.
**Options considered:** (A) hand-edit the generated index — dead on arrival, it's
generated; (B) hard-code four bespoke rows in `build_site.py` — themes don't belong in the
generator; (C) extend the registry: per-musing `emblem` (an SVG in the musing folder,
inlined into its row) + `theme` (`font` + `light`/`dark` token maps emitted as `--m-<key>`
CSS vars on `.row-<slug>`).
**Choice:** C. Four emblems authored, each in its musing's own visual language, colored
exclusively via `var(--m-*, fallback)` so one SVG follows both color schemes for free:
the **LoMa proof-seal** (glyph ring on a textPath around the settle rule — closest to
Panda's reference), the **THAU mirror-fields** (ember and storm circles coupled across the
dashed mirror), the **MSL lane web** (one accent route, a ship diamond mid-run, the front
collapsing in dashed from the right — with a bespoke `front` theme token), and the
**Space Feudal system roundel** (font star + bloom + orbits, gilt keep, two lane mouths
with drift rings). Row layout (flex, emblem column, mobile stack) lives once in
`site/style.css` (`.musing-list`/`.musing-row`, replacing `.project-grid`/`.project-card`);
generated CSS carries only colors. Serif rows for THAU/LoMa/SF, sans for MSL — matching
their pages. Palettes lifted verbatim from each musing's own `:root` tokens.
**Why:** the registry stays the single source of truth (Rule: anything on the landing
comes from config), themes stay with their musings (emblem lives in the musing folder,
palette in its config entry), and the open token-map schema means a future musing can
bring whatever colors its emblem needs without touching the generator again.
**Notes:** (1) The emblem is inlined into `index.html` only — it is not copied into
`site/musings/`, so HTML-first copy scripts and the MSL assets rule needed no changes.
(2) Fixed a stale card blurb while in the config: Space Feudal's description said "25-row
ledger" — it has been 27 rows since the Loom appended SF.26–27. (3) Verified light + dark
+ 375 px; the same SVGs recolor across schemes with no per-mode variants. Schema
documented in `musing-tech-notes.md` ("Landing rows: themes + emblems").

## 2026-07-11 — Site-wide breadcrumb navigation (coherent, portfolio-rooted) via a Sonnet fan-out

**Context:** Nav across the musings was a grab-bag — thaumodynamics and logical-magic had
*no* back-link at all, the 16 explorations used a `.72rem` mono backlink, space-feudal had a
small per-page `.crumb` bar, the MSL Markdown pages a minimal `← All musings`, and the React
approaches app a single `← back`. Panda asked (before the PR) for a coherent UX pass with
bigger, consistent navigation, rooted at the **portfolio** — and to run it as a fan-out of
**Sonnet** sub-agents with *firm* rules (Sonnets have ignored Rule 1 / PII guidance before),
supplying the portfolio repo for reference of the breadcrumb root.
**Recon:** the portfolio (`spiffy-panda_github-portfolio`, a Quartz site) deploys at the org
root `https://spiffy-panda.github.io/`; Game Design Musings is a *separate* Pages deploy at
`…github.io/game-design-musings-blog/`. So the coherent trail is **Panda's Portfolio ›
Game Design Musings › ‹Musing› › ‹sub-page›**: the portfolio crumb an absolute cross-site
URL (works everywhere), the landing crumb site-relative, the rest relative, current page a
non-link.
**Method:** wrote one prescriptive standard (`.crumbs` structure, sizing/a11y, exact
per-page hrefs, firm Rule 1 verbatim + PII gate + lane discipline) and had every agent read
the *same* file so parallel work stayed coherent. Split by write-disjoint folders: **4
Sonnet agents** (thaumodynamics / logical-magic / space-feudal / explorations, each editing
only its self-contained HTML) + **central (me)** for the shared surfaces that can't be
parallelized — `musing_render.py` (new `crumbs=` param), MSL `build-musing.py`, `site/style.css`,
the landing generator in `build_site.py`, and the React `Page`/`TopBar` (`kit.tsx` + 4 pages).
**Choice / invariant change:** the old "HTML-first pages carry **no** back-link to the
landing (file://-openability)" rule is **relaxed**: pages still render standalone from disk,
but the "Game Design Musings" crumb is site-relative (correct on the served site + local
preview; the one link that doesn't resolve on a raw `file://` open). Coherent wayfinding
rooted at the portfolio won the trade; the served site is the canonical target. Documented
in `musing-tech-notes.md` ("Navigation: the breadcrumb standard").
**Why:** a single shared spec + write-disjoint lanes is what made a Sonnet fan-out produce a
*coherent* result rather than five different nav designs; the shared-chrome layer (Python
renderer, React component, CSS) stays central so one edit fans out to many pages.
**Notes:** (1) All 13 page families verified post-build (fetch audit: portfolio-abs root,
depth-correct landing href, single `aria-current` crumb; React pages checked live since they
hydrate client-side) + screenshots in both themes and a 375px wrap check. (2) Agents each ran
a PII sweep and a global sweep confirmed no dead/real name in source or output. (3) Agent
finds logged for triage: thaumodynamics `ashfield-bout` slide-dots are 9px (kept — enlarging
edges into a layout change); explorations interactive controls not exhaustively audited.
(4) One agent caught a `.crumbs` class collision in `utility-ai-fit` and renamed the clashing
footer class — coherence dividend of the shared class name.

## 2026-07-10 — Space Feudal: The Loom (consequence threads + counterweights) + SF.26–27

**Context:** Panda pulled a thread the brief never examined — decoherence polices only
*transit* and the bloom only *the font*, so in-system automation is free: a settled
system's drone home-fleet dwarfs any chrism-fed mobile force. Commission: follow such
threads (and find more), chase consequences, offer counter-forces — keep both the
emergent texture and the tuned feudal feel.
**Options considered:** (A) fold consequences into existing pages — rejected, each page
has one job and the method deserves its own statement; (B) a fourth page with an explicit
thread → runaway → counterweight → dial format.
**Choice:** B — `loom.html` ("The Loom", handles `LOOM.1`–`LOOM.6`), opening with three
binding rules: counterweights must be *found in canon*, never decreed; every thread ends
on a feel dial; an uncheckable thread becomes new canon or a dissolution-tier tech. The
six threads: home shell (Panda's — checked by the pyramid leak pricing drone *quality*,
remass making it a point-defense shell, the bloom paradox [the only prize is anti-drone
ground], and the shell pointing inward at successions); muster of ghosts (unauditable
arsenals → Potemkin shells, checked by live-fire reviews/the Progress/defector ledgers);
fact corsairs (keys and prices as physical cargo, checked by courier sanctity + staleness
+ split keys + poisoned pouches); cold coast (ballistic stealth favors crewless pods,
checked by time cost + the floodlit Deep + sweep-certification as a banal fee); endemic
minds (AI crosses only as cold weights → heirloom lineages, wright guilds, the poisoned
codex); stratified manor (automation leaves three estates: bloom gangs / drone-wright
yeomanry / lodesman gentry — "bread cheap, chips dear"). Two counterweights were strong
enough to *append to the ledger*: **SF.26** license to crenellate ↔ shell charters
(adulterine shells razed), **SF.27** tournament ↔ the live-fire muster of ghosts —
first exercise of the append-only handle law (rows sit in the war group; IDs ≠
positions; ranges updated across pages).
**Why:** the home-shell thread *strengthened* the siege doctrine rather than breaking it
(it explains why SIEGE.4 storms are rare and why leaguers stand off in the Shallows) —
evidence the kit's counterweights are load-bearing rather than decorative, which is the
whole bet of the musing.
**Notes:** (1) New canon (nav spec invariants): drone home-shells exist and are chartered
("license to swarm"); AI ships as cold weights needing witnessed local revival; cold-coast
stealth exists and favors drones. (2) The dials interact — same counterweights serve
multiple threads (pyramid leak runs both sieges and shell quality) — noted in §7 as the
bench test for future threads.

## 2026-07-10 — Space Feudal: siege doctrine corrected + The Long Patience page + Harrow map

**Context:** Panda sustained an objection against the brief's one-line siege ("cut the
font → bunkers fade → surrender date"): chrism is *jump* fuel, not life support — a
near-future keep with closed loops grows its own food and lives for generations, so
fuel interdiction can't starve it. Commissioned: a siege-details page + a system map
with marked distances ("scale fudged — to scale nothing is readable").
**Options considered:** (A) give chrism in-system tactical uses so the old claim holds —
rejected, retcons the fuel into magic juice and muddies the §2 kit rule; (B) quietly
soften the wording — rejected, the claim was load-bearing and wrong; (C) concede the
objection in full and rebuild the doctrine on what the fade *does* clock.
**Choice:** C — `sieges.html` ("The Long Patience"): the fade kills a keep's **reach**
(three half-lives → no jumps) and the **besieger's reserve** (re-buy ≈ 1−2^(−t/90) ≈ 60%
per 120-day season), never a population. The keep's real clock is the one loop that
can't close at 30k souls — the industrial pyramid (fab-grade spares, pharma) — measured
in *years*; so a siege is two public clocks, most sieges end in terms (the customary
law of the siege: terms decay with resistance time) or a lifted leaguer, and six endings
got handles `SIEGE.1`–`SIEGE.6` with base rates. The Harrow map (inline themed SVG,
schematic on purpose, distances + brachistochrone times true at corsair 0.3 g / war
0.05 g / freight 0.01 g) grounds the doctrine: mouths anchor in flat space ~3 AU out
(wander ±0.2 AU seasonally), so nobody covers both gates — geometry forces
subinfeudation, and Millstone (volatiles at the Shallows' edge) is the natural leaguer
campsite, the counter-castle re-derived. Amended `SF.8` + the brief's §5 bullet;
constants contract extended in the siege page's foot; correction canonized in the nav
spec invariants ("don't reintroduce 'the keep starves'").
**Why:** the honest fix made the analogy *stronger* — the medieval record agrees (few
storms, fewer starvations; most sieges broke on the attacker's clock: the forty days,
winter, the pay chest) — and "the besieger is the one melting" lands the setting's core
thesis (chrism is income, never wealth) in siege form.
**Notes:** (1) New canon knock-on: "mouths are munitions" — population is fortification,
because industrial diversity scales the decay clock; big keeps outlast small ones, the
inverse of a granary siege. (2) The map quietly reconciles SF.7's tempo claim: corsairs
sprint mouth→font in ~10 d and raids resolve in hours; interstellar relief still needs a
season — the tempo gap survives the geometry. (3) In-system war is "chess by mail"
(burns are visible weeks out; surprise exists only at emergence) — this is why sieges
are stately and ambush lives at the mouths.

## 2026-07-10 — Space Feudal authored + registered (HTML-first: brief + correspondence ledger)

**Context:** Panda commissioned a new musing — X-series/Elite-style play, but the point of
interest is the economic layer above it: mimic the actual economic problems feudalism
solved and show how they lead to feudal lords again; FTL stipulated, resources free to
invent, plus "a page explaining how the old and to-be line up."
**Options considered:** (A) Markdown musing — dead on arrival for the centerpiece, the
renderer has no tables and the alignment page is inherently tabular; (B) extend
`musing_render.py` with tables — touches shared tooling for one musing's layout needs;
(C) HTML-first musing (the day-old THAU/LoMa pattern) — full layout control, two
self-contained pages.
**Choice:** C — `space-feudal/` (lowercase == slug per the HTML-first invariant):
`index.html` = the brief (six pillar-problems each emitting a spec line; an invented kit
where *every invention must earn ≥1 pillar and repair none*; §3 constants; the
company→governor→fief ladder; knobs + four "dissolution" techs), `ledger.html` = the
alignment page (25 old ‖ new ‖ in-play rows, grouped, mnemonic **SF**, append-only
handles `SF.1`–`SF.25`). Registered in `MUSING-CONFIG.json` with "The Ledger" sublink;
PLAN/SITE.md synced; plan file added.
**Why:** the ledger *is* the deliverable the brief argues for — a two-column
old-vs-to-be page wants designed HTML, and the verbatim-copy pattern was built for
exactly this shape. The load-bearing invention is **chrism decay** (t½ = 90 d): one
constant re-derives itinerant kingship (tithes can't be hauled → the Progress), plenary
governors (120-day order loops), and castle logic (attackers arrive at 63% bunkers) —
that triple coincidence is why it, and not the lane graph, anchors the setting.
**Notes:** (1) No ansible is the absolute knob — lanes carry hulls, not signals; every
other constraint is tunable, that one unravels the whole equilibrium if softened, so the
"wire returns" only as an endgame dissolution tech. (2) The MSL mention in the brief's
foot is deliberately *not* a hyperlink: MSL's repo folder has no `index.html`, so a
slug-relative link would 404 from disk, violating the file://-openable invariant.
(3) SF.19 (bound labor) carries an explicit darkness-knob note rather than sanitizing
the serfdom analogue — flagged as a designer's visible choice, with abolition as
late-game politics. (4) Constants contract lives in the brief §3; future SF pages cite,
never fork (LoMa §8 discipline).

## 2026-07-10 — Landing page links every project: THAU + LoMa promoted as HTML-first musings

**Context:** Panda asked for the Game Design Musings landing page to link the other
projects (the `thaumodynamics/` and `logical-magic/` sets, until now staging-only and
invisible to the site build).
**Options considered:** (A) hand-edit `site/index.html` — dead on arrival, it's generated;
(B) teach the landing generator "link-only cards" pointing at unbuilt folders — broken on
Pages, where unregistered folders don't deploy; (C) promote both sets to registered
musings with verbatim-copy `build-musing.py` scripts, the path both READMEs anticipated.
**Choice:** C — a new sanctioned musing variant, the **HTML-first musing**: no `MUSING.md`;
the hand-authored gallery `index.html` is the published entry; `build-musing.py` copies
every top-level `*.html` verbatim (the MSL-explorations treatment); the Rule-2 pair
becomes `index.html` + `<FOLDER-NAME>.md` (nav specs added for both). Registered both in
`MUSING-CONFIG.json` with card sublinks (THAU: Monograph/Worksheet/Bout; LoMa: The Pitch).
Pattern documented in `musing-tech-notes.md`; SITE.md inventory + plan files synced.
**Why:** the registry is the only honest way onto the landing page — anything else forks
the generator or ships dead links; and the copy-script promotion was designed for exactly
this moment.
**Notes:** (1) cross-musing links between the sets use `../<slug>/<page>.html`, which
resolves identically in-repo and under `site/musings/` — that only works because these
folders are lowercase == slug; keep that invariant. (2) HTML-first pages carry no site
chrome, hence no back-link to the landing page — deliberate, they must stay
`file://`-openable (precedent: copied explorations pages). (3) Registration flips these
folders from "committed" to "published on next push" — Rule 6 was re-checked on all six
HTML pages this session. Verified locally: full build clean (3 musings), all seven routes
200, LoMa→THAU cross-link lands on the copied gallery.

## 2026-07-10 — LoMa (Logical Magic) staged: the "casting is proving" pitch page

**Context:** Panda pitched a new system — **[Lo]gical [Ma]gic (LoMa)**: magic built on
first/second-order logic, advanced tiers dipping into monads and abstract CS/math. Classic
magical effects arrive *by fiat* (explicitly unlike MDYN's field equations), but it must keep
MDYN's detailed-grounded-calculation discipline. First deliverable: a graphical pitch page.
**Options considered:** (A) register a full musing now; (B) a top-level staging folder like
`thaumodynamics/`; (C) park it inside `explorations/` (rejected — that gallery is MSL-only).
**Choice:** B — `logical-magic/` with `pitch.html` (the deliverable), a THAU-style gallery
`index.html` with ghost cards for planned pages, and `README.md` declaring mnemonic **LOMA**;
plus `plans/PLAN-logical-magic.md` + a `PLAN.md` line. Unregistered; nothing deploys.
**Why:** mirrors the thaumodynamics precedent exactly (HTML-first set; promotion to a musing
is a later, deliberate hub-page step), and the two systems now read as deliberate siblings —
field equations vs. metamathematics, same grounded-calculation bet.
**Notes:** core design decisions worth not re-deriving: (1) two-currency cost model —
**strokes** (proof labor, caster-side) vs **flips** of **grace** (facts changed at settlement,
world-side); (2) **the Miser's Law** — settlement is minimal-model revision, which makes the
monkey's paw a *theorem* (Plea 02 audits it in a table); (3) spell circles = quantifier
alternation depth (the arithmetical hierarchy), with induction-vs-instantiation as the whole
economics of ∀ (Plea 01's 13-strokes-vs-904,779 punchline); (4) duels = game semantics (foes
buy the falsifier's seat on your ∀); (5) rituals = monads, skinned as **vessels/pouring**,
with the monad laws as "the Three Duties" and Writer-residue as forensics; (6) the six limit
theorems (Gödel/Tarski/Löb/Rice/compactness/Löwenheim–Skolem) as unpatchable physics.
`pitch.html` §8 is the tuning table — future LoMa pages cite or amend it, never fork numbers.
Gotcha for the next session: staging folders are invisible on the normal preview server
(`serve_site.py` serves `site/` only) — preview via a repo-root static server or open the
file directly. Also: SVG `font-size` attributes lose to the CSS `font` shorthand in utility
classes — size SVG text with an inline `style` when it matters.

## 2026-07-10 — Thaumodynamics set imported as a top-level staging folder

**Context:** Panda built a three-page fictional-physics set in a Claude Code session scoped to
the Builder-Research workspace — a field-theory magic monograph, a worksheet with a
blank/student/answer-key toggle, and a duel-chronicle slide deck — then realized game-design
material belongs here. Asked to move it into a subfolder as real files.
**Options considered:** (A) `explorations/<slug>/` — matches the self-contained-HTML staging
convention, but that gallery is explicitly MSL-only; (B) a new top-level `thaumodynamics/`
staging folder, musing-shaped but unregistered; (C) register it as a full musing now
(`MUSING.md` + `build-musing.py` + `MUSING-CONFIG.json` row).
**Choice:** B. Three standalone `file://`-openable pages + a small gallery `index.html` +
`README.md`; companion cross-links rewritten from artifact URLs to relative hrefs; **not**
registered, so nothing deploys.
**Why:** keeps `explorations/` single-universe; mirrors its staging precedent; promotion to a
published musing is a later, deliberate step — and this set is HTML-first, so it would publish
via a hub page rather than the Markdown pipeline.
**Notes:** authored under the Rule 6/7 gate (all names fictional). Each page also exists as a
private claude.ai artifact (URLs in the folder README) — repo copies are canonical. The three
pages share one token system and one set of in-world constants; the worksheet's numbers match
the monograph's plates deliberately.

## 2026-06-27 — Exploration explainer videos: a zero-pip slide+narration pipeline

**Context:** Panda asked for a narrated 60–90s explainer video (visuals + audio) for each
of the 16 MSL explorations. No video skill exists locally (confirmed via web search; third-party
Claude Code video toolkits exist but need paid APIs / installs). Decided to build it in-repo.

**Options considered:**
- *Visuals:* (A) screenshot the live pages and pan, (B) author purpose-built slides, (C) hybrid.
  Chose **C (hybrid)** per Panda.
- *TTS:* MCP voice gateway vs. Windows SAPI directly. The gateway turned out to be **the same
  three SAPI desktop voices** (David/Zira/Haruka), so calling SAPI directly via a `.ps1` keeps the
  util self-contained (no MCP dependency).
- *HTML→PNG:* headless Edge/Chrome (present on the box) vs. a pip rasterizer. Chose **headless Edge**
  (`--headless=new --screenshot`) — zero pip.

**Choice:** `utils/python/build_exploration_video.py` + `utils/powershell/tts_sapi.ps1`, driven by a
per-exploration scene script at `explorations/_video/scenes/<slug>.json`. Output → `explorations/_video/out/<slug>.mp4`.
Building blocks: SAPI (audio) + headless Edge (stills) + `ffmpeg` (Ken-Burns clips + concat). All zero-pip.

**Why / the surprise that shaped it:** the live JS **instruments do not paint in one-shot headless
capture** — `--screenshot` runs zero animation frames, so the canvases/SVGs come out blank (and
`--virtual-time-budget` freezes rAF, making it worse). *Static* page content (hero/prose) captures
perfectly. So the pipeline screenshots only the static page parts and **re-draws the key diagrams
(circulation loop, phase plot) as static SVG inside the slides** — which also reads better at video
scale. Capturing real running-simulation footage would need an interactive browser driver (e.g. the
Playwright MCP) and can't be fully batch-automated without extra setup; left as an optional upgrade.

**Notes:**
- Headless gotcha: capture reliably with `Start-Process -Wait` + a **fresh `--user-data-dir`** per
  shot + `--run-all-compositor-stages-before-draw`; too-short a wait yields no file.
- Duration is governed by narration length × SAPI `rate`; the pilot landed 129s→91s by trimming
  copy, `rate:1`, and a 0.4s per-scene tail. Tune `rate`/copy per page to stay in band.
- **Pilot:** `liquidity-deflation-spiral` (91.1s, 1080p, 22 MB). Other 15 pending Panda's review.
- **Git/disk:** 16 × ~20 MB ≈ 320 MB of binaries. Decided: **gitignore** `explorations/_video/{out,build}/`
  and track only the pipeline + per-page scene scripts (anyone can re-render).

**Update (same day) — Piper backend, speed knob, two more pilots:** Panda found SAPI's pacing slow
(consumes content at ~2×) and wanted denser narration + a "how to use the tool" beat, plus a
**SAPI-vs-Piper** voice comparison across the next two ideas. Added: a `piper` TTS backend (free local
neural voice `en_US-amy-medium`, installed to `%LOCALAPPDATA%\piper`, resolved with no hardcoded path),
a uniform `speed` knob (`ffmpeg atempo`, decoupled from the TTS engine so it's comparable across both),
and `--tts`/`--speed` CLI overrides. Two new bespoke pilots: **solvency-cell** (SAPI Zira, 1.6×, 89 s)
and **enemy-attack-schedule** (Piper amy, 1.8×, 87 s), each with re-drawn SVG diagrams (payer-gap clamp,
hub threshold; opening-book timeline, fog map) and a controls "how to poke it" slide. Piper's base pace
is slower than SAPI, so it needs a higher `speed` to hit the same band. Three pilots now await Panda's
voice pick before the bespoke batch of the remaining 13.

**Update — full batch done (16/16):** Panda picked Piper, ~1.6–1.8×, and a louder binaural bed
(gain 0.10→0.16). Authored the 13 remaining bespoke scene scripts via a parallel subagent fan-out
(one agent per page, each reading its page and writing `explorations/_video/scenes/<slug>.json` to the
schema; 6 produced clean inline-SVG diagrams — all spot-checked in-bounds). Added inline-SVG support
(`scene.svg`) so per-page diagrams live in the data, not the engine. All 16 render to **62–88 s**
(avg 76 s, 339 MB total, gitignored). **`speed` had to be tuned per page** because Piper's pace and each
script's word count vary: most sit at 1.7–1.9×, but the wordiest (utility-ai-fit, market-clearing-cell)
needed 2.2× to fit the 60–90 s band — pick `speed` from the first render's duration rather than guessing.
One render run was interrupted at a session boundary and left a truncated `solvency-cell.mp4` (moov atom
missing); re-rendering fixed it — the batch is restartable since each slug is independent.

## 2026-06-25 — MSL explorables: run complete + published into the site

**Context:** Morning wrap of the overnight run (entry below). It produced **16** interactive
explorables (the planned set); the final three landed but the session limit truncated the last
agent's summary, and `dead-reckoning-deck` never launched (classifier briefly down — left out, not
referenced anywhere). Panda then asked to (1) wrap the explorables in an **overview page** and
(2) link both the approaches and explorations hubs from the **landing-page MSL card**.

**What shipped:**
- **All 16 committed.** `explorations/index.html` rewritten into a real overview: top-nav back to the
  musing + a link to the approaches hub, intro framing, a lineage legend, 16 cards in three tiers.
- **Published into the site.** MSL's `build-musing.py` now **copies** the repo-root `explorations/`
  (overview + every folder with an `index.html`; internal `README`/`RUN-LOG`/`_research` skipped) into
  `site/musings/<slug>/explorations/`. Static HTML — copied, not rendered.
- **Landing-card sublinks, config-driven.** `build_site.py`'s card generator renders an optional
  `"links"` array from `MUSING-CONFIG.json`; the MSL entry gained Approaches + Explorations. New
  `.card-links` rule in `site/style.css`.
- **Bug found + fixed in QA.** The un-agent-verified `liquidity-deflation-spiral` crashed on boot —
  `reset()` runs `pause()→render()` before `S = freshState()`, so `render()` dereferenced an undefined
  `S`. One-line boot guard `if (!S) return;` in `render()`; re-verified (interactive console renders,
  zero console errors).

**Verification:** full `build_site.py` (incl. the React app) builds clean; served `site/` and
browser-checked the landing card (both sublinks render), the explorations overview (nav + 16 cards),
and earlier the marquee pages (solvency-cell, jumpgate-topology, enemy-attack-schedule, glass-cockpit).
All 16 identity-grepped clean — they're **public now**, so Rule 6/7 matters: no dead name, real last
name, or local paths; third-party game refs are transformative one-liners.

**Notes:**
- During the run: an agent edited tracked `.claude/launch.json` (reverted); one agent brushed Rule 1
  with a single `node -e` (no artifact). `.playwright-mcp/` added to `.gitignore`; QA screenshots removed.
- **Not pushed** — local commits only; Panda to review and push. On push, CI (Node step) builds the
  React app and `build-musing.py` copies the explorations, so the whole tree deploys automatically.

---

## 2026-06-25 — MSL: overnight "explorables" run (interactive HTML technical explorations)

**Context:** Overnight, unattended. Panda flagged mutation M1 (*The Two Ledgers*) as the favorite
and added two design seeds — (1) a broad enemy front whose attack *order* is predictable so the
player learns the firing conditions (time-since-start, time-since-last-op, prior-op failed/succeeded),
and (2) a jumpgate lane web (X4 / Freelancer / EVE / Stellaris / Mass-Effect-relay lineage). Brief:
spawn Opus agents to explore *technical aspects of the game*, each producing a web page with strong
info-visuals; pace the launches; branch + commit for a morning review.

**Options considered:**
- *Page form:* standalone interactive HTML vs. new React pages in `approaches-app/` vs. Markdown
  approaches. **Standalone interactive HTML** (chosen with Panda) — lowest merge-risk for parallel
  autonomous agents, richest fit for "poke the model," opens offline via `file://` with zero build.
- *Placement:* under the musing / under `approaches-app/` / a new top-level staging dir.
  **`explorations/`** — the site build does not read it, so nothing deploys to Pages until promoted
  (Rule 6 conservative).
- *Orchestration:* one Workflow vs. individual background Agents. **Background Agents** — Panda
  directed agent-spawning, and the cadence / wall-clock cutoff can't be expressed in a Workflow script.
- *Cutoff:* the "10am" cutoff read as **10:00 ET = 07:00 PT** (tied to *peak hours*; peak ~09:00 ET,
  so the Eastern reading serves the stated reason). Run started 03:17 PT.

**Choice:** A curated, tiered backlog of ~12 interactive "explorables," each a self-contained HTML page
in the console style (tokens copied from `approaches-app/src/styles/index.css` so they match M1/M2/M3
without the Vite build). A rolling ~3 Opus builders in the background, replace-on-completion; each page
committed as it lands. Wave 0 = the favorite (`solvency-cell`) + both Panda seeds
(`enemy-attack-schedule`; a `jumpgate-topology` page fed by a Sonnet net-scout) + an honest
`utility-ai-fit` audit. `explorations/index.html` is the morning entry point; `explorations/RUN-LOG.md`
tracks live state; `plans/PLAN-msl-explorations.md` is the plan.

**Why:** Interactive explainers are the highest-value reading of "explore a technical aspect," and
standalone HTML lets many agents work without touching shared build config. Staging in `explorations/`
keeps the public surface clean until Panda picks winners. Rolling-3 keeps the session alive on
background-completion notifications without depending on a timer tool, and naturally paces launches to
~agent-duration.

**Notes:**
- Rule 1 passed **verbatim, with a stern warning**, into every subagent prompt; each agent writes
  exactly one file in its own slug folder (no shared-file contention) and is forbidden servers/installers/builds.
- Identity gate (Rule 6/7) baked into every prompt: no real names, no local filesystem paths in any
  page, third-party game refs brief + transformative. The jumpgate scout held to small transformative
  excerpts (no wiki bulk).
- `UtilityAi` ("PandasAutonome") used **read-only** as reference; its public, AI-authored utility-AI
  architecture (response curves, modifiers, disembodied agents issuing *directives* that reshape
  subordinates' utility landscape) is the spine of the fit-audit page — MSL's contract board reads as
  exactly such a directive layer.
- `explorations/` is **intentionally not** wired into `build_site.py` (a deliberate desync, flagged in
  the plan: it's staging, not deployed). **Not pushed** — local commits only; Panda to review, promote
  favorites, then push.
- Results addendum to follow in the morning once the run completes.

---

## 2026-06-24 — Approaches go React; three HAND-lineage mutations

**Context:** Next pass on *Minimalist Space Logistics*. Two asks: (1) switch the approaches
hub + sub-pages to a rich HTML front end (the MD landing page stays Markdown, for
portability); (2) spawn three *mutations* of the HAND approach, each taking the original
pitch + a shared set of revisions + a divergent seed, each owning a slice of five open
questions.

**Options considered:**
- *Front-end tech:* keep hand-authored static HTML/CSS (zero-dependency) vs. a real
  framework + build step. **User chose the framework.**
- *Mutation placement:* nested under HAND vs. siblings under `/approaches/`. **Siblings.**
- *Existing three approaches:* re-skin into the new design vs. leave as-is. **Leave as-is.**

**Choice:** A Vite + React 19 + Tailwind v4 **multi-page** app in `approaches-app/`
(`base: "./"` so assets resolve under the Pages sub-path). It owns the hub
(`approaches/index.html`) and the three mutation pages (`two-ledgers`, `known-war`,
`glass-cockpit`); the retired Markdown hub (`APPROACHES.md`) was deleted and its synthesis
ported into `Hub.tsx`. `build_site.py` is now the single orchestrator: render the Markdown
pages, then run the Vite build and copy `dist/` over the `approaches/` folder — so
`serve_site.py` and CI both get the full site from one call. A shared component kit
(`src/components/kit.tsx`) keeps the pages consistent; three agents each authored one page
against it. CI gained a Node step; `.gitignore` covers `node_modules/` + `dist/`.

**Why:** The approaches pages wanted designed, interactive layout the Markdown subset can't
carry; the landing page wanted to stay portable. Scoping the framework to `approaches/` and
below satisfies both, and one orchestrator keeps "build = one command" true. A fixed kit + a
worked example (`Hub.tsx`) made three parallel React authors safe to integrate.

**Notes:**
- The zero-dependency stance is **amended, not abandoned** — two new rows in
  `PROJECT-PITCH.md`. `--no-frontend` does a fast Markdown-only build; a missing Node
  toolchain is non-fatal (warns + skips).
- `background-attachment: fixed` and header `backdrop-blur` both stalled the preview
  screenshot tool — dropped both (also better paint perf). The capture tool also returns
  black for deep-scrolled shots of tall pages; verify those via DOM + a tall viewport.
- Rule 1 passed verbatim to all three agents; each authored only its one page; all four
  pages compile in one Vite build with zero console errors (hub + M1/M2/M3 verified live).
- Mutations inherit HAND **minus ghosts** (shelved per request) and replace HAND's free
  agent-market with a faction-AI contract board. Not pushed (no request to).

---

## 2026-06-24 — MSL: approaches sub-page, authored by three divergent agents

**Context:** Pushed *Minimalist Space Logistics* past its first sketch. The fiction was
settled but the engineering was wide open (the musing's own "open questions"). Rather than
answer once, added an *approaches* sub-page and generated three pitches in parallel — each
given the same canon plus a distinct "spark" chosen to send it into a different design space
*and a different simulation paradigm* from the other two.

**Options considered:**
- *Sub-page shape:* one long page of three sections vs. a hub page + one page per approach.
- *Rendering sub-pages:* extend the shared `render_page` vs. hand-inject nav in the Markdown
  body vs. a fully bespoke build that bypasses the shared renderer.
- *Authoring:* write the three myself vs. fan out to three parallel agents with diverging sparks.

**Choice:** Hub + three sub-pages under `Minimalist-Space-Logistics/approaches/`, rendered by
an extended (still thin) `build-musing.py`. Added two optional, backward-compatible params to
`musing_render.render_page` — `back_href`/`back_text` — so a nested page back-links to its
parent instead of always "← All musings"; the build passes depth-aware `css_href`/`home_href`.
Fanned out three agents — *The Invisible Hand* (agent-based economy), *The Tide Line*
(pressure-field front), *Dead Reckoning* (deterministic content deck) — then wrote the hub as
a synthesis of where they converge and fork.

**Why:** Hub + pages gives each pitch room to go deep (the brief was "iron the loop down to
the simulation tech"), and reads better than one giant page. Extending `render_page` was the
minimal correct change — a sub-page back-linking to "All musings" with the wrong label is
worse than two optional params, and the shared renderer stays generic. Divergent-spark agents
produced genuinely different design spaces; the core they *independently* agreed on
(`APR.1`–`APR.5`) is the most trustworthy signal in the result — three explorers told to
disagree still bottomed out at the same game.

**Notes:**
- Rule 1 was passed **verbatim, with a stern warning**, into all three agent prompts (required
  by `CLAUDE.md`). Each was also constrained to the renderer's Markdown subset — no tables /
  nested lists. They complied: *Dead Reckoning* used a fenced ASCII ledger (not a pipe table),
  and equations stayed inside code spans/fences so underscores didn't turn into emphasis.
- Verified the build: `build_site.py --drafts` → 5 pages. Spot-checked the generated HTML for
  depth-correct `style.css` hrefs (`../../../` hub, `../../../../` approach pages) and the
  parent back-links.
- Rule 6 (public surface): the approaches are public design prose — no identity/third-party
  issues; reviewed before declaring done. **Not pushed** (no request to).
- Sub-page mnemonics registered in the nav spec (Rule 8): `APR` (hub), `HAND` / `TIDE` / `DEAD`.

---

## 2026-06-24 — Musing build framework (config + per-folder build, render into `site/`)

**Context:** First musing requested (*Minimalist Space Logistics*), and with it a framework:
each musing is a top-level `<MUSE-SLUG>/` folder with its own `MUSING.md` content, a
`<FOLDER-NAME>.md` nav spec, and a `build-musing.py` that renders it to HTML. A registry
(`MUSING-CONFIG.json`) drives a build that the server runs and includes in `site/`. This is
the generator the v1 plan deferred.

**Options considered:**
- *Markdown:* a pip library (`markdown` / `mistune`) vs a small in-repo stdlib renderer.
- *Output:* commit generated HTML into `site/` vs gitignore it and build in CI.
- *Hidden flag:* skip entirely (draft) vs build-but-unlist (unlisted-public).
- *Per-musing build:* duplicate logic in each `build-musing.py` vs a thin script that
  delegates to a shared `musing_render.py`.

**Choice:** Pure-stdlib renderer (`utils/python/musing_render.py`, documented Markdown
subset) so Pages needs no `pip install`. Generated output (`site/index.html`,
`site/musings/`) is gitignored and built in CI (`pages.yml` gains a build step);
`site/style.css` stays the one tracked source asset. `hidden: true` = **draft**: skipped by
the default build (never deployed), but `serve_site.py` builds with `--drafts` so drafts
preview locally with a badge. Each `build-musing.py` is thin and imports the shared renderer.
Retired the old `site/projects/` hand-authored model.

**Why:** Zero-dependency is a stated core value (it's why the preview server is stdlib);
breaking it for Markdown wasn't worth it for hand-authored prose. Gitignoring output keeps
git focused on sources and matches "the server builds the site." Draft-skip honors the
Rule 6 public-surface gate — a hidden musing's source stays local, never deployed.

**Notes:**
- This **introduces a build step**, superseding the pitch's "no build step (yet)" stance —
  flagged here per Rule 3; decisions recorded in `PROJECT-PITCH.md`.
- `build-musing.py` lives in the deliverable folder — a sanctioned third script location
  beyond `utils/` / `scrap_scripts/`; it still anchors to the repo root per Rule 1.
- Renderer bug caught during verification: soft-wrapped list items were splitting into stray
  `<p>`s; the parser now folds lazy continuation lines into the list item. Verified in the
  browser preview (landing card + full musing page: headings, lists, blockquote, code block).
- Hidden ≠ private: a hidden musing's `MUSING.md` is still in the repo. Don't put
  gate-failing material in a musing folder just because it's hidden.
- `slug` is lowercase (Pages is case-sensitive Linux; Windows isn't) — the folder can be
  PascalCase, the URL slug must be lowercase.

---

## 2026-06-24 — Local-server launch config + canonical server name

**Context:** Mirrors the `.claude/launch.json` "local-server" preview pattern from a
sibling project so the site can be previewed in the Claude Code launch panel. Also written
up as a reusable appendix to the bootstrap skill.

**Choice:** Renamed `utils/python/serve.py` → `utils/python/serve_site.py` (the
cross-project canonical name the launch config expects) and updated every reference. Added
`.claude/launch.json` with the `local-server` config (`python utils/python/serve_site.py
--port 8000`, `port: 8000`). Flipped the server's browser behavior from auto-open
(`--no-browser` opt-out) to opt-in (`--open`).

**Why:** One canonical server name keeps the pattern identical across repos and lets the
appendix be authoritative. Browser opt-in matches `python -m http.server` and avoids a
redundant browser window beside the in-panel preview (the reference launch.json passes no
browser flag).

**Notes:**
- Pattern documented in `../initialize-skill-v0_2-appendix-local-site-preview.md` (next to
  the prototype, outside this repo — a bootstrap artifact, not committed here).
- `.claude/launch.json` is committed (shared preview config); keep machine-local Claude
  settings in `.claude/settings.local.json` (gitignore that if it appears).
- The bootstrap entry below still names the old `serve.py` — left as-is (append-only history).

---

## 2026-06-24 — Bootstrap: scaffold + landing-page site + Pages deploy

**Context:** Fresh repo for miscellaneous game-design musings and exploration (named
after a Godot directory, but not Godot-specific). Initialized per the bootstrap skill
`initialize-skill-v0_2.md`. First real task bundled in: a Python preview server + a
landing page that acts as a directory to future projects, plus a GitHub Actions
workflow to publish it to GitHub Pages.

**Options considered:**
- *Repo shape:* code-bearing (stand up `src/` + `CodeDocs/` + `CODE-DESIGN.md`) vs
  prose/knowledge-base + tooling (skip the code-doc tier).
- *Landing page:* hand-authored `index.html` vs a data-driven generator that rebuilds
  the index from per-project metadata.
- *Publishing:* deploy-from-branch Pages vs GitHub Actions Pages deploy.
- *Preview server location:* `src/` (product code) vs `utils/` (durable tooling).

**Choice:** Prose/KB + tooling shape — no `src/`, no `CodeDocs/`. Deliverables are
written explorations surfaced via the static site (`site/`, a README + SITE.md
deliverable pair); the only code is a local preview server placed at
`utils/python/serve.py` and cataloged in `utils/README.md`. Hand-authored `index.html`
for v1 (generator deferred — see `plans/PLAN-blog-site.md`). Publish via GitHub Actions
(`.github/workflows/pages.yml`) uploading `site/` as a Pages artifact.

**Why:** The product here is content, not a program; the server is tooling, so the
code-doc tier would be ceremony with nothing to mirror. A hand-authored page is the
"basic" thing asked for and stays robust with zero projects; the generator is a clean
follow-up once real musings exist. Actions-based Pages is the current first-class path
and keeps `site/` the single source of truth (the same folder the local server previews).

**Notes:**
- `scrap_scripts/` is gitignored *except* its `README.md`, so the scratch-script
  convention ships with the repo while throwaway scripts stay local.
- Site links are **relative** so the page works both locally (served at `/`) and on
  Pages (served under `/game-design-musings-blog/`).
- Identity gate (Rule 6/7): git author is `Spiffy-Panda <CptSpiffyPanda@gmail.com>` —
  pseudonymous, no dead/real-name leak — so the public push is clean.
- One-time manual step: set GitHub Pages source to "GitHub Actions" (repo Settings →
  Pages) for the workflow to publish.

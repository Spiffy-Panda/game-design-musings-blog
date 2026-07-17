# Village Fish-bowl — a playable observatory

The village fish-bowl: the town-simulation pillar of the Adventuring Guild Teller, built as an
*observatory* you watch rather than a game you play. Eighteen townees follow authored day-plans, slow
pressures drift underneath them, and JSON-authored **storylets** fire into a **chronicle** that a dawn
**summary** narrates as village gossip. Needs that cross a threshold become **postings** on the guild
board, which stand, age, and come down unanswered. v0 is all readouts and debug knobs — no guild desk,
no floor.

It's a **Godot 4.6 (mono)** front-end over an **engine-free C# core**, with **JSON** data.

## Run it

**The sim, headless** (no Godot needed — this is where the logic and tests live):

```bash
dotnet test  core/Fishbowl.Core.Tests/Fishbowl.Core.Tests.csproj   # the whole suite (71 tests)
dotnet run   --project core/Fishbowl.Cli -- --days 3 --chronicle   # watch three days scroll by
dotnet run   --project core/Fishbowl.Cli -- --soak --days 14       # how much does this town actually SAY?
dotnet run   --project core/Fishbowl.Cli -- --lint --town data     # the content linter
dotnet run   --project core/Fishbowl.Cli -- --report out.json      # machine-readable run JSON
```

**`--lint` is a *content* linter, and it is the unusual one.** It doesn't read your C# — it runs the
real sim and reports what the *town* does wrong: drives that can only ever go one way, rules gated shut
forever, pairs who share 30 slots a day with nothing authored between them. Findings it can prove are
errors. It ships with an **acceptance ledger** (`data/lint-accepted.json`) for defects a human has
knowingly chosen to ship — but nothing in it is silenced: **every accepted finding still prints in
full, every run**, with the reason and the ruling above it, and any *new* one still fails the build.
The live town passes with 14 accepted. The frozen test fixture fails with 23 errors, which is correct —
it has no ledger and it really is defective.

**The observatory** (Godot): open `project.godot` in Godot 4.6 mono and press Play, or drive it
headlessly. On a fresh checkout run the import pass once so `class_name Sparkline` registers:

```bash
"C:/Program Files/godot/godot.exe" --headless --path . --import
```

In the window: **Step** advances one half-hour slot, **Run to Dawn** finishes the day, and the
panels update — roster, place board, chronicle (expand a row for the *because-list* that let the
event fire), the dawn summary, the postings board, and a townee inspector with pressure sparklines
and directed regard. The **debug knobs** are live: slide *actionability* to re-read the summary
from hearsay to report, turn *storylet_rate* down to thin the chronicle, and so on. **F9** saves a
screenshot to `.captures/`. The status strip along the bottom shows the day-hash — the same seed
always produces the same hash.

**The knob worth playing with is `novelty decay`.** It fatigues a story the town has told recently, so
tomorrow's summary reaches further down the bank. Drag it to **1.0** to turn it off, and you get the
old behaviour: because a rule's "tellability" is authored once and never changes, the summary was a
**fixed leaderboard** — the same handful of loud rules every night, forever, while 20 of the 50 rules
fired and were *never once mentioned to you*. Over a fortnight that's 31 different sentences; with
fatigue on, 47. It re-orders nights that have already happened, which looks like cheating and isn't —
the summary is derived when you read it, never stored.

## How it fits together

- `core/Fishbowl.Core` — the sim: determinism primitives, the typed data model, the CPS engine
  (clockwork day-plans → pressures → storylets → summarizer), and the JSON view layer.
- `core/Fishbowl.Cli` — a headless runner for traces, reports, and multi-seed soaks.
- `core/Fishbowl.Core.Tests` — xUnit, including a suite that replays Godot's number-stringifying
  round-trip over the real data so the engine never trusts a fragile parse.
- `cs/FishbowlBridge.cs` — the one C# node the engine touches; GDScript talks to it in JSON.
- `scripts/`, `scenes/` — the observatory UI (node scripting only; no sim logic).
- `data/` — **the live town**: townees, places, day-plans, traits, storylets, postings, and the
  linter's acceptance ledger. All features on; this is the town the observatory runs.
- `tests/towns/golden-town/` — **the frozen fixture**: a full, separate, posting-free copy of the
  original 12-townee town, which most of the suite loads (the board tests can't — a posting-free town
  has no board — so they assert invariants against `data/` instead). It is deliberately *not* inside
  `data/` — a golden master living in the live data directory tracks the very thing it is supposed to
  be pinning, which is exactly what it did until 2026-07-16. It will drift from `data/` over time; for
  a frozen master, **drift is the feature**.
- `addons/gd_test_harness/` — a drop-in **test harness** (synthetic input + scene inspection +
  token-frugal capture); inert unless activated. See its `README.md`; spec in the repo-root
  `plans/PLAN-godot-test-harness.md`.

For the full contract — the bridge surface, the determinism rules and the hash literals, the
two towns, the linter's ledger, the data shapes, and milestone status — see
[`FISHBOWL.md`](./FISHBOWL.md). For *why* it's built this way, see the parent
[`plans/PLAN-village-fishbowl.md`](../../plans/PLAN-village-fishbowl.md) and the repo `DEV-LOG.md`.

## Status

Milestones M0–M3 are complete and gate-checked; M4 (creation menus, generator, stats, soak) is in
place. The **board** landed 2026-07-16 (`PNO.M1`) and reached the screen on 2026-07-17: a posting
files, stands, ages, and expires in the town you actually run, and the **Postings board** panel shows
what is up right now. **Outings — an adventurer taking a posting and leaving town — are next**
(`PNO.M2`), and they are the reason `restlessness` currently ships as a known-broken drive: the buildup
exists to push a townee somewhere, and today there is nowhere to be pushed.

Determinism holds across runs and between the CLI and the editor. The day-hash is pinned to three
literals in the test suite, and they have **moved exactly once** — on an explicit ruling, recorded in
`DEV-LOG.md` before the strings changed.

**The most useful thing this project has learned so far** is in the same log, and it isn't about
Godot: an acceptance test that encodes a *defect* looks exactly like one that encodes a *requirement*.
The golden day's pinned beats included two that only ever fired **because** of a bug, and that survived
a hash pin, a 30-test suite and a determinism contract — because a bug can be perfectly deterministic.
Determinism was never the missing property. The only thing that told the fossil from the requirement
was fixing the bug and watching the test object.

# Village Fish-bowl — a playable observatory

The first release of the **village fish-bowl**: the town-simulation pillar of the Adventuring
Guild Teller, built as an *observatory* you watch rather than a game you play. Twelve townees
follow authored day-plans, slow pressures drift underneath them, and JSON-authored **storylets**
fire into a **chronicle** that a dawn **summary** narrates as village gossip. v0 is all readouts
and debug knobs — no guild desk, no floor.

It's a **Godot 4.6 (mono)** front-end over an **engine-free C# core**, with **JSON** data.

## Run it

**The sim, headless** (no Godot needed — this is where the logic and tests live):

```bash
dotnet test  core/Fishbowl.Core.Tests/Fishbowl.Core.Tests.csproj   # the whole suite
dotnet run   --project core/Fishbowl.Cli -- --days 3 --chronicle   # watch three days scroll by
dotnet run   --project core/Fishbowl.Cli -- --soak --days 7        # gossip-yield instrument
```

**The observatory** (Godot): open `project.godot` in Godot 4.6 mono and press Play, or drive it
headlessly. On a fresh checkout run the import pass once so `class_name Sparkline` registers:

```bash
"C:/Program Files/godot/godot.exe" --headless --path . --import
```

In the window: **Step** advances one half-hour slot, **Run to Dawn** finishes the day, and the
panels update — roster, place board, chronicle (expand a row for the *because-list* that let the
event fire), the dawn summary, and a townee inspector with pressure sparklines and directed
regard. The **debug knobs** are live: slide *actionability* to re-read the summary from hearsay to
report, turn *storylet_rate* down to thin the chronicle, and so on. **F9** saves a screenshot to
`.captures/`. The top bar shows the day-hash — the same seed always produces the same hash.

## How it fits together

- `core/Fishbowl.Core` — the sim: determinism primitives, the typed data model, the CPS engine
  (clockwork day-plans → pressures → storylets → summarizer), and the JSON view layer.
- `core/Fishbowl.Cli` — a headless runner for traces, reports, and multi-seed soaks.
- `core/Fishbowl.Core.Tests` — xUnit, including a suite that replays Godot's number-stringifying
  round-trip over the real data so the engine never trusts a fragile parse.
- `cs/FishbowlBridge.cs` — the one C# node the engine touches; GDScript talks to it in JSON.
- `scripts/`, `scenes/` — the observatory UI (node scripting only; no sim logic).
- `data/` — the authored town: townees, places, day-plans, traits, storylets, and the golden-day
  fixture the tests reproduce.

For the full contract — the frozen bridge surface, the determinism rules, the data shapes, and
milestone status — see [`FISHBOWL.md`](./FISHBOWL.md). For *why* it's built this way, see the
parent [`plans/PLAN-village-fishbowl.md`](../../plans/PLAN-village-fishbowl.md) and the repo
`DEV-LOG.md`.

## Status

Milestones M0–M3 are complete and gate-checked; M4 (creation menus, generator, stats, soak) is in
place, with the gossip-yield tuning (`VFB.Q1`) left as the live research question the knobs exist
to explore. Determinism holds across runs and between the CLI and the editor.

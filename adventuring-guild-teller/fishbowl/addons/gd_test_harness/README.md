# gd_test_harness (GTH) — a reusable input / inspect / capture harness

Drop-in Godot addon that drives a project through **synthetic input** (real `Input.parse_input_event`
pipeline), reports **who's under a click and who consumed it**, answers **clickability / hit-box /
offscreen** queries, and takes **token-frugal captures** (by reference, settle-then-shoot, change-detect
dedup). Two thin drivers over one pure-GDScript core:

- **Prescripted** — a JSON scenario run headlessly/CI via `--gth-scenario=…` (this file's `_ready` path).
- **Live (MCP)** — a loopback WebSocket in `bridge.gd` that an external MCP server calls.

It is **project-agnostic**: nothing here references any specific game. Spec + rationale live in
`plans/PLAN-godot-test-harness.md` (mnemonic `GTH`) at the repo root.

## Files

| File | Role |
|------|------|
| `harness_core.gd` | Autoload façade + activation gating + config + the command API both drivers call. |
| `input_injector.gd` | Mouse / key / action synthesis (`Input.parse_input_event`; `push_input` for SubViewports). |
| `scene_probe.gd` | Element resolution, predictive hit-stack + consumption order, clickability/geometry, snapshot. |
| `capturer.gd` | Settle → sha256 + 8×8 perceptual-hash dedup → downscale/region/annotate → write + `manifest.jsonl`. |
| `bridge.gd` | Loopback WebSocket JSON-RPC (the live/MCP transport). **Gated**; never opens in a shipped build. |
| `scenario_runner.gd` | The prescripted driver: reads steps, drives the core, evaluates `expect`. |

## Wiring it into a project (reusable in 3 steps)

1. Copy `addons/gd_test_harness/` into the project.
2. Add the autoload (Project Settings → Autoload, or `project.godot`):
   `TestHarness="*res://addons/gd_test_harness/harness_core.gd"`
3. *(optional)* Drop a `harness.config.json` at the project root (artifacts dir, port, capture caps).

The autoload is **inert** unless activated — normal play pays nothing.

## Activation (precedence: cmdline > env > `harness.local.json` > off)

```bash
# prescripted, self-verifying (rendered window; pixels need a real renderer, not --headless):
godot --path <proj> -- --gth-scenario=res://tests/harness/smoke.json --gth-exit-after

# live MCP bridge:
godot --path <proj> -- --gth-serve --gth-port=8787
```

Env equivalents: `GTH_SCENARIO`, `GTH_SERVE`, `GTH_PORT`, `GTH_ENABLE`, `GTH_EXIT_AFTER`.
Or a gitignored `res://harness.local.json`: `{ "scenario": "res://tests/harness/smoke.json", "exit_after": true }`.

## Command API (methods on the autoload; identical over the bridge and the scenario runner)

`snapshot(filter)` · `query_element(handle)` · `read_element(handle)` · `hit_test(x,y,normalized)` ·
`click_at(x,y,opts)` · `click_element(handle,opts)` · `move_to` · `drag` · `press_key(keys,opts)` ·
`send_action(action,opts)` · `capture(opts)` · `wait_for(opts)` · `run_scenario(spec)`.

**Element handle** (resolution order, ambiguity returns candidates, never guesses):
`{test_id}` (durable — `node.set_meta("test_id", …)` or a `test:<id>` group) · `{path}` · `{group}` ·
`{text}` (exact) · `{contains}` (substring). Coordinates are normalized `0..1` unless `normalized:false`.

## Known limitations (v0)

- **Hit-stack is predictive (Mode A):** replays Godot's GUI pick order; an active `gui_input` trace
  (Mode B) is future work. CanvasLayer/z_index handled for the common single-layer case.
- **Single main viewport:** embedded `Window`s (popup dialogs) are their own viewports and are excluded
  from the hit-stack — flagged in every report.
- **Pixels need a rendered session:** `--headless` uses a dummy renderer with no framebuffer; capture
  returns an error there. Run a real (optionally offscreen) window for captures.

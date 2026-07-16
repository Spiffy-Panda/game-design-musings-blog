# gd_test_harness (GTH) — a reusable input / inspect / capture harness

Drop-in Godot addon that drives a project through **synthetic input** (real `Input.parse_input_event`
pipeline), reports **who's under a click and who consumed it**, answers **clickability / hit-box /
offscreen** queries, and takes **token-frugal captures** (by reference, settle-then-shoot, change-detect
dedup). Two thin drivers over one pure-GDScript core:

- **Prescripted** — a JSON scenario run headlessly/CI via `--gth-scenario=…` (this file's `_ready` path).
- **Live (MCP)** — a loopback WebSocket in `bridge.gd` that an external MCP server calls. That server is
  `utils/dotnet/gth-mcp-server/` (.NET 8, dependency-free); build it once with
  `dotnet build utils/dotnet/gth-mcp-server` and see its `README.md` for registration.

It is **project-agnostic**: nothing here references any specific game. Spec + rationale live in
`plans/PLAN-godot-test-harness.md` (mnemonic `GTH`) at the repo root.

> ## ⚠ This directory is the CANONICAL copy. Edit it here.
>
> `utils/godot/gd_test_harness/` is the source of truth. Every Godot project's
> `addons/gd_test_harness/` is a **generated copy** — `utils/python/sync_gth_addon.py` overwrites
> it, so an edit made over there is silently lost, and worse, silently *diverges* until it is.
> Godot resolves addons under `res://` and cannot share a directory between projects, and symlinks
> need Developer Mode on Windows and don't survive a clean clone; one canonical copy plus a sync
> step is the least-bad arrangement (`GTH.Q4`, ruled 2026-07-16).
>
> ```bash
> python utils/python/sync_gth_addon.py           # canonical -> every project
> python utils/python/sync_gth_addon.py --check    # exit 1 if any copy drifted
> ```

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

1. Get the addon into the project's `addons/gd_test_harness/`. **In this repo:** add the project to
   `TARGETS` in `utils/python/sync_gth_addon.py` and run it — never hand-copy, that is the drift the
   script exists to stop. **Outside this repo:** copy the directory.
2. Add the autoload (Project Settings → Autoload, or `project.godot`):
   `TestHarness="*res://addons/gd_test_harness/harness_core.gd"`
3. *(optional)* Drop a `harness.config.json` at the project root (artifacts dir, port, capture caps).

Then tag the nodes worth addressing with `test_id` meta (`node.set_meta("test_id", "btn-step")`) —
metadata only, no behaviour, safe against a frozen contract. Code-built UIs get auto-generated node
names (`@Button@10`) and localized text, so neither survives as a handle; the meta does.

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
`send_action(action,opts)` · `capture(opts)` · `wait_for(opts)` · `window_state(opts)` ·
`run_scenario(spec)`.

**Element handle** (resolution order, ambiguity returns candidates, never guesses):
`{test_id}` (durable — `node.set_meta("test_id", …)` or a `test:<id>` group) · `{path}` · `{group}` ·
`{text}` (exact) · `{contains}` (substring). Coordinates are normalized `0..1` unless `normalized:false`.

## What the geometry words mean

The harness is allowed to be wrong. It is not allowed to be **confidently** wrong — a false
"reachable" is the same failure direction as a false "unchanged", and both have bitten this addon
(see `GTH.B1`–`B6` on the plan). So the vocabulary is deliberately narrow:

| Field | Means |
|-------|-------|
| `on_screen` | **Fully** inside the viewport. 4px of a 90px button showing is `false`, not `true`. |
| `visible_fraction` / `clipped` | How much is really visible, and which edges cut it. Present whenever `< 1.0`. |
| `clickable` | A click aimed at this **would land on it**. Decoupled from `on_screen` on purpose: a 4px sliver is genuinely clickable. |
| `anchor_point_px` / `anchor_clamped` | Where a click will actually go — the anchor clamped into the on-screen part. `click_element` uses this, so a clipped control gets clicked where it is reachable. |
| `blocked_by_window` | An embedded `Window` covers this control's anchor; the click cannot reach it. |

## Known limitations (v0)

- **Hit-stack is predictive by default (Mode A):** replays Godot's GUI pick order; CanvasLayer/z_index
  handled for the common single-layer case. Pass **`trace: true`** to `click_at`/`click_element` for
  **Mode B**, which watches every Control's `gui_input` during the click, reports who *actually*
  received it, and scores itself against Mode A (`agrees_with_mode_a`, plus a `disagreement` note when
  they differ — believe the trace). Mode B is best-effort: Godot emits `gui_input` *before* a Control
  runs its own `_gui_input`, so the consumer is inferred from where the chain stops, and anything
  consuming input outside the GUI dispatch (a raw `_input` handler) stays invisible to it.
- **Embedded `Window`s are handled, not skipped** (they were skipped in v0, which made a click over a
  dialog get attributed to whatever sat underneath — a wrong answer rather than a gap). A point over
  one reports `viewport: "embedded_window"` plus that window's own contents; a main-viewport control
  under one reports `blocked_by_window`. **Not** covered: arbitrary `SubViewport` auto-discovery.
- **Pixels need a rendered session:** `--headless` uses a dummy renderer with no framebuffer; capture
  returns an error there. Run a real (optionally offscreen) window for captures.
- **The window is locked while the harness is active** (`lock_window`, default on): resize and
  maximize are disabled, because every coordinate here is normalized against the viewport size and a
  resize would silently invalidate every rect already handed out. If it changes anyway, reports carry
  a `window_warning`.
- **A minimized window is restored before a capture** (`restore_on_minimize`, default on). This is not
  a nicety: minimized, the framebuffer freezes while the game keeps running, so `get_image()` returns
  a *stale* frame — byte-identical to the previous one, which `if_changed` would then dedup to
  `changed: false`. Measured, not assumed: see `tests/harness/regression-b1-b6.json`. Pass
  `allow_minimized` to shoot anyway and the result carries a loud warning.

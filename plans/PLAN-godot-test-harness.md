# PLAN — godot-test-harness (a reusable input / inspect / capture MCP for .NET + GDScript Godot projects)

**Mnemonic:** `GTH` (gates `GTH.D*`, milestones `GTH.M*`, requirements `GTH.R*`, open questions `GTH.Q*`).
**Status:** **built & verified 2026-07-15** — the in-engine core in the fishbowl (`GTH.M0`–`M4`) **and** the
external MCP server (`../utils/dotnet/gth-mcp-server/`, .NET 8, dependency-free), whose **live round-trip is
proven** (`--selftest`: WebSocket connect → snapshot → a `click_element` advances the sim to slot 1 →
capture) and **registered project-scoped in the repo `.mcp.json` as `gth-fishbowl`** (Rule-7-clean —
relative paths + a self-defaulted Godot exe, no machine path committed). Remaining: `dotnet build` the
server once (`bin/` is gitignored) + restart + approve to close the MCP-stdio→model leg.
`GTH.D2` resolved **.NET**; `GTH.D1`/`D3`/`D4`/`D5`/`D7` adopted on the recommendation; `GTH.D6` split —
the addon lives in the fishbowl (own copy, VFB isolation), the server is shared tooling in `../utils/dotnet/`
(repo-wide convergence of both stays `GTH.Q4`).
**Home:** a Godot **addon** (`addons/gd_test_harness/`, drop-in, project-agnostic) **+** a per-project
`harness.config.json` **+** the external **MCP server** (`../utils/dotnet/gth-mcp-server/`, built). Addon
copy lives in the fishbowl; server in `utils/`.
**First use case (done):** built into the fishbowl prototype at
`../adventuring-guild-teller/fishbowl/addons/gd_test_harness/` — **additive** to its F9 DevHarness (not a
replacement; convergence is `GTH.Q4`) and inert unless activated. Nav: `../adventuring-guild-teller/fishbowl/FISHBOWL.md`.
**Not a musing.** Pure tooling. Building it trips Rule 2's code-doc tier (`CodeDocs/` + `CODE-DESIGN.md`, or the addon's own `README.md` + nav spec) — see `GTH.D6`.

---

## The brief (Panda, 2026-07-15 — the chat dump this plan serves)

> Spec out (but don't implement yet) a reusable testing MCP that can send clicks and get
> back images (or place them in a shared location/directory). The testing harness should
> allow for both prescripted and MCP-live interaction. The "clicks" should be doable thru
> location or by element name. When it is by location, it reports back what elements are at
> that location (and if possible which ones got it before it was "consumed"). If it is by
> element, then it checks if that element is clickable and sends back a hit-box info (layer
> info, offscreen, screen-space size, etc.). The images should use methods to not be too
> wasteful with tokens — things like the call taking in a refresh-delay allowance. This
> harness should be reusable for any .NET/GD mixed project, not just the AGT prototypes that
> are the first use case.

## Requirements read-back (`GTH.R*` — correct me before we gate)

- **`GTH.R1` Inject + observe.** Send synthetic clicks/keys through Godot's real input pipeline (`Input.parse_input_event` / `Viewport.push_input`, per the input-injection note) and return observations. ✔ understood.
- **`GTH.R2` Two drivers, one core.** The same in-engine primitives serve *both* a **prescripted** runner (scenario file, runs headless in CI, no live model) **and** **MCP-live** interaction (Claude drives it turn-by-turn). Read as: capabilities live in-engine; "prescripted" and "MCP" are two front-ends onto them.
- **`GTH.R3` Click by location *or* by element.** Coordinate clicks (normalized `0..1` or pixels) and element clicks (by a stable handle) are both first-class.
- **`GTH.R4` Location → who's under it, and the consumption order.** A coordinate click returns the ordered stack of elements under the point and, best-effort, *which received the event before one consumed it*.
- **`GTH.R5` Element → clickability + geometry.** An element click (or a dry query) returns: is-it-clickable (with reasons), hit-box, layer/draw-order, on/off-screen, screen-space size.
- **`GTH.R6` Token-frugal images.** Images returned by reference (written to a shared dir, metadata back), settle-then-shoot, change-detection/dedup, downscale/crop, and a "refresh-delay allowance" so we don't spam near-identical frames.
- **`GTH.R7` Project-agnostic.** Reusable for *any* .NET/GDScript-mixed Godot project. Zero per-project code for basic use; a thin config + optional annotation opt-in.

---

## Why this is a two-process design (the load-bearing constraint)

An MCP server is an external process (Claude talks to it over stdio/HTTP). But injection and
introspection **must happen inside the running Godot process** — `Input.parse_input_event`,
viewport hit-testing, and `get_viewport().get_texture().get_image()` only exist in-engine. So
GTH is inherently split, and the split is the architecture:

```
  Claude ──MCP──▶  MCP server (external)  ──WS/TCP JSON-RPC──▶  Bridge autoload (in Godot)
   (live)          · tool surface                                 · routes to core primitives
                   · process lifecycle                            · gated: never in shipped builds
                   · artifacts + image budget                     ▼
                                                          ┌── InputInjector  (R1/R3)
  scenario.json ─────────────────────────────────────────┤── SceneProbe     (R4/R5)
   (prescripted, via ScenarioRunner in-engine)            └── Capturer       (R6)
```

The key move: **the four in-engine modules are the product.** The MCP server and the
ScenarioRunner are thin drivers that both call the same primitives — that is `GTH.R2`
satisfied by construction, not by duplication.

### Language choice makes it `.NET/GD`-agnostic (`GTH.R7`)

The in-engine core is **pure GDScript**. Injection, hit-testing, and capture are *engine-level*
— a C#-authored `Node` and a GDScript `Node` are both just `Node`s in the tree, so introspection
and input work identically regardless of the project's C#/GD split. A GDScript addon is the
lowest common denominator every Godot project can load, mono or not. (Optional thin C# facade so
C#-first shops can *author scenarios* in C#; not required for the harness to work.)

---

## Component specs

### 1. `InputInjector.gd` — the input surface (`GTH.R1`, `GTH.R3`)

Wraps the injection note's mechanism into reusable calls. Every call: build event → set
`position`/`global_position`/`button_mask` → `Input.parse_input_event()` → `Input.flush_buffered_events()`
→ `await` N frames to let reactions land. For a `SubViewport` target, route through
`sub_viewport.push_input(event, true)` (local coords) instead of the root.

- `click_at(pos, button, normalized, settle)` — press+release pair (never a lone press;
  `button_mask` set on down, `0` on up). `pos` normalized→px via `get_visible_rect().size`.
- `click_element(handle, …)` — resolve (§2), refuse-or-warn if not clickable, click its anchor.
- `move_to`, `drag(from,to)`, `scroll`, `key(keys,text)`, `action(name,pressed)`.
- `InputEventAction` path exposed too — resolution-independent, best for game-logic tests where
  a pixel isn't the point.
- **Settle knobs** (`use_accumulated_input=false` at session start for determinism; explicit
  `flush`; configurable post-event frame-wait) so a click's effects are observable before return.

### 2. `SceneProbe.gd` — resolution, hit-testing, clickability (`GTH.R4`, `GTH.R5`)

**Element addressing (resolution order; ambiguity returns candidates, never a guess):**

1. **`test_id`** — `node.get_meta("test_id")` (or membership in a `test:<id>` group). The durable,
   refactor-proof handle — GTH's `data-testid`. Projects opt in by tagging nodes; the harness
   reads it generically. *(Recommended default — `GTH.D4`.)*
2. **`%UniqueName`** (scene-unique names) · 3. **node path** (abs/rel) · 4. **text match**
   (Button/Label/… containing text — convenience, may be ambiguous) · 5. **group**.

**Hit-stack at a point (`GTH.R4`) — the honest two-mode design:**

- **Mode A · predictive (default, non-invasive, headless-safe).** Walk the tree; collect every
  `Control` whose `get_global_rect()` contains the point and is `visible`, respecting
  `clip_contents` and `SubViewport` transforms; add physics hits via
  `direct_space_state.intersect_point` (2D) / `intersect_ray` from the active camera (3D) where
  `input_pickable`. Order **front-to-back** by `CanvasLayer.layer → z_index → tree order`, tag each
  with its `mouse_filter`. The reported chain = the run of `PASS` controls from the top down
  **through and including the first `STOP`** (the predicted consumer); `IGNORE` excluded. This is
  exactly Godot's GUI pick semantics, computed rather than observed — deterministic and cheap.
- **Mode B · active trace (opt-in).** Immediately before injection, connect to `gui_input` on the
  Mode-A candidates, inject, and record who *actually* fired plus whether
  `get_viewport().is_input_handled()` flipped after each — the observed consumption chain.
  Best-effort and documented as such (can't see arbitrary user-code `set_input_as_handled()`
  without broader instrumentation); connections restored after. Gated by `GTH.D3`.

**Clickability report for an element (`GTH.R5`) — computed predicate + reasons + geometry:**

```
clickable = in_tree ∧ visible_in_tree ∧ ¬disabled ∧ mouse_filter≠IGNORE ∧ on_screen ∧ is_top_hit
```
Returned fields: `exists`, `type`, `clickable` (+ per-factor booleans & human strings),
`rect_px` & `rect_norm` (global), `screen_size_px`, `on_screen` (rect ∩ viewport-visible-rect,
accounting for parent clip / SubViewport) + `offscreen_reason`, `visible`, `disabled`,
`mouse_filter`, `focus_mode`, `z`/`CanvasLayer`/`layer`, `is_top_hit`, and `occluded_by` (what
sits on top at the anchor — reuses Mode A). `query_element` returns all this **without clicking**;
`hit_test(x,y)` is the read-only sibling of a location click.

### 3. `Capturer.gd` — token-frugal imaging (`GTH.R6`)

Principle to bank on: **structured observation is primary; pixels are the fallback.** Most
"what's on screen?" questions are answered by `snapshot`/`query_element` at a fraction of the
tokens of an image. When an image *is* needed:

- **Reference, not inline.** Write to the shared artifacts dir; return
  `{path, w, h, bytes, sha256, changed, phash_distance}`. Claude opens the file only when it must
  actually look. (Optional opt-in downscaled inline thumbnail.)
- **Settle-then-shoot** (the "refresh-delay allowance"). `settle_ms`: poll frame hashes and wait
  until the viewport stops changing (delta < ε for a debounce window) up to a timeout, so we never
  shoot mid-transition. Renderer-agnostic (compare successive `get_image()` hashes; no dependence
  on tween/anim bookkeeping).
- **Change-detection / dedup** (`if_changed`, default on). Content hash **+** perceptual hash vs
  the last capture; if within threshold, return `changed:false` + the prior path — **no new file,
  no new tokens.** `phash_distance` is reported so the model can decide.
- **Downscale / format / region / element-crop.** `max_dim` (default cap, e.g. long-edge 1280),
  `format` (PNG for crisp UI, JPEG/WebP for photographic), `quality`, `region`/`element` to shoot
  only the rect that matters. Optional `annotate` draws the last click point / target rect so **one**
  image answers the question.
- **Session image budget.** Soft cap; the MCP warns/refuses past N images per session unless
  overridden — a backstop against silent token bleed.
- **Batching.** `run_scenario` (or a `batch` of steps) runs N actions in one round-trip and returns
  **one** settled capture + a step log — the biggest single token lever.

Artifacts layout (shared dir, inspectable, same for CI and live):
`artifacts/<session>/<seq>-<label>.<ext>` + `manifest.jsonl` (one line per capture: seq, label,
hashes, geometry, changed-flag).

### 4. `Bridge.gd` + `ScenarioRunner.gd` — the two drivers (`GTH.R2`)

- **`Bridge.gd`** — a localhost WS/TCP JSON-RPC endpoint (`GTH.D1`) routing commands to the three
  modules above. **Gated:** activates only under an env flag / debug build; binds loopback only;
  **never opens a socket in a shipped game.**
- **`ScenarioRunner.gd`** — reads a scenario (JSON steps, or a tiny line DSL) and drives the same
  primitives with assertions, writing the artifacts + a JUnit-ish report. Runs under headless CI
  with **no MCP and no model in the loop.** Example step vocabulary:
  `capture baseline` · `click element=%StartButton settle=300` · `expect element=%HUD visible=true`
  · `click at=0.5,0.5 report_hits` · `capture if_changed`. `run_scenario` lets the **live** side
  submit the same scenario in one call — prescripted and live share one format.

---

## The MCP tool surface (structured-first)

| Tool | Returns | Notes |
|------|---------|-------|
| `session_start(config?, mode)` | session id, viewport size, mode | `mode` = `headless` (no pixels) or `rendered` (see risk below). Launches or attaches. |
| `session_stop()` | — | Tears down the game if GTH launched it. |
| `snapshot(filter?)` | interactable tree: `[{test_id, path, type, text, rect_norm, on_screen, clickable, layer}]` | **Primary "what's on screen" call. No image.** Token-cheap. |
| `query_element(handle)` | full clickability + geometry report (§2) | **No click, no pixels.** |
| `hit_test(x,y)` | ordered hit-stack + predicted consumer (§2 Mode A) | **No click.** |
| `click_at(x,y,{button,normalized,settle,report_hits})` | hit report (stack + consumer, +trace if on) | `GTH.R4`. |
| `click_element(handle,{button,settle})` | clickability report; clicks iff clickable (flag to force) | `GTH.R3`/`R5`. |
| `key/action/move_to/drag/scroll` | ack + optional settle | rest of input. |
| `capture({region?,element?,max_dim,format,quality,settle,if_changed,annotate})` | `{path,w,h,bytes,sha256,changed,phash_distance}` | `GTH.R6`. |
| `wait_for({element_visible|settled|signal|ms})` | resolved condition | deterministic waits. |
| `run_scenario(path\|steps)` | step log + assertions + one settled capture ref | `GTH.R2` + batching. |

---

## Session modes & the headless-pixel constraint (Known Risk → `GTH.D7`)

Godot `--headless` uses a dummy display/renderer: input, tree introspection, hit-testing, and
scenario assertions all work, **but there is no framebuffer to capture** — `get_image()` comes back
blank. On Windows there's no `xvfb` fallback. So GTH offers **two session modes**, which also
happens to reinforce token frugality:

- **`headless`** — input + introspection + assertions, **no pixels.** The CI default; most
  prescripted scenarios never need an image.
- **`rendered`** — a **real** rendering context so `Capturer` works. On Windows that means an actual
  window (positioned offscreen / minimized), **not** `--headless`. This is precisely what the
  existing DevHarness already does (it runs windowed and captures the viewport to `.captures/`), so
  the precedent holds — GTH just generalizes it.

`GTH.D7` = ratify this split (headless-no-pixels / rendered-with-pixels) and the offscreen-window
approach for `rendered` on Windows.

---

## Decision gates (need a Panda ruling before build)

- **`GTH.D1` Transport.** WS/TCP JSON-RPC between MCP server and `Bridge.gd`. *Recommend* `WebSocketPeer`
  (loopback) — headless-friendly, clean reconnect, JSON-native, game connects out to a port the MCP
  server passes via env. Alt: raw `TCPServer`.
- **`GTH.D2` MCP host language.** *Recommend* **.NET / C#** (official MCP C# SDK) to keep the stack
  cohesive with the C# cores and let scenario authors share types. Alt: Node/TypeScript (most mature
  MCP + Playwright-MCP prior art) or Python.
- **`GTH.D3` Consumption-chain fidelity.** *Recommend* Mode A (predictive) as the always-on default,
  Mode B (active trace) opt-in and built in a later milestone. Ruling: is best-effort trace in scope
  for v1, or deferred?
- **`GTH.D4` Addressing convention.** *Recommend* adopt `test_id` meta (+ `test:<id>` group) as the
  project-agnostic durable contract, node-path as fallback. Ruling: bless the convention?
- **`GTH.D5` Image defaults.** *Recommend* reference-by-path, `if_changed=true`, settle-then-shoot,
  long-edge cap ~1280, PNG default. Ruling: accept defaults (and the session image budget)?
- **`GTH.D6` Home + Rule-2 tier.** In-repo `utils/godot-test-harness/` **or** its own repo (it's meant
  to serve *other* projects too, which argues standalone). Either way, building it stands up the
  code-doc tier. Ruling: where, and stand up `CODE-DESIGN.md` now or at first commit?
- **`GTH.D7` Session-mode split.** Ratify `headless`(no pixels) / `rendered`(offscreen window) per
  the constraint above.

## Milestones

**Status (2026-07-15, in the fishbowl):** M0 ✅ · M1 ✅ · M2 ✅ · M3 ✅ · M4 ✅ — the prescripted
`smoke.json` runs green: a synthetic `btn-step` click advances the sim (click → `Button.pressed` →
`bridge.StepSlot` → clock slot 1), a location click selects a roster row, hit-stacks report the
consumption chain, clickability/geometry reports resolve, and captures write with sha-dedup + annotate.
**M5 ◑** — external MCP server built (.NET, `../utils/dotnet/gth-mcp-server/`) and its **live round-trip
verified** (`--selftest`: WS connect → snapshot → `click_element` advances the sim → capture, game
launched/stopped by the server), and **registered project-scoped in the repo `.mcp.json` as `gth-fishbowl`**
(Rule-7-clean: relative `args`/`GTH_PROJECT` + a self-defaulted `GTH_GODOT_EXE`, so no home-dir path is
committed). Remaining: `dotnet build` the server once (`bin/` is gitignored) then restart + approve to close
the MCP-stdio→model leg (can't be self-verified mid-session). Mode-B active trace stays future work.

- **`GTH.M0`** Addon skeleton + gated `Bridge` + `harness.config.json` + `session_start/stop` +
  smoke `capture` (reference-return). Both session modes stand up.
- **`GTH.M1`** Input core: `click_at`, `move`, `drag`, `key`, `action`, settle/flush; predictive hit
  report (Mode A) + `hit_test`. → `GTH.R1`, `R3`(loc), `R4`.
- **`GTH.M2`** `SceneProbe`: resolution (test_id/unique/path/text/group), `query_element`, `snapshot`,
  clickability report, `click_element`. → `GTH.R3`(elem), `R5`.
- **`GTH.M3`** Token-frugal imaging: settle, `if_changed` (content+perceptual hash), region/element
  crop, downscale/format, annotate, session budget, `manifest.jsonl`. → `GTH.R6`.
- **`GTH.M4`** Prescripted driver: scenario format + `ScenarioRunner` + `run_scenario` + assertions +
  headless CI report. → `GTH.R2` (prescripted half).
- **`GTH.M5`** Optional Mode-B trace (`GTH.D3`), C# scenario facade (`GTH.D2`), docs, and first real
  adoption against an AGT prototype (opt-in, isolation rule honored).

## Open questions (`GTH.Q*`)

- **`GTH.Q1`** Multi-viewport / multi-window games: does v1 target only the root viewport + declared
  `SubViewport`s, or auto-discover every viewport? (Affects `snapshot` scope.)
- **`GTH.Q2`** 3D pickable objects — is `intersect_ray` hit-reporting in scope for v1, or 2D/Control-only
  first? (AGT prototypes are 2D; the harness claims general reuse.)
- **`GTH.Q3`** Determinism hooks — should GTH also expose seed-pinning / fixed-timestep stepping (many
  .NET/GD projects, incl. the fishbowl, run a seeded core), or stay strictly input+capture?
- **`GTH.Q4`** Convergence: once GTH exists, do `morning-queue/` and `fishbowl/` retire their bespoke
  DevHarnesses onto it — and does that finally justify the shared library their isolation rule has so
  far deferred? (Post-v1, Panda's call.)

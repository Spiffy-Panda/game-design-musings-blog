# PLAN — godot-test-harness (a reusable input / inspect / capture MCP for .NET + GDScript Godot projects)

**Mnemonic:** `GTH` (gates `GTH.D*`, milestones `GTH.M*`, requirements `GTH.R*`, open questions `GTH.Q*`,
bugs/backlog `GTH.B*`).
**Status:** **built, registered, and in use.** The in-engine core in the fishbowl (`GTH.M0`–`M4`) and the
external MCP server (`../utils/dotnet/gth-mcp-server/`, .NET 8, dependency-free) are both done. **The
MCP-stdio→model leg is closed** — the server is built, registered project-scoped in the repo `.mcp.json` as
`gth-fishbowl` (Rule-7-clean: relative paths + a self-defaulted Godot exe, no machine path committed), and
was **driven live by a model for ~213 tool calls over 9 relaunches on 2026-07-16** (the field report at the
bottom of this file). A fresh clone still needs one `dotnet build utils/dotnet/gth-mcp-server` because
`bin/` is gitignored — that is a clone step, not an open task.
**All seven gates are ruled:** `GTH.D2` resolved **.NET**; `GTH.D1`/`D3`/`D4`/`D5`/`D7` adopted on the
recommendation; `GTH.D6` split — the addon lives in the fishbowl (own copy, VFB isolation), the server is
shared tooling in `../utils/dotnet/` (repo-wide convergence of both stays `GTH.Q4`).
**Open — and it is all rulings, no code.** The `GTH.B*` bugs are **all six fixed & verified 2026-07-16**;
`GTH.Q1` is answered by `B4`; the **Mode-B trace is built**, so `GTH.M5`'s only survivor is the optional
C# scenario facade, **recommended closed as YAGNI** (no consumer exists). That leaves `GTH.Q2` (is 3D
`intersect_ray` hit-reporting in v1 scope?), `GTH.Q3` (should GTH expose seed-pinning / fixed-timestep
stepping?), and `GTH.Q4` (do `morning-queue/` and `fishbowl/` retire their bespoke DevHarnesses onto GTH,
and does that finally justify the shared library their isolation rule has deferred?) — all Panda's calls,
none of them blocking anything.
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
clickable = in_tree ∧ visible_in_tree ∧ ¬disabled ∧ mouse_filter≠IGNORE ∧ anchor_on_screen ∧ is_top_hit
```
**Amended by `GTH.B1` (2026-07-16): `clickable` is decoupled from `on_screen`.** The original predicate
`∧ on_screen` conflated two different questions, and once `on_screen` was made strict it would have
called a 4px-visible button *unclickable* — which is just the old lie told backwards, since such a
button genuinely can be clicked. The operative term is `anchor_on_screen`: **would a click aimed at
this land on it?** — evaluated at the anchor *clamped into the control's visible part*, which is also
the point `click_element` actually clicks.

Returned fields: `exists`, `type`, `clickable` (+ per-factor booleans & human strings),
`rect_px` & `rect_norm` (in the owning viewport's space), `screen_size_px`, `viewport_px`,
`on_screen` (**strict — fully inside**), `visible_fraction` + `clipped` edges + `offscreen_reason`
("clipped right — only 13% of it is inside the 1290x810 viewport"), `anchor_point_px` +
`anchor_clamped`, `visible`, `disabled`, `mouse_filter`, `focus_mode`, `z`/`CanvasLayer`/`layer`,
`is_top_hit`, `occluded_by`, and — for embedded Windows — `in_embedded_window` / `blocked_by_window`.
`query_element` returns all this **without clicking**; `hit_test(x,y)` is the read-only sibling of a
location click. `snapshot` shares the same predicate rather than a cheaper approximation of it.

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
- **`GTH.D3` Consumption-chain fidelity.** *Recommended* Mode A (predictive) as the always-on default,
  Mode B (active trace) opt-in and built in a later milestone. **Adopted, and now fully discharged —
  Mode B was built 2026-07-16** (`trace: true`), which `GTH.B4` made the case for: Mode A can be
  confidently wrong, and only an observation can catch that.
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

**Status (2026-07-16, in the fishbowl):** M0 ✅ · M1 ✅ · M2 ✅ · M3 ✅ · M4 ✅ — the prescripted
`smoke.json` runs green: a synthetic `btn-step` click advances the sim (click → `Button.pressed` →
`bridge.StepSlot` → clock slot 1), a location click selects a roster row, hit-stacks report the
consumption chain, clickability/geometry reports resolve, and captures write with sha-dedup + annotate.
**M5 ◑** — the external MCP server is built, `--selftest`-verified, registered as `gth-fishbowl`, **and the
MCP-stdio→model leg is closed**: a model drove it live for ~213 calls on 2026-07-16 (field report below).
M5's "first real adoption against an AGT prototype" is **done** — that pass *was* it, and it found four
fish-bowl bugs. The **Mode-B active trace is built and verified** (2026-07-16, §below), and docs are
synced. **All that is left in M5 is the optional C# scenario facade**, which is recommended closed as
YAGNI — see the Mode-B section. On that recommendation, **M5 is effectively complete**.

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
- **`GTH.M5`** ~~Optional Mode-B trace (`GTH.D3`)~~ **✅ built 2026-07-16**, ~~docs~~ ✅, ~~first real
  adoption against an AGT prototype~~ ✅ (the 2026-07-16 observatory pass was it). **Remaining: the
  optional C# scenario facade (`GTH.D2`) only** — see the note below.

### Mode B — the observed trace (`GTH.D3`, built 2026-07-16)

`click_at`/`click_element` take **`trace: true`**. It connects to **every** visible non-IGNORE Control's
`gui_input` — deliberately not just Mode A's candidates, because *a trace that can only see what the
prediction predicted cannot falsify it*, and falsifying it is the entire point: `GTH.B4` was Mode A
naming a consumer, with complete confidence, that never received the event. The report carries
`consumer_observed`, `consumer_predicted`, `agrees_with_mode_a`, and a `disagreement` note telling the
reader to believe the trace.

**Best-effort, and honest about which part:** Godot emits `gui_input` *before* a Control runs its own
`_gui_input`, so a per-control "did you consume it?" flag is not available — the consumer is inferred
from where the chain **stops**, and `handled_on_arrival` reports whether the event was already spent
when it got there. Input consumed outside the GUI dispatch (a raw `_input` handler) is invisible to it.
Verified: on `btn-step` and a roster-Tree location click, Mode B watched 52 controls and **agreed with
Mode A both times** — which is the expected result. Mode A is usually right; the point is that its being
wrong is now *detectable* instead of silent.

**On the C# scenario facade (`GTH.D2`'s "optional thin facade"):** deliberately **not built**, flagged
for a ruling rather than silently dropped. It has no consumer — the one project using GTH authors its
scenarios in JSON, and the format is shared by both drivers by design (`GTH.R2`). Building an authoring
surface for a user who does not exist adds maintained API surface against a guess, and this repo's
zero-dependency ethos argues the same way. Recommend closing it as YAGNI; revisit if a C#-first project
actually adopts the harness.

## Bugs (`GTH.B*`) — **all six fixed & verified 2026-07-16**

`GTH.B1`–`B4` came out of the harness's first use in anger (the field report at the bottom of this file);
`GTH.B5`–`B6` are Panda's, added 2026-07-16. All six are now closed, covered by
`../adventuring-guild-teller/fishbowl/tests/harness/regression-b1-b6.json` (**green: 32 steps, 0
failures**) and by an extended `--selftest`. The analysis is kept in full rather than deleted — it is why
the fixes are shaped the way they are, and `B5`/`B6` are the standing reason two guards exist that a
future reader would otherwise be tempted to remove.

**The through-line, and the reason these were fixed as a set rather than as four patches:** every one of
`B1`–`B4` fails toward *false reassurance* — a false `clickable: true`, a dropped argument reported as
success, a confidently wrong consumer. This repo has already ruled on that failure direction once: the
sha-vs-phash call (DEV-LOG 2026-07-15) held that **for a test harness a false "unchanged" is the dangerous
direction**, and demoted the perceptual hash to advisory for exactly this reason. `B1`–`B4` are that same
ruling arriving in the other three modules. The harness is allowed to be wrong; it is not allowed to be
*confidently* wrong. `B6` then turned out to be the *literal* false-"unchanged" all over again — measured,
not theorised (below).

- **`GTH.B1` — `query_element` reports a false `clickable` / `on_screen`.** `scene_probe.gd`'s `_onscreen`
  is `Rect2(Vector2.ZERO, _vsize()).intersects(grect)` — *any* overlap counts, so a button spanning
  x=1276–1366 in a 1280 viewport is "on screen" on the strength of 4px. There is a second half the field
  report missed: the anchor (`grect.position + grect.size * anchor`, centre by default) is **unclamped**, so
  the hit-stack gets tested at x=1321 — outside the window, yet still inside the button's rect, so it
  reaches the control, finds no occluder, and returns `is_top_hit: true`. That is why the harness said
  "clickable" about a coordinate where clicking demonstrably did nothing. Three-part fix: `on_screen` means
  *fully* on screen; add `visible_fraction` + `clipped` so a partly-visible control tells the truth instead
  of rounding to a boolean; clamp the anchor into the visible intersection so `click_element` lands on a
  pixel that is actually reachable. This **amends the `GTH.R5` predicate** — `clickable` decouples from
  `on_screen`, because a 4px sliver genuinely *is* clickable and the report's job is to stop implying it is
  fully reachable. Also reconcile `snapshot()`, which computes `clickable` with a *different, weaker*
  formula that omits `is_top_hit`: two callers get two answers today.
  **✅ Fixed.** `_onscreen` is now `encloses` (strict); `visible_fraction` / `clipped` / `anchor_clamped`
  added; the anchor is clamped into the visible part and `click_element` now aims at *that* point rather
  than re-deriving the raw centre (it did, which is why the click missed as well as the report lying);
  `snapshot` shares the one predicate. Geometry is measured per **owning viewport**, so a dialog's button
  is no longer measured against the root's size. **Verified against the real case:** post-day
  `btn-storylets` reports `on_screen=false, visible_fraction=0.133, clipped=[right], clickable=true`,
  anchor clamped 1323→1289, *"only 13% of it is inside the 1290x810 viewport"* — and the click lands.
- **`GTH.B2` — `capture` with `region` throws.** `Trying to assign value of type 'String' to a variable of
  type 'Array'` at `capturer.gd:39`. Region capture is unusable via MCP; full-frame is fine.
- **`GTH.B3` — `press_key` silently drops `repeat`.** `repeat: 6` presses once and reports success.
- **Root cause of `B2` + `B3`, found while triaging and *not* in the field report: the MCP tool schemas are
  incomplete, and the server drops unrecognised arguments in silence.** `McpServer.cs`'s `Tools` table
  declares no `region` or `annotate` on `capture` and no `repeat` on `press_key`; `Pick()` then filters
  arguments against a hardcoded allowlist and **discards anything not on it, without a word**. So `repeat`
  was never implemented at *any* layer — schema, picker, core, or injector — and a caller passing it gets a
  success result for work never done. `region` survives `Pick`, but undeclared it arrives shaped however
  the caller guessed. The fix is systemic rather than two patches: complete the schemas, implement `repeat`
  end-to-end, accept a tolerant `region`, and **make an unrecognised argument say so** instead of vanishing.
  `--selftest` passed neither argument, which is precisely why both shipped.
  **✅ Fixed, and the drift is now structurally impossible.** The three copies of the contract (schema,
  `Pick`'s allowlist, `Map`'s per-tool key list) collapsed to **two**: `Map` forwards exactly what
  `Accepts` lists, and **`ContractErrors()` proves the schema and `Accepts` agree at startup**, failing
  `--selftest` if they ever drift. An unrecognised argument now returns `gth_warning` naming it. `repeat`
  is implemented end-to-end and the result echoes the count actually injected; `region`/`annotate` are
  declared, tolerant (array / `"[x,y,w,h]"` / `"x,y,w,h"`), clamped to the frame, and reject garbage with
  a *named* error instead of a GDScript type crash.
  **The self-test gap was worse than "it never passed a region":** it called `bridge.CallAsync` directly,
  so `Map()` — the layer both bugs lived in — was untested *by construction*. A self-test that had passed
  a region would have handed the bridge a clean array and gone green while real MCP calls stayed broken.
  It now routes through `Map()`. Verified over the live wire: `capture region [0,0,320,100] → 320x100`,
  and `press_key repeat=2` fires the project's own F9 handler twice (its two `[capture]` log lines are the
  independent witness — the harness is not merely echoing the number back).
- **`GTH.B4` — the predictive hit-stack mis-attributes clicks over embedded `Window`s.** `scene_probe.gd`
  `continue`s past any `Window` child, so a click over a popup dialog is attributed to whatever
  main-viewport control sits underneath — it named a `CheckBox` as the consumer of a dialog-close click that
  control provably never received. The addon README documents embedded Windows as *excluded*, which
  undersells it: the failure is not a hole in the report, it is a wrong answer stated plainly. **Settles
  `GTH.Q1` for v1** — root viewport + embedded `Window`s, no arbitrary viewport auto-discovery.
  **✅ Fixed.** `hit_report` finds embedded `Window`s over the point, reports `viewport:
  "embedded_window"` + the window's identity, and walks *its* contents in *its* coords; `clickability`
  marks a main-viewport control under one `blocked_by_window` and not clickable. **Verified:** a
  `hit_test` at (0.5,0.5) with the storylet browser open now names a Button inside
  `AcceptDialog "Storylet browser — force-fire (debug)"` at rect [435,168,420,473] — where it previously
  named whatever main-viewport control happened to sit beneath the dialog.
- **`GTH.B5` — lock window resize/maximize while the harness is active.** *(Panda, 2026-07-16.)* Every GTH
  coordinate is normalized against `get_visible_rect().size`, and every geometry report is a snapshot of one
  layout at one size — so a mid-session resize silently invalidates cached rects, normalized coordinates,
  and any `rect_px` the caller is still holding. Lock resize + maximize on activation, restore on stop, and
  if the size changes anyway, **say so in the report** rather than quietly answering from the new size.
  **✅ Fixed.** `WINDOW_FLAG_RESIZE_DISABLED` on activation (which also greys out maximize on Windows),
  restored on `_exit_tree`; `lock_window` config, default on. `snapshot` / `capture` / `window_state` carry
  a `window_warning` if the size drifts anyway, and a new `window_state` command reports mode/size/focus/
  lock. **The first run of the regression scenario immediately caught a false positive in this very
  guard** — baselining the size in `_ready()` reads the *requested* 1280x800 from `project.godot`, which
  the platform then adjusts to 1290x810 before the first command, so the drift warning fired on every
  session. Baseline is now sampled lazily on first use. A warning that always fires is a warning nobody
  reads — which is the same disease as a green test nobody questions.
- **`GTH.B6` — does minimize-to-taskbar break the harness? Detect and compensate if so.** *(Panda,
  2026-07-16.)* Suspected to be `GTH.D7`'s constraint arriving at *runtime*: D7 established that a renderer
  with no framebuffer cannot produce pixels, and a minimized window on Windows may stop presenting — in
  which case `get_texture().get_image()` returns a stale or blank frame. That is the dangerous direction
  again: a stale frame is byte-identical to its predecessor, so `if_changed` dedup would answer
  **`changed: false`** — the harness reporting "nothing happened" when the truth is "I cannot see."
  Investigate first, then detect (`DisplayServer.window_get_mode()`) and compensate.
  **✅ Answered: YES, minimize breaks it — and the suspicion above was exactly right.** Measured with a
  controlled step in the regression scenario (`allow_minimized`, a deliberate escape hatch, exists so
  this is an experiment rather than a story): capture a frame, **minimize**, click `btn-step`, read the
  clock — *the clock advances to `Day 2 · 00:30 (slot 1)`, so input is entirely unaffected* — then
  capture again. **The sha comes back byte-identical (`2c6be602759f5fc5`) to the pre-minimize frame**,
  while the post-restore frame is `fe5149114b74d101`. The framebuffer freezes while the game keeps
  running. With the default `if_changed: true` that identical sha **would have deduped to
  `changed: false`** — a false "unchanged", the precise failure the sha-vs-phash ruling exists to
  prevent, arriving through a door nobody was watching. **Fixed:** `_ensure_presentable()` restores the
  window before any capture and flags `window_was_minimized`; `restore_on_minimize` config, default on;
  refusing rather than shooting when it is off; `allow_minimized` shoots anyway and carries a loud
  warning that `changed` cannot be trusted.

## Open questions (`GTH.Q*`)

- **`GTH.Q1`** ~~Multi-viewport / multi-window games: does v1 target only the root viewport + declared
  `SubViewport`s, or auto-discover every viewport?~~ **Answered by `GTH.B4`** — the question stopped being
  academic the moment it arrived as a bug. v1 covers the root viewport + embedded `Window`s (which is what
  a popup dialog is, and what the mis-attribution was about); arbitrary viewport auto-discovery stays out.
- **`GTH.Q2`** 3D pickable objects — is `intersect_ray` hit-reporting in scope for v1, or 2D/Control-only
  first? (AGT prototypes are 2D; the harness claims general reuse.)
- **`GTH.Q3`** Determinism hooks — should GTH also expose seed-pinning / fixed-timestep stepping (many
  .NET/GD projects, incl. the fishbowl, run a seeded core), or stay strictly input+capture?
- **`GTH.Q4`** Convergence: once GTH exists, do `morning-queue/` and `fishbowl/` retire their bespoke
  DevHarnesses onto it — and does that finally justify the shared library their isolation rule has so
  far deferred? (Post-v1, Panda's call.)

## Field report — first real use in anger (2026-07-16)

The live MCP path was driven hard for the first time since its `--selftest` verification: a full
functional pass over the fish-bowl observatory (~213 tool calls, ~9 relaunches, every `test_id` in the
table, four A/B experiments). **The harness held up — it found four genuine bugs in the fish-bowl,
including a shipped feature that had never worked.** Launch mode, boot, `session_start`/`stop`,
`snapshot`, `click_element`, `click_at`, `read_element`, `wait_for`, and sha-dedup `capture` all
behaved. That is the headline: it did its job.

**Four defects in GTH itself**, worth fixing before the next build leans on it — tracked as
**`GTH.B1`–`B4`** above and **all fixed 2026-07-16**. Triage added two things this report did not have:
`B2` and `B3` turned out to share one root cause (the server silently drops undeclared arguments), and
`B1` has a second half (the unclamped anchor) that this report missed.

- **`capture` with `region` is broken.** Throws `Trying to assign value of type 'String' to a variable
  of type 'Array'` at `capturer.gd:39` — **the .NET MCP server marshals the region array across the
  wire as a String**, while `capturer.gd` expects `[x,y,w,h]`. A server-side marshalling bug, not a
  GDScript one. Region capture is unusable via MCP today (full-frame capture is fine).
- **`press_key` silently ignores `repeat`.** `repeat: 6` applied exactly one step. Silent, so a caller
  reads the result as "the key did nothing" rather than "the harness dropped 5 of 6".
- **`query_element` does not clamp to the viewport** — it reported `on_screen: true` and
  `clickable: true` for a button occupying x=1276–1366 in a **1280**-wide viewport. It was in fact
  4px reachable. **This is the dangerous one for a *test* harness**: a false "clickable" is the same
  failure direction as the false "unchanged" that the sha-vs-phash decision (DEV-LOG 2026-07-15)
  already rejected once. `on_screen` should mean on screen.
- **The predictive hit-stack mis-attributes clicks over embedded Windows** — it claimed a `CheckBox`
  consumed a dialog-close click that it demonstrably did not (the frame was byte-identical before and
  after). Correctly documented as a Mode A limitation in the addon README, but it cost real debugging
  time before that caveat was found.

**Method note worth keeping:** the pass was framed as *"static analysis made four claims; you are the
behavioral check on them"* — and behavior confirmed all four (the `storylet_rate` no-op above 1.0, the
two knobs with no dial, the live-without-restart claim). **Pairing a code reader with a harness pass
that tries to falsify it is cheap and it works.** The harness earns its keep as an *epistemic* tool,
not just a clicking one.

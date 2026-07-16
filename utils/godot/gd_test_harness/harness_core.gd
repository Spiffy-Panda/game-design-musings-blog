extends Node
## GTH · HarnessCore — the autoload façade and the ONE place the capabilities live. Both
## drivers are thin over this: the prescripted ScenarioRunner and the live WebSocket Bridge
## call the same command API. Holds InputInjector / SceneProbe / Capturer.
##
## ACTIVATION (else fully inert — no process, no socket, no input; ships disabled):
##   cmdline:  -- --gth-enable --gth-serve --gth-port=8787 --gth-scenario=res://… --gth-exit-after
##   env:      GTH_ENABLE / GTH_SERVE / GTH_PORT / GTH_SCENARIO / GTH_EXIT_AFTER
##   file:     res://harness.local.json  {enable, serve, port, scenario, exit_after}  (gitignored)
##
## Reusable across any Godot project: copy addons/gd_test_harness/, add this as an autoload
## named `TestHarness`, optionally drop a harness.config.json at the project root.

const _InputInjector := preload("res://addons/gd_test_harness/input_injector.gd")
const _SceneProbe := preload("res://addons/gd_test_harness/scene_probe.gd")
const _Capturer := preload("res://addons/gd_test_harness/capturer.gd")
const _Bridge := preload("res://addons/gd_test_harness/bridge.gd")
const _ScenarioRunner := preload("res://addons/gd_test_harness/scenario_runner.gd")

var _cfg := {}
var _injector
var _probe
var _capturer
var _bridge
var _active := false
var _win_locked := false
var _win_prev_resize_disabled := false
var _size_at_start := Vector2i.ZERO

func _ready() -> void:
	var act := _activation()
	if not act.get("enable", false) and act.get("scenario", "") == "" and not act.get("serve", false):
		set_process(false)
		return  # inert — normal play is untouched
	_active = true
	_cfg = _load_config()
	Input.use_accumulated_input = false  # deterministic: no buffered-motion coalescing
	_injector = _InputInjector.new()
	_probe = _SceneProbe.new(self, str(_cfg.get("test_id_meta", "test_id")))
	_capturer = _Capturer.new(self, _cfg)
	_lock_window()
	print("[GTH] harness active (serve=%s scenario=%s)" % [act.get("serve", false), act.get("scenario", "")])

	if act.get("serve", false):
		_bridge = _Bridge.new()
		add_child(_bridge)
		_bridge.start(self, int(act.get("port", _cfg.get("port", 8787))))

	if act.get("scenario", "") != "":
		await _boot_settle()
		var runner = _ScenarioRunner.new(self)
		var spec = _load_json(act["scenario"])
		var res = await runner.run(spec)
		print("[GTH] RESULT " + JSON.stringify(res))
		if act.get("exit_after", false):
			get_tree().quit(0 if res.get("ok", false) else 1)
	elif act.get("exit_after", false):
		get_tree().quit(0)

# ============================================================================================
# Command API — the seam both drivers sit on. All coordinates accept normalized (0..1) unless
# the opts carry `normalized=false`. All return JSON-able Dictionaries.
# ============================================================================================

func snapshot(filter := {}) -> Dictionary:
	var s: Dictionary = _probe.snapshot(filter)
	var drift := _size_drift()
	if not drift.is_empty():
		s["window_warning"] = drift
	return s

func query_element(handle: Dictionary) -> Dictionary:
	var hits: Array = _probe.resolve(handle)
	if hits.is_empty():
		return {exists = false, handle = handle}
	var d: Dictionary = _probe.clickability(hits[0])
	d["candidate_count"] = hits.size()
	if hits.size() > 1:
		d["candidates"] = _paths(hits)
	return d

func read_element(handle: Dictionary) -> Dictionary:
	var hits: Array = _probe.resolve(handle)
	if hits.is_empty():
		return {exists = false, handle = handle}
	var n = hits[0]
	var out := {exists = true, path = str(get_tree().root.get_path_to(n)), type = n.get_class()}
	if n is Range:
		out["value"] = (n as Range).value
	if n is Button or n is Label or n is LineEdit or n is TextEdit:
		out["text"] = str(n.get("text"))
	out["candidate_count"] = hits.size()
	return out

func hit_test(x: float, y: float, normalized := true) -> Dictionary:
	return _probe.hit_report(_px(x, y, normalized))

func click_at(x: float, y: float, opts := {}) -> Dictionary:
	var normalized: bool = opts.get("normalized", true)
	var p := _px(x, y, normalized)
	var report: Variant = _probe.hit_report(p) if opts.get("report_hits", true) else null
	var watched: int = _probe.begin_trace() if opts.get("trace", false) else 0
	_injector.click_at(p, _button(opts), int(opts.get("clicks", 1)))
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	var out := {clicked = true, clicked_px = [p.x, p.y], hits = report}
	if opts.get("trace", false):
		out["trace"] = _traced(report, watched)
	return out

func click_element(handle: Dictionary, opts := {}) -> Dictionary:
	var hits: Array = _probe.resolve(handle)
	if hits.is_empty():
		return {clicked = false, error = "element not found", handle = handle}
	if hits.size() > 1 and not opts.has("index"):
		return {clicked = false, error = "ambiguous (%d matches)" % hits.size(), candidates = _paths(hits)}
	var node = hits[int(opts.get("index", 0))]
	var a: Array = opts.get("anchor", [0.5, 0.5])
	var report: Dictionary = _probe.clickability(node, Vector2(float(a[0]), float(a[1])))
	if not report.get("clickable", false) and not opts.get("force", false):
		report["clicked"] = false
		# GTH.B7 — an `error` key, NOT a `note`. A refusal is a failure: the caller asked for a
		# click and did not get one. As a `note` it had no key any driver checked, so the
		# ScenarioRunner walked straight past it and a scenario could go green having clicked
		# nothing at all. Found the day morning-queue adopted the harness: 6 of its 17 stamp
		# clicks were refused (the desk disables its stamps between visitors) and the run
		# reported zero failures. Same silent-success family as B2/B3.
		report["error"] = "refused — not clickable (%s). Pass force=true to click anyway, or " \
			% str(report.get("offscreen_reason", report.get("factors", {}))) \
			+ "wait_for {element_clickable} first if it is merely not ready yet."
		return report
	# Aim where the probe says a click would ACTUALLY land: `anchor_point_px` is already
	# clamped into the on-screen part of the rect. Re-deriving the raw centre from rect_px
	# here (what this line used to do) is the half of GTH.B1 that made the click miss — the
	# report lied AND the click obediently flew out of the window after it.
	var ap: Array = report.get("anchor_point_px", [0, 0])
	var p := Vector2(float(ap[0]), float(ap[1]))
	var hits_report: Dictionary = _probe.hit_report(p)
	var watched: int = _probe.begin_trace() if opts.get("trace", false) else 0
	_injector.click_at(p, _button(opts), int(opts.get("clicks", 1)))
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	var out := {clicked = true, target = report.get("text", report.get("path", "")),
		clicked_px = [p.x, p.y], clickability = report, hits = hits_report}
	if opts.get("trace", false):
		out["trace"] = _traced(hits_report, watched)
	return out

## Close a Mode-B trace and score it against what Mode A predicted (GTH.D3 / GTH.M5).
func _traced(hits: Variant, watched: int) -> Dictionary:
	var predicted = hits.get("consumer", null) if typeof(hits) == TYPE_DICTIONARY else null
	var tr: Dictionary = _probe.trace_report(_probe.end_trace(), predicted)
	tr["controls_watched"] = watched
	return tr

func move_to(x: float, y: float, opts := {}) -> Dictionary:
	var p := _px(x, y, opts.get("normalized", true))
	_injector.move_to(p)
	await _frames(1)
	return {moved_px = [p.x, p.y]}

func drag(from: Array, to: Array, opts := {}) -> Dictionary:
	var norm: bool = opts.get("normalized", true)
	var a := _px(from[0], from[1], norm)
	var b := _px(to[0], to[1], norm)
	_injector.drag(a, b, _button(opts), int(opts.get("steps", 6)))
	await _frames(int(opts.get("post_frames", 2)))
	return {dragged = [[a.x, a.y], [b.x, b.y]]}

## GTH.B3 — `repeat` presses the key N times. It used to be accepted-and-ignored at every
## layer (the MCP schema never declared it, Pick() dropped it, and neither this method nor the
## injector had ever heard of it), so `repeat: 6` pressed once and returned success. The
## returned `repeat` is the count actually injected: the caller should never have to take our
## word for it.
func press_key(keys: String, opts := {}) -> Dictionary:
	var m := _keymap(keys)
	if m.is_empty():
		return {error = "unknown key '%s'" % keys}
	var n: int = maxi(1, int(opts.get("repeat", 1)))
	for i in n:
		_injector.press_key(m["keycode"], m["unicode"],
			opts.get("shift", false), opts.get("ctrl", false), opts.get("alt", false), opts.get("meta", false))
		if i < n - 1:  # let each press land as its own event, not 6 coalesced into one frame
			await _frames(int(opts.get("repeat_frames", 1)))
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	return {key = keys, repeat = n}

func send_action(action: String, opts := {}) -> Dictionary:
	if opts.has("pressed"):
		_injector.send_action(action, bool(opts["pressed"]), float(opts.get("strength", 1.0)))
	else:  # tap
		_injector.send_action(action, true)
		_injector.send_action(action, false)
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	return {action = action}

func capture(opts := {}) -> Dictionary:
	var w: Dictionary = await _ensure_presentable(opts)
	if w.has("error"):
		return w
	var r: Dictionary = await _capturer.capture(opts)
	if w.has("was_minimized"):
		r["window_was_minimized"] = true
		r["note"] = "window was minimized and restored before this capture; " + str(r.get("note", ""))
	if w.has("minimized_unguarded"):
		r["window_minimized"] = true
		r["warning"] = "captured while MINIMIZED (allow_minimized) — this frame may be stale or " \
			+ "blank, and `changed` cannot be trusted: a stale frame dedups as 'unchanged'."
	var drift := _size_drift()
	if not drift.is_empty():
		r["window_warning"] = drift
	return r

## Window state (GTH.B5/B6). No args = report. `minimize`/`restore` exist so a scenario can
## reproduce the minimize case B6 exists to handle — verifying the compensation needs a way to
## induce the condition. Note the asymmetry that looks odd but isn't: the harness may set the
## window's MODE, while B5 locks out *resizing* — because a resize silently invalidates every
## coordinate we have handed out, and minimizing doesn't.
func window_state(opts := {}) -> Dictionary:
	if _headless():
		return {headless = true, note = "no window in a headless session"}
	if opts.get("minimize", false):
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MINIMIZED)
		await _frames(3)
	elif opts.get("restore", false):
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		await _frames(3)
	return _window_report()

func wait_for(opts := {}) -> Dictionary:
	if opts.has("ms"):
		await get_tree().create_timer(float(opts["ms"]) / 1000.0).timeout
		return {waited_ms = opts["ms"]}
	if opts.has("settled_ms"):
		await _capturer._settle(int(opts["settled_ms"]))
		return {settled = true}
	# GTH.B8 — `element_clickable` as well as `element_visible`. Visible is not ready: a desk
	# that disables its stamp buttons between visitors has them visible-but-dead for a beat, and
	# waiting on "visible" sails through and clicks a disabled button. Waiting for the thing you
	# are about to do is the only wait that means anything.
	if opts.has("element_visible") or opts.has("element_clickable"):
		var want_clickable: bool = opts.has("element_clickable")
		var handle: Dictionary = opts.get("element_clickable", opts.get("element_visible", {}))
		var deadline := float(opts.get("timeout_ms", 3000)) / 1000.0
		var elapsed := 0.0
		var last := {}
		while elapsed < deadline:
			last = query_element(handle)
			var ready: bool = last.get("clickable", false) if want_clickable \
				else (last.get("exists", false) and last.get("factors", {}).get("visible", false))
			if ready:
				return {ok = true, waited_ms = int(elapsed * 1000),
					waited_for = "clickable" if want_clickable else "visible"}
			await _frames(2)
			elapsed += 0.03
		# ...and a timeout is an ERROR, not a quiet {timed_out: true} nobody checks (B7 again:
		# the old shape had no key any driver treated as failure).
		return {error = "timed out after %dms waiting for %s to be %s — last seen: %s"
			% [int(deadline * 1000), str(handle),
				"clickable" if want_clickable else "visible",
				str(last.get("factors", "element not found"))]}
	await _frames(1)
	return {waited = true}

func run_scenario(spec: Variant) -> Dictionary:
	var runner = _ScenarioRunner.new(self)
	return await runner.run(spec)

# ============================================================================================
# internals
# ============================================================================================

# --- window guard (GTH.B5 / GTH.B6) ---------------------------------------------------------

func _headless() -> bool:
	return DisplayServer.get_name() == "headless"

## GTH.B5 — every GTH coordinate is normalized against get_visible_rect().size, and every
## geometry report describes one layout at one size. A resize mid-session silently invalidates
## all of it at once: the rect_px a caller is still holding, the normalized coords baked into a
## scenario file, the lot. So while the harness is active, the window does not resize.
## RESIZE_DISABLED also greys out the maximize box on Windows, which covers the other half.
func _lock_window() -> void:
	if _headless() or not bool(_cfg.get("lock_window", true)):
		return
	_win_prev_resize_disabled = DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_RESIZE_DISABLED)
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_RESIZE_DISABLED, true)
	_win_locked = true
	# The baseline size is sampled lazily, on first use — NOT here. At _ready the window is
	# not yet realised, so window_get_size() hands back the requested size from project.godot
	# (1280x800) which the platform then adjusts (to 1290x810 on this box) before the first
	# command runs. Baselining eagerly made _size_drift() cry wolf on every single session,
	# which the first run of tests/harness/regression-b1-b6.json caught. A warning that always
	# fires is a warning nobody reads — the same way a green test nobody questions hid
	# btn-generate for a whole release.
	print("[GTH] window lock on (resize/maximize disabled while the harness is active)")

func _unlock_window() -> void:
	if not _win_locked or _headless():
		return
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_RESIZE_DISABLED, _win_prev_resize_disabled)
	_win_locked = false

func _exit_tree() -> void:
	_unlock_window()

## Non-empty when the window changed size despite the lock. B5 has two halves — prevent it,
## and if it somehow happens anyway, SAY SO, rather than quietly answering from the new size
## as though every coordinate already handed out were still good.
func _size_drift() -> Dictionary:
	if _headless() or not _win_locked:
		return {}
	var now := DisplayServer.window_get_size()
	if _size_at_start == Vector2i.ZERO:  # first observation IS the baseline (see _lock_window)
		_size_at_start = now
		print("[GTH] window baseline %dx%d" % [now.x, now.y])
		return {}
	if now == _size_at_start:
		return {}
	return {resized = true, from = [_size_at_start.x, _size_at_start.y], to = [now.x, now.y],
		note = "the window changed size mid-session despite the lock — every rect_px and "
			+ "normalized coordinate issued before now was measured against the old size"}

func _mode_name(m: int) -> String:
	match m:
		DisplayServer.WINDOW_MODE_WINDOWED: return "windowed"
		DisplayServer.WINDOW_MODE_MINIMIZED: return "minimized"
		DisplayServer.WINDOW_MODE_MAXIMIZED: return "maximized"
		DisplayServer.WINDOW_MODE_FULLSCREEN: return "fullscreen"
		DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN: return "exclusive_fullscreen"
	return "?"

func _window_report() -> Dictionary:
	var drift := _size_drift()  # first, so it can baseline before we report size_at_start
	var mode := DisplayServer.window_get_mode()
	var size := DisplayServer.window_get_size()
	var vp := get_tree().root.get_visible_rect().size
	var r := {
		mode = _mode_name(mode),
		minimized = mode == DisplayServer.WINDOW_MODE_MINIMIZED,
		size_px = [size.x, size.y],
		viewport_px = [vp.x, vp.y],
		focused = DisplayServer.window_is_focused(),
		resize_locked = _win_locked,
		size_at_start = [_size_at_start.x, _size_at_start.y],
	}
	if not drift.is_empty():
		r["window_warning"] = drift
	return r

## GTH.B6 — a minimized window stops presenting, so the framebuffer goes stale and get_image()
## hands back the last frame it managed to draw. That frame is byte-identical to its
## predecessor, so `if_changed` dedup answers `changed: false` — the harness reporting "nothing
## happened" when what it actually means is "I cannot see". Same false-reassurance direction as
## B1/B4, and the reason this guard refuses to shoot and hope.
func _ensure_presentable(opts := {}) -> Dictionary:
	if _headless():
		return {}
	if DisplayServer.window_get_mode() != DisplayServer.WINDOW_MODE_MINIMIZED:
		return {}
	# Escape hatch, and the reason B6 is an answer rather than an assumption: without a way to
	# shoot a minimized window on purpose, "minimize breaks capture" stays a plausible story.
	# It carries a loud warning precisely because the result cannot be trusted.
	if bool(opts.get("allow_minimized", false)):
		return {minimized_unguarded = true}
	if not bool(_cfg.get("restore_on_minimize", true)):
		return {error = "window is minimized — it is not presenting, so a capture would be a stale "
			+ "or blank frame that dedup would then call 'unchanged'. Restore the window, or leave "
			+ "restore_on_minimize on."}
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	await _frames(3)
	await get_tree().create_timer(0.15).timeout  # let the compositor actually present a frame
	return {was_minimized = true}

# --- misc -----------------------------------------------------------------------------------

func _px(x: float, y: float, normalized: bool) -> Vector2:
	if normalized:
		return Vector2(x, y) * get_tree().root.get_visible_rect().size
	return Vector2(x, y)

func _button(opts: Dictionary) -> int:
	var b = opts.get("button", "left")
	if typeof(b) == TYPE_INT:
		return b
	match str(b):
		"right": return MOUSE_BUTTON_RIGHT
		"middle": return MOUSE_BUTTON_MIDDLE
	return MOUSE_BUTTON_LEFT

func _frames(n: int) -> void:
	for i in max(1, n):
		await get_tree().process_frame

func _paths(nodes: Array) -> Array:
	var out := []
	for n in nodes:
		out.append({path = str(get_tree().root.get_path_to(n)), type = n.get_class(), text = str(n.get("text")) if ("text" in n) else ""})
	return out

const _KEYS := {
	"ENTER": KEY_ENTER, "RETURN": KEY_ENTER, "ESCAPE": KEY_ESCAPE, "ESC": KEY_ESCAPE,
	"TAB": KEY_TAB, "SPACE": KEY_SPACE, "BACKSPACE": KEY_BACKSPACE, "DELETE": KEY_DELETE,
	"UP": KEY_UP, "DOWN": KEY_DOWN, "LEFT": KEY_LEFT, "RIGHT": KEY_RIGHT, "HOME": KEY_HOME, "END": KEY_END,
	"F1": KEY_F1, "F2": KEY_F2, "F3": KEY_F3, "F5": KEY_F5, "F9": KEY_F9, "F10": KEY_F10,
}

func _keymap(keys: String) -> Dictionary:
	var up := keys.to_upper()
	if _KEYS.has(up):
		return {keycode = _KEYS[up], unicode = 0}
	if keys.length() == 1:
		return {keycode = keys.to_upper().unicode_at(0), unicode = keys.unicode_at(0)}
	return {}

func _boot_settle() -> void:
	await _frames(3)
	await get_tree().create_timer(0.3).timeout
	# safety: never let an --gth-exit-after run hang the window open forever
	get_tree().create_timer(60.0).timeout.connect(func(): if _active: get_tree().quit(2))

func _activation() -> Dictionary:
	var a := {enable = false, serve = false, port = 0, scenario = "", exit_after = false}
	# 3) local file (lowest of the explicit sources)
	var local := _read_json_file("res://harness.local.json")
	if typeof(local) == TYPE_DICTIONARY:
		for k in ["enable", "serve", "port", "scenario", "exit_after"]:
			if local.has(k): a[k] = local[k]
	# 2) env
	if OS.get_environment("GTH_ENABLE") != "": a.enable = true
	if OS.get_environment("GTH_SERVE") != "": a.serve = true
	if OS.get_environment("GTH_PORT") != "": a.port = int(OS.get_environment("GTH_PORT"))
	if OS.get_environment("GTH_SCENARIO") != "": a.scenario = OS.get_environment("GTH_SCENARIO")
	if OS.get_environment("GTH_EXIT_AFTER") != "": a.exit_after = true
	# 1) cmdline (highest) — scan engine args and user args (after `--`)
	var args := OS.get_cmdline_args() + OS.get_cmdline_user_args()
	for arg in args:
		if arg == "--gth-enable": a.enable = true
		elif arg == "--gth-serve": a.serve = true
		elif arg == "--gth-exit-after": a.exit_after = true
		elif arg.begins_with("--gth-port="): a.port = int(arg.split("=")[1])
		elif arg.begins_with("--gth-scenario="): a.scenario = arg.split("=", true, 1)[1]
	if a.scenario != "" or a.serve: a.enable = true
	return a

func _load_config() -> Dictionary:
	var defaults := {
		artifacts_dir = "res://.captures/gth", port = 8787, test_id_meta = "test_id",
		max_dim = 1280, format = "png", jpeg_quality = 0.8, if_changed = true, phash_threshold = 4,
		settle_ms = 250, settle_timeout_ms = 3000, settle_interval_ms = 60,
		warp_mouse = false, post_event_frames = 2, image_budget = 60, session_id = "s",
		lock_window = true,          # GTH.B5 — no resize/maximize while the harness is active
		restore_on_minimize = true,  # GTH.B6 — un-minimize rather than capture a stale frame
	}
	var loaded := _read_json_file("res://harness.config.json")
	if typeof(loaded) == TYPE_DICTIONARY:
		_merge(defaults, loaded)
	return defaults

func _merge(base: Dictionary, over: Dictionary) -> void:
	for k in over:
		if typeof(over[k]) == TYPE_DICTIONARY and typeof(base.get(k)) == TYPE_DICTIONARY:
			_merge(base[k], over[k])
		else:
			base[k] = over[k]

func _load_json(path_or_inline: Variant) -> Variant:
	if typeof(path_or_inline) != TYPE_STRING:
		return path_or_inline  # already an inline spec
	return _read_json_file(path_or_inline)

func _read_json_file(res_path: String) -> Variant:
	var abs := ProjectSettings.globalize_path(res_path)
	if not FileAccess.file_exists(abs):
		return null
	var f := FileAccess.open(abs, FileAccess.READ)
	if f == null:
		return null
	var txt := f.get_as_text()
	f.close()
	return JSON.parse_string(txt)

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
	return _probe.snapshot(filter)

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
	_injector.click_at(p, _button(opts), int(opts.get("clicks", 1)))
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	return {clicked = true, clicked_px = [p.x, p.y], hits = report}

func click_element(handle: Dictionary, opts := {}) -> Dictionary:
	var hits: Array = _probe.resolve(handle)
	if hits.is_empty():
		return {clicked = false, error = "element not found", handle = handle}
	if hits.size() > 1 and not opts.has("index"):
		return {clicked = false, error = "ambiguous (%d matches)" % hits.size(), candidates = _paths(hits)}
	var node = hits[int(opts.get("index", 0))]
	var report: Dictionary = _probe.clickability(node)
	if not report.get("clickable", false) and not opts.get("force", false):
		report["clicked"] = false
		report["note"] = "refused — not clickable (pass force=true to click anyway)"
		return report
	var grect: Array = report.get("rect_px", [0, 0, 0, 0])
	var anchor: Array = opts.get("anchor", [0.5, 0.5])
	var p := Vector2(grect[0] + grect[2] * float(anchor[0]), grect[1] + grect[3] * float(anchor[1]))
	var hits_report: Dictionary = _probe.hit_report(p)
	_injector.click_at(p, _button(opts), int(opts.get("clicks", 1)))
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	return {clicked = true, target = report.get("text", report.get("path", "")),
		clicked_px = [p.x, p.y], clickability = report, hits = hits_report}

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

func press_key(keys: String, opts := {}) -> Dictionary:
	var m := _keymap(keys)
	if m.is_empty():
		return {error = "unknown key '%s'" % keys}
	_injector.press_key(m["keycode"], m["unicode"],
		opts.get("shift", false), opts.get("ctrl", false), opts.get("alt", false), opts.get("meta", false))
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	return {key = keys}

func send_action(action: String, opts := {}) -> Dictionary:
	if opts.has("pressed"):
		_injector.send_action(action, bool(opts["pressed"]), float(opts.get("strength", 1.0)))
	else:  # tap
		_injector.send_action(action, true)
		_injector.send_action(action, false)
	await _frames(int(opts.get("post_frames", _cfg.get("post_event_frames", 2))))
	return {action = action}

func capture(opts := {}) -> Dictionary:
	return await _capturer.capture(opts)

func wait_for(opts := {}) -> Dictionary:
	if opts.has("ms"):
		await get_tree().create_timer(float(opts["ms"]) / 1000.0).timeout
		return {waited_ms = opts["ms"]}
	if opts.has("settled_ms"):
		await _capturer._settle(int(opts["settled_ms"]))
		return {settled = true}
	if opts.has("element_visible"):
		var deadline := float(opts.get("timeout_ms", 3000)) / 1000.0
		var elapsed := 0.0
		while elapsed < deadline:
			var q := query_element(opts["element_visible"])
			if q.get("exists", false) and q.get("factors", {}).get("visible", false):
				return {visible = true, waited_ms = int(elapsed * 1000)}
			await _frames(2)
			elapsed += 0.03
		return {visible = false, timed_out = true}
	await _frames(1)
	return {waited = true}

func run_scenario(spec: Variant) -> Dictionary:
	var runner = _ScenarioRunner.new(self)
	return await runner.run(spec)

# ============================================================================================
# internals
# ============================================================================================

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

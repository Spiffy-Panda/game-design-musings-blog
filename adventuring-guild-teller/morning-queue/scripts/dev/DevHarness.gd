extends Node
## DevHarness — a validation aid, NOT shipped game logic.
##
## Two jobs, both to make checking the desk easy without fighting the OS screenshotter:
##   1. CAPTURE — writes a PNG of the whole game viewport to `capture_dir` (a plain folder
##      on disk that a human or an agent can just read). Press F12 any time for a manual
##      shot; when auto-stepping it also shoots one frame per visitor + the summary.
##   2. AUTO-STEP — when `enabled`, it walks the whole shift on a timer, feeding each
##      visitor a stamp by invoking the SAME handler a real button-press fires
##      (Main._on_stamp_chosen), so it exercises the true submit→advance path.
##
## DISABLE FOR MANUAL PLAY (what you asked for): in the editor, open scenes/Main.tscn,
## select the "DevHarness" node, and untick **Enabled** in the Inspector (or set it false).
## With Enabled off, the harness does nothing but keep the F12 manual-capture hotkey.
##
## Default stamps = each visitor's CORRECT verdict (so a clean run shoots the whole queue
## and ends on 16/16). Put explicit stamps in `actions` to script a specific case, e.g.
## ["approve","reject", ...] — index i pairs with visitor i.

@export var enabled: bool = true
## Seconds to wait before each scheduled activation (the "schedule" you asked for).
@export var step_delay: float = 0.6
## Shoot one frame per visitor + a summary frame. Off = only F12 manual shots.
@export var capture_each_step: bool = true
## Exit the game once the scripted run finishes (handy for a fire-and-forget capture pass).
@export var quit_when_done: bool = false
## Where PNGs land. res://.captures maps to <project>/.captures/ — carries a .gdignore so
## Godot never imports the shots, and it is .gitignored.
@export var capture_dir: String = "res://.captures"
## Optional explicit stamp script; empty = auto-play each visitor's correct verdict.
@export var actions: Array[String] = []

var _stamp_handler: Callable
var _shot: int = 0


func _ready() -> void:
	_ensure_dir()


func _unhandled_input(event: InputEvent) -> void:
	# Manual capture works whether or not the harness is enabled.
	if event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_F12:
		await _capture("manual_%02d" % _shot)


## Called by Main once the desk is built. `handler` is Main._on_stamp_chosen — the exact
## Callable the VerdictBar's stamp_chosen signal is wired to, so we drive the real path.
func begin(handler: Callable) -> void:
	if not enabled:
		return
	_stamp_handler = handler
	_run.call_deferred()


func _run() -> void:
	print("[harness] auto-step start — %d visitors, %.2fs step" % [Deck.count(), step_delay])
	var i := 0
	while Session.index < Deck.count():
		var v := Session.current()
		if v.is_empty():
			break
		var stamp := _stamp_for(i, v)
		if capture_each_step:
			await _capture("%02d_%s_%s" % [i + 1, str(v.get("id", "?")), stamp])
		await get_tree().create_timer(step_delay).timeout
		if _stamp_handler.is_valid():
			_stamp_handler.call(stamp)   # == pressing that stamp
		else:
			Session.submit(stamp)
			Session.advance()
		await get_tree().process_frame
		i += 1
	if capture_each_step:
		await _capture("99_summary")
	print("[harness] done — %d captures -> %s" % [_shot, ProjectSettings.globalize_path(capture_dir)])
	if quit_when_done:
		get_tree().quit()


## The stamp to apply to visitor `i`: an explicit override if scripted, else the correct
## verdict for the current desk mode (binary vs four-stamp).
func _stamp_for(i: int, v: Dictionary) -> String:
	if i < actions.size():
		return actions[i]
	var truth: Dictionary = v.get("truth", {})
	return str(truth.get("binary", "reject")) if Session.STRICT_BINARY else str(truth.get("stamp", "reject"))


func _capture(shot_name: String) -> void:
	await RenderingServer.frame_post_draw   # let the frame finish so the grab is complete
	var img := get_viewport().get_texture().get_image()
	if img == null:
		push_warning("[harness] viewport image was null for " + shot_name)
		return
	var path := "%s/%s.png" % [capture_dir, shot_name]
	var err := img.save_png(path)
	if err != OK:
		push_warning("[harness] save_png failed (%d) for %s" % [err, path])
		return
	_shot += 1


func _ensure_dir() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(capture_dir))
	# .gdignore keeps the Godot importer from turning the shots into project assets.
	var gdignore := capture_dir + "/.gdignore"
	if not FileAccess.file_exists(gdignore):
		var f := FileAccess.open(gdignore, FileAccess.WRITE)
		if f:
			f.store_string("")
			f.close()

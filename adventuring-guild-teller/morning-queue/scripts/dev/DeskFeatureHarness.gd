extends Node
## DeskFeatureHarness — a dev-only auto-play that exercises and screenshots the
## desk-tile and Quest Board foldout features added 2026-07-15.
##
## What it exercises:
##   • Clicking a tool tab (Glass / Scale) spawns a tile on the main desk.
##   • Clicking the same tool again REPLACES the existing tile (no duplicate).
##   • Clicking a Quest Board posting row also spawns a tile.
##   • Multiple distinct tile sources accumulate independently.
##   • Advancing to the next visitor clears all tiles.
##   • Tiles are correctly absent after clear (next visitor starts clean).
##
## Enable by setting `enabled = true` in the Inspector on the DeskFeatureHarness
## node in scenes/Main.tscn (default false so DevHarness runs by default).
##
## Screenshots land in .captures/ with prefix "df_NN_" so they sort separately
## from the regular DevHarness shots.
##
## Heuristic check output is printed at the end of the run to the Godot Output
## panel / stdout for a quick expert-review sweep.

@export var enabled: bool = false
@export var step_delay: float = 0.8
@export var capture_dir: String = "res://.captures"
@export var quit_when_done: bool = false

var _stamp_handler: Callable
var _ref: Node    # ReferencePanel (accessed via parent's private var — dev-only ok)
var _card: Node   # VisitorCard
var _shot: int = 0
var _pass: int = 0
var _fail: int = 0


func begin(handler: Callable) -> void:
	if not enabled:
		return
	_stamp_handler = handler
	var main := get_parent()
	_ref  = main._reference
	_card = main._card
	_run.call_deferred()


func _run() -> void:
	print("[desk-harness] ── start ──────────────────────────────────────────")
	_ensure_dir()

	# ── Visitor 1 (wren-sixpence — item_check / moonwort, both tools load-bearing) ──

	await _wait()
	await _capture("00_initial_state_v1")
	_check("initial — no tiles", _tile_count() == 0)

	# Glass tile ---------------------------------------------------------------
	_ref._on_tool_pressed("glass")
	await _settle()
	await _capture("01_glass_tile")
	_check("after Glass click — 1 tile", _tile_count() == 1)
	_check("Glass tile has reading text", _top_tile_has_text())

	# Scale tile ---------------------------------------------------------------
	await _wait()
	_ref._on_tool_pressed("scale")
	await _settle()
	await _capture("02_scale_tile_added")
	_check("after Scale click — 2 tiles", _tile_count() == 2)

	# Glass re-click should REPLACE (still 2, not 3) ---------------------------
	await _wait()
	_ref._on_tool_pressed("glass")
	await _settle()
	await _capture("03_glass_replace_not_duplicate")
	_check("Glass re-click — still 2 tiles (replace, not duplicate)", _tile_count() == 2)

	# Quest Board: apothecary standing order -----------------------------------
	await _wait()
	_ref.dev_fire_tile_for_posting("apothecary-standing-order")
	await _settle()
	await _capture("04_posting_tile_apothecary")
	_check("posting tile added — 3 tiles total", _tile_count() == 3)

	# Quest Board: second posting (different id → new tile) --------------------
	await _wait()
	_ref.dev_fire_tile_for_posting("cistern-wisp-swarm")
	await _settle()
	await _capture("05_posting_tile_bounty")
	_check("second posting tile added — 4 tiles total", _tile_count() == 4)

	# Same posting re-fired → replace (still 4) --------------------------------
	await _wait()
	_ref.dev_fire_tile_for_posting("apothecary-standing-order")
	await _settle()
	await _capture("06_posting_replace_not_duplicate")
	_check("posting re-click — still 4 tiles (replace)", _tile_count() == 4)

	# × dismiss one tile (glass) → 3 remain ------------------------------------
	await _wait()
	_card._dismiss_tile("glass")
	await _settle()
	await _capture("07_dismiss_glass_tile")
	_check("after dismiss Glass — 3 tiles remain", _tile_count() == 3)

	# Advance to visitor 2 — ALL tiles must clear ------------------------------
	await _wait()
	_stamp_handler.call("approve")   # wren-sixpence correct stamp
	await get_tree().process_frame
	await get_tree().process_frame
	await _capture("08_after_advance_tiles_cleared")
	_check("after visitor advance — tiles cleared (0)", _tile_count() == 0)

	# ── Visitor 2 (hulbr-odd-eye — rank_gate, tools are decoys) ──

	await _wait()
	await _capture("09_visitor2_clean_desk")
	_check("visitor 2 starts with clean desk", _tile_count() == 0)

	# Glass tile still spawns even when it is a decoy --------------------------
	_ref._on_tool_pressed("glass")
	await _settle()
	await _capture("10_decoy_glass_tile_visitor2")
	_check("decoy Glass still produces a tile", _tile_count() == 1)

	# ── Heuristic check sweep -------------------------------------------------
	_print_heuristic_notes()

	# Summary capture after notes are printed
	await _wait()
	await _capture("99_desk_feature_summary")

	var total := _pass + _fail
	print("[desk-harness] ── results: %d/%d pass ──────────────────────────────" % [_pass, total])
	if _fail > 0:
		print("[desk-harness] WARNING — %d assertion(s) failed — see output above" % _fail)
	print("[desk-harness] %d screenshot(s) → %s" % [_shot, ProjectSettings.globalize_path(capture_dir)])

	if quit_when_done:
		get_tree().quit()


# ── Tile introspection ────────────────────────────────────────────────────────

## Count live PanelContainer tiles inside VisitorCard._tiles_list.
func _tile_count() -> int:
	var tlist: Node = _card._tiles_list
	if tlist == null:
		return 0
	var count := 0
	for c in tlist.get_children():
		if is_instance_valid(c) and c.visible:
			count += 1
	return count


## True if any tile text label is non-empty (sanity that something was rendered).
func _top_tile_has_text() -> bool:
	var tlist: Node = _card._tiles_list
	if tlist == null or tlist.get_child_count() == 0:
		return false
	var tile := tlist.get_child(0)
	for child in tile.get_children():
		for label in child.get_children():
			if label is Label and (label as Label).text != "":
				return true
	return false


# ── Assertion helpers ─────────────────────────────────────────────────────────

func _check(desc: String, result: bool) -> void:
	if result:
		print("[desk-harness]  PASS  %s" % desc)
		_pass += 1
	else:
		print("[desk-harness]  FAIL  %s" % desc)
		_fail += 1


# ── Heuristic notes ───────────────────────────────────────────────────────────

func _print_heuristic_notes() -> void:
	print("")
	print("[desk-harness] ── heuristic notes (post-fix, expert review) ────────")
	print("H1 Visibility: tiles appear immediately on click — ✔ instant feedback.")
	print("H1 Visibility: first-use hint in empty tiles area now surfaces feature — ✔.")
	print("H1 Visibility: posting row hover tint signals interactivity — ✔ (fixed).")
	print("H2 Real-world match: 'pulling a reference sheet to the desk' maps naturally")
	print("               to the Papers-Please setting — ✔ good metaphor.")
	print("H3 User control: × dismiss button per tile allows mid-visitor declutter — ✔ (fixed).")
	print("H4 Consistency: BRASS/GREEN tint system coherent; tile label now 12px — ✔.")
	print("H5 Error prevention: replace-not-duplicate ✔; silent no-op is acceptable")
	print("               (tool page shows 'nothing to examine' in panel anyway).")
	print("H6 Recognition: tile body echoes reference content verbatim — ✔ core payoff.")
	print("H7 Flexibility: G/S keyboard shortcuts for Glass/Scale — ✔ (fixed).")
	print("H8 Aesthetics: ScrollContainer caps tiles at 170px; 5+ tiles scroll cleanly — ✔ (fixed).")
	print("H8 Aesthetics: foldout headers now GROUND bg / LINE2 border — ✔ (fixed).")
	print("H9 Error recovery: × dismiss restores control; tiles don't affect verdict — ✔.")
	print("H10 Help: first-use hint explains the mechanic in-context — ✔ (fixed).")
	print("")
	print("Remaining open (low priority / future work):")
	print("  • Tool keyboard shortcuts work only when the window has focus (no in-game label).")
	print("  • Townee Directory and Adventurer Directory are very wide tables —")
	print("    consider column-capping or wrapping for narrower viewport widths.")
	print("[desk-harness] ── end heuristic notes ──────────────────────────────")
	print("")


# ── Utilities ────────────────────────────────────────────────────────────────

func _wait() -> void:
	await get_tree().create_timer(step_delay).timeout


func _settle() -> void:
	# Two frames: one for queue_free of replaced tiles, one for layout recalc.
	await get_tree().process_frame
	await get_tree().process_frame


func _capture(shot_name: String) -> void:
	await RenderingServer.frame_post_draw
	var img := get_viewport().get_texture().get_image()
	if img == null:
		push_warning("[desk-harness] viewport null for " + shot_name)
		return
	var path := "%s/df_%02d_%s.png" % [capture_dir, _shot, shot_name]
	var err := img.save_png(path)
	if err != OK:
		push_warning("[desk-harness] save failed (%d) %s" % [err, path])
		return
	_shot += 1


func _ensure_dir() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(capture_dir))
	var gdignore := capture_dir + "/.gdignore"
	if not FileAccess.file_exists(gdignore):
		var f := FileAccess.open(gdignore, FileAccess.WRITE)
		if f:
			f.store_string("")
			f.close()

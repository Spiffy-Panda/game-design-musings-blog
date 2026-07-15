extends Control
## Main — the integrator. Assembles the four component scenes into the desk layout and
## wires them to the Session/Deck autoloads. Owns NO game rules; it is pure plumbing, so
## component sub-agents can work behind stable signals without ever editing this file.
##
## Layout:  [ booth column: VisitorCard / VerdictBar / Scoreboard ] | [ ReferencePanel ]

const VisitorCardScene := preload("res://scenes/VisitorCard.tscn")
const ReferencePanelScene := preload("res://scenes/ReferencePanel.tscn")
const VerdictBarScene := preload("res://scenes/VerdictBar.tscn")
const ScoreboardScene := preload("res://scenes/Scoreboard.tscn")

## The desk runs a week: day 0 is the curated tutorial, days 1..LAST_DAY are generated
## (deterministic, seed = day). The Next-Day button walks the queue up to here, then stops.
const LAST_DAY := 7

var _card: Node
var _reference: Node
var _verdict: Node
var _score: Node

# Day-flow chrome (Main-owned — not part of any frozen component interface).
var _day_label: Label
var _skip_btn: Button
var _next_btn: Button

# Floor beat (Main-owned — shown after shift_complete; lets owing townees pay dues).
var _floor_box: VBoxContainer
var _floor_rows: VBoxContainer


func _ready() -> void:
	theme = ThemeFactory.build()  # parchment desk theme (cascades to every child Control)
	_build_layout()
	_wire()

	if not Deck.ok:
		_card.show_visitor({ "name": Loc.t("data_error"), "claim": { "summary": "\n".join(Deck.load_errors) } })
		return

	_reference.set_references(Deck.references)
	_update_day_chrome(false)
	Session.start()

	# Dev-only: hand the validation harness the same handler a stamp-press fires, so it can
	# auto-step the shift and screenshot each state. Untick DevHarness.Enabled to play manually.
	var dev := get_node_or_null("DevHarness")
	if dev:
		dev.begin(_on_stamp_chosen)

	var desk_dev := get_node_or_null("DeskFeatureHarness")
	if desk_dev:
		desk_dev.begin(_on_stamp_chosen)


func _build_layout() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var margin := MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	for side in ["left", "top", "right", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 20)
	add_child(margin)

	var split := HBoxContainer.new()
	split.add_theme_constant_override("separation", 20)
	margin.add_child(split)

	# Booth column (left) — the counter and the commit controls.
	var booth := VBoxContainer.new()
	booth.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	booth.add_theme_constant_override("separation", 16)
	split.add_child(booth)

	booth.add_child(_build_daystrip())

	_card = VisitorCardScene.instantiate()
	_card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_verdict = VerdictBarScene.instantiate()
	_score = ScoreboardScene.instantiate()
	booth.add_child(_card)
	booth.add_child(_verdict)
	booth.add_child(_score)

	# Floor beat — appears after the shift summary, lets owing townees pay dues before
	# the next shift opens. Hidden at the start of every shift; shown at shift end.
	_floor_box = _build_floor_beat()
	_floor_box.visible = false
	booth.add_child(_floor_box)

	# Sits under the day's ledger; only shows once the shift is stamped through.
	_next_btn = _make_desk_button("")
	_next_btn.visible = false
	_next_btn.pressed.connect(_on_next_day)
	booth.add_child(_next_btn)

	# Reference column (right) — the whole lookup surface.
	_reference = ReferencePanelScene.instantiate()
	_reference.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	split.add_child(_reference)


func _wire() -> void:
	Session.visitor_changed.connect(_on_visitor_changed)
	Session.verdict_recorded.connect(_on_verdict_recorded)
	Session.shift_complete.connect(_on_shift_complete)
	_verdict.stamp_chosen.connect(_on_stamp_chosen)
	_reference.tile_requested.connect(_on_tile_requested)


func _on_visitor_changed(v: Dictionary) -> void:
	_card.clear_tiles()
	_card.show_visitor(v)
	_reference.set_inspection_target(v)  # refill the Glass/Scale tool pages for this visitor
	_score.set_progress(Session.index, Deck.count(), Session.score)
	_verdict.set_enabled(true)


func _on_stamp_chosen(stamp: String) -> void:
	_verdict.set_enabled(false)
	Session.submit(stamp)
	Session.advance()


## A tool tab or Quest Board row was clicked in the ReferencePanel — post a reference
## tile to the main desk so the player can compare it against the visitor's claim.
func _on_tile_requested(tile_id: String, title: String, body: String, tint: Color) -> void:
	_card.add_tile(tile_id, title, body, tint)


## G / S keyboard shortcuts for Glass and Scale — speed path for expert play.
## Only active while the desk is running (Deck loaded, shift not complete).
func _unhandled_key_input(event: InputEvent) -> void:
	if not (event is InputEventKey):
		return
	var ke := event as InputEventKey
	if not ke.pressed or ke.echo:
		return
	if not Deck.ok:
		return
	match ke.keycode:
		KEY_G:
			_reference._on_tool_pressed("glass")
		KEY_S:
			_reference._on_tool_pressed("scale")


func _on_verdict_recorded(entry: Dictionary) -> void:
	var mark := "right" if entry.get("right", false) else "WRONG (wanted %s)" % entry.get("correct", "?")
	print("[verdict] %s -> %s : %s" % [entry.get("id", "?"), entry.get("chosen", "?"), mark])


func _on_shift_complete(summary: Dictionary) -> void:
	_verdict.set_enabled(false)
	_score.show_summary(summary)
	_update_day_chrome(true)
	_refresh_floor_beat()
	print("[shift] complete: %d / %d correct" % [summary.get("correct", 0), summary.get("total", 0)])


# ---------------------------------------------------------------- day flow

## The day strip along the top of the booth: which day this is, plus a Skip-tutorial
## shortcut while the curated day 0 is up.
func _build_daystrip() -> Control:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)

	_day_label = Label.new()
	_day_label.add_theme_font_size_override("font_size", 13)
	_day_label.add_theme_color_override("font_color", Palette.BRASS)
	_day_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(_day_label)

	_skip_btn = _make_desk_button(Loc.t("skip_tutorial"))
	_skip_btn.size_flags_horizontal = Control.SIZE_SHRINK_END
	_skip_btn.pressed.connect(func() -> void: _go_to_day(1))
	row.add_child(_skip_btn)
	return row


## Reflect the loaded day into the chrome. `finished` = the shift's ledger is showing,
## so the Next-Day button takes over (up to the end of the week) and Skip retires.
func _update_day_chrome(finished: bool) -> void:
	var day: int = Deck.day
	_day_label.text = Loc.t("day_label_tutorial") if day == 0 else Loc.t("day_label") % day
	_skip_btn.visible = day == 0 and not finished

	if finished and day < LAST_DAY:
		_next_btn.text = Loc.t("next_day") % (day + 1)
		_next_btn.visible = true
	elif finished:
		_next_btn.text = Loc.t("week_done")
		_next_btn.disabled = true
		_next_btn.visible = true
	else:
		_next_btn.visible = false


func _on_next_day() -> void:
	_go_to_day(Deck.day + 1)


## Reload the queue for a day and re-open the desk on its first visitor. The banks
## (rulebook + directories) are unchanged across days, so only the shift is reloaded.
func _go_to_day(d: int) -> void:
	_floor_box.visible = false
	Deck.load_day(d)
	if not Deck.ok:
		_card.show_visitor({ "name": Loc.t("data_error"), "claim": { "summary": "\n".join(Deck.load_errors) } })
		return
	_next_btn.visible = false
	_update_day_chrome(false)
	Session.start()


# ---------------------------------------------------------------- floor beat

## Build the floor beat container (rule + header + dynamic rows area). Initially hidden;
## shown by _refresh_floor_beat after the shift summary.
func _build_floor_beat() -> VBoxContainer:
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 8)

	var rule := ColorRect.new()
	rule.color = Palette.LINE2
	rule.custom_minimum_size = Vector2(0, 1)
	rule.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	box.add_child(rule)

	var head := Label.new()
	head.text = Loc.t("floor_head")
	head.add_theme_font_size_override("font_size", 13)
	head.add_theme_color_override("font_color", Palette.BRASS)
	box.add_child(head)

	_floor_rows = VBoxContainer.new()
	_floor_rows.add_theme_constant_override("separation", 6)
	box.add_child(_floor_rows)

	return box


## Populate the floor beat with owing townees (or a "no dues" note) and show it.
## Safe to call only from _on_shift_complete — not from inside a button handler.
func _refresh_floor_beat() -> void:
	for child in _floor_rows.get_children():
		child.queue_free()

	if not Deck.ok or Deck.townees.is_empty():
		return

	var owing: Array = []
	for tid in Deck.townees.keys():
		if str(Deck.townees[tid].get("dues", "current")) == "owing":
			owing.append(tid)

	if owing.is_empty():
		var lbl := Label.new()
		lbl.text = Loc.t("floor_no_dues")
		lbl.add_theme_font_size_override("font_size", 13)
		lbl.add_theme_color_override("font_color", Palette.INK3)
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_floor_rows.add_child(lbl)
	else:
		var sub := Label.new()
		sub.text = Loc.t("floor_dues_intro")
		sub.add_theme_font_size_override("font_size", 12)
		sub.add_theme_color_override("font_color", Palette.INK3)
		_floor_rows.add_child(sub)
		for tid in owing:
			_floor_rows.add_child(_build_dues_row(tid))

	_floor_box.visible = true


## Build one owing-townee row: name + owed amount + Accept button.
func _build_dues_row(tid: String) -> Control:
	var t: Dictionary = Deck.townees[tid]
	var tname: String = str(t.get("name", Loc.humanize(tid)))
	var owed: int = int(t.get("owed", 0))

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)

	var lbl := Label.new()
	lbl.text = "%s — owes %dg" % [tname, owed]
	lbl.add_theme_font_size_override("font_size", 13)
	lbl.add_theme_color_override("font_color", Palette.INK)
	lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(lbl)

	var btn := _make_desk_button(Loc.t("floor_accept_btn") % owed)
	btn.pressed.connect(func() -> void: _on_dues_accepted(tid, btn, lbl))
	row.add_child(btn)

	return row


## Player accepted a townee's dues payment. Clears the dues at runtime and updates
## the row in place — the change takes effect for the next generated shift.
func _on_dues_accepted(tid: String, btn: Button, lbl: Label) -> void:
	Deck.pay_dues(tid)
	btn.disabled = true
	btn.text = Loc.t("floor_paid")
	lbl.add_theme_color_override("font_color", Palette.INK3)


## A quiet brass-outlined desk button, matching the parchment theme.
func _make_desk_button(text: String) -> Button:
	var b := Button.new()
	b.text = text
	b.focus_mode = Control.FOCUS_NONE
	b.add_theme_font_size_override("font_size", 15)
	for state in ["normal", "hover", "pressed", "disabled"]:
		b.add_theme_stylebox_override(state, _desk_button_style(state))
	b.add_theme_color_override("font_color", Palette.INK)
	b.add_theme_color_override("font_hover_color", Palette.INK)
	b.add_theme_color_override("font_pressed_color", Palette.INK)
	b.add_theme_color_override("font_disabled_color", Palette.INK3)
	return b


func _desk_button_style(state: String) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	var warm := state == "hover" or state == "pressed"
	sb.bg_color = Palette.SURFACE.lerp(Palette.BRASS, 0.16 if warm else 0.07)
	sb.border_color = Color(Palette.BRASS, 0.35 if state == "disabled" else 1.0)
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(5)
	sb.content_margin_left = 14
	sb.content_margin_right = 14
	sb.content_margin_top = 9
	sb.content_margin_bottom = 9
	return sb

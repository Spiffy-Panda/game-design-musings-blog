extends PanelContainer
## VerdictBar — the stamps. The player's only commit action.
##
## The row of desk stamps the teller slams onto a visitor's papers. In the full
## four-stamp desk it shows APPROVE / REJECT / HOLD / CONDITIONAL; when
## `Session.STRICT_BINARY` is on it collapses to the two-stamp feel (APPROVE /
## REJECT only). Each stamp is inked in its verdict color (Palette.verdict_color)
## and answers a press with a quick "thunk" — a scale punch plus an ink bloom —
## before it reports the choice, so committing feels like pressing a real stamp.
##
## Contract (FROZEN — Main.gd wires stamp_chosen to Session.submit):
##   set_enabled(on: bool) -> void      gate all stamps between visitors
## Signal:
##   stamp_chosen(stamp: String)        one of "approve"|"reject"|"hold"|"conditional"
##
## Keyboard shortcuts (nice-to-have): A approve · R reject · H hold · C conditional.

signal stamp_chosen(stamp: String)

const STAMPS_FULL := ["approve", "reject", "hold", "conditional"]
const STAMPS_BINARY := ["approve", "reject"]

## key -> stamp; only fires for stamps currently on the desk.
const SHORTCUTS := {
	KEY_A: "approve",
	KEY_R: "reject",
	KEY_H: "hold",
	KEY_C: "conditional",
}

var _buttons: Dictionary = {}   # stamp:String -> Button
var _enabled: bool = false
var _busy: bool = false          # true while a thunk plays (guards double-commit)


func _ready() -> void:
	# Parchment plinth the stamps sit on, so the row reads as one desk fixture.
	var bg := StyleBoxFlat.new()
	bg.bg_color = Palette.SURFACE
	bg.border_color = Palette.LINE2
	bg.set_border_width_all(1)
	bg.set_corner_radius_all(6)
	bg.content_margin_left = 14
	bg.content_margin_right = 14
	bg.content_margin_top = 12
	bg.content_margin_bottom = 12
	add_theme_stylebox_override("panel", bg)

	var row := HBoxContainer.new()
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	row.add_theme_constant_override("separation", 14)
	add_child(row)

	var stamps: Array = STAMPS_BINARY if Session.STRICT_BINARY else STAMPS_FULL
	for s in stamps:
		var b := _make_stamp(s)
		row.add_child(b)
		_buttons[s] = b

	set_enabled(false)


## Build one stamp button, inked in its verdict color, with a flash overlay child.
func _make_stamp(stamp: String) -> Button:
	var col: Color = Palette.verdict_color(stamp)

	var b := Button.new()
	b.text = Loc.stamp_button(stamp)
	b.focus_mode = Control.FOCUS_NONE
	b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	b.custom_minimum_size = Vector2(148, 56)
	b.clip_contents = true
	b.add_theme_font_size_override("font_size", 20)

	b.add_theme_stylebox_override("normal", _stamp_style(col, false, false))
	b.add_theme_stylebox_override("hover", _stamp_style(col, true, false))
	b.add_theme_stylebox_override("pressed", _stamp_style(col, true, false))
	b.add_theme_stylebox_override("disabled", _stamp_style(col, false, true))
	b.add_theme_color_override("font_color", col)
	b.add_theme_color_override("font_hover_color", col)
	b.add_theme_color_override("font_pressed_color", col)
	b.add_theme_color_override("font_disabled_color", Color(col, 0.35))

	# Ink-bloom overlay: fades in and out on press to sell the stamp contact.
	var flash := ColorRect.new()
	flash.name = "Flash"
	flash.color = Color(col, 0.0)
	flash.mouse_filter = Control.MOUSE_FILTER_IGNORE
	flash.set_anchors_preset(Control.PRESET_FULL_RECT)
	b.add_child(flash)

	b.pressed.connect(_on_stamp.bind(stamp))
	return b


func _stamp_style(col: Color, warm: bool, dim: bool) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	# Faint wash of the verdict color over the card fill; warmer on hover.
	var wash := 0.16 if warm else 0.08
	if dim:
		wash = 0.03
	sb.bg_color = Palette.SURFACE.lerp(col, wash)
	sb.border_color = Color(col, 0.35 if dim else 1.0)
	sb.set_border_width_all(2)
	sb.set_corner_radius_all(5)
	sb.content_margin_left = 10
	sb.content_margin_right = 10
	sb.content_margin_top = 8
	sb.content_margin_bottom = 8
	return sb


func set_enabled(on: bool) -> void:
	_enabled = on
	if on:
		_busy = false
	for stamp in _buttons:
		var b: Button = _buttons[stamp]
		b.disabled = not on
		if not on:
			# Reset any lingering transform from an interrupted thunk.
			b.scale = Vector2.ONE
			var flash := b.get_node_or_null("Flash") as ColorRect
			if flash:
				flash.color = Color(flash.color, 0.0)


func _unhandled_input(event: InputEvent) -> void:
	if not _enabled or _busy:
		return
	if event is InputEventKey and event.pressed and not event.echo:
		var stamp: String = SHORTCUTS.get(event.keycode, "")
		if stamp != "" and _buttons.has(stamp):
			accept_event()
			_on_stamp(stamp)


## The thunk: a stamp is committed exactly once, after the animation reads.
func _on_stamp(stamp: String) -> void:
	if not _enabled or _busy or not _buttons.has(stamp):
		return
	_busy = true
	# Lock the desk immediately so a second key/click can't double-commit; Main
	# re-opens it via set_enabled(true) when the next visitor steps up.
	for s in _buttons:
		(_buttons[s] as Button).disabled = true

	var b: Button = _buttons[stamp]
	b.pivot_offset = b.size / 2.0
	var flash := b.get_node("Flash") as ColorRect
	var col: Color = Palette.verdict_color(stamp)

	var tw := create_tween()
	tw.set_parallel(false)
	# Punch down (ink contact) then overshoot back up.
	tw.tween_property(b, "scale", Vector2(0.86, 0.86), 0.06).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(b, "scale", Vector2.ONE, 0.18).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	# Ink bloom in parallel with the punch, then clearing.
	var bloom := create_tween()
	bloom.tween_property(flash, "color", Color(col, 0.4), 0.06)
	bloom.tween_property(flash, "color", Color(col, 0.0), 0.22)

	tw.finished.connect(func() -> void:
		stamp_chosen.emit(stamp)
	)

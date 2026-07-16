extends PanelContainer
## Scoreboard — shift progress while playing, and the day's-end ledger.
##
## AGENT-OWNED (owner: `score`). Mid-shift it keeps a legible "where am I in the
## queue / how am I doing" line plus a pip strip; at shift end it closes the book
## with a parchment "SHIFT COMPLETE" panel and a one-row-per-visitor ledger tinted
## green (right) / wax-red (wrong) — the pitch's "gossip, not telemetry" beat.
##
## Contract (FROZEN — bodies only, never rename):
##   set_progress(index: int, total: int, score: int) -> void
##   show_summary(summary: Dictionary) -> void   # {total, correct, log:[{id,chosen,correct,right}]}

# --- progress view ---
var _progress_box: VBoxContainer
var _line: Label
var _pips: _PipStrip

# --- summary view ---
var _summary_box: VBoxContainer
var _summary_headline: Label
var _summary_sub: Label
var _ledger: VBoxContainer


func _ready() -> void:
	_build()


# ---------------------------------------------------------------- contract

func set_progress(index: int, total: int, score: int) -> void:
	if _line == null:
		return
	_line.text = Loc.t("progress_line") % [index + 1, total, score]
	_pips.set_state(index, total)
	# In case the panel is being reused for a fresh shift after a summary.
	_progress_box.visible = true
	_summary_box.visible = false


func show_summary(summary: Dictionary) -> void:
	if _ledger == null:
		return
	var total := int(summary.get("total", 0))
	var correct := int(summary.get("correct", 0))
	var entries: Array = summary.get("log", [])

	_summary_headline.text = "%d / %d" % [correct, total]
	var missed := total - correct
	if missed <= 0:
		_summary_sub.text = Loc.t("summary_sub_clean")
	elif missed == 1:
		_summary_sub.text = Loc.t("summary_sub_one")
	else:
		_summary_sub.text = Loc.t("summary_sub_many") % missed

	for child in _ledger.get_children():
		child.queue_free()
	for entry in entries:
		_ledger.add_child(_build_row(entry))

	_progress_box.visible = false
	_summary_box.visible = true


# ---------------------------------------------------------------- build

func _build() -> void:
	add_theme_stylebox_override("panel", _panel_style())

	var pad := MarginContainer.new()
	pad.add_theme_constant_override("margin_left", 16)
	pad.add_theme_constant_override("margin_right", 16)
	pad.add_theme_constant_override("margin_top", 14)
	pad.add_theme_constant_override("margin_bottom", 14)
	add_child(pad)

	var stack := VBoxContainer.new()
	stack.add_theme_constant_override("separation", 10)
	stack.size_flags_vertical = Control.SIZE_EXPAND_FILL
	pad.add_child(stack)

	_progress_box = _build_progress()
	stack.add_child(_progress_box)

	_summary_box = _build_summary()
	_summary_box.visible = false
	stack.add_child(_summary_box)


func _build_progress() -> VBoxContainer:
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 8)

	_line = _mk_label("—", 15, Palette.INK)
	_line.set_meta("test_id", "progress")  # GTH: "Visitor N / M · correct: K"
	box.add_child(_line)

	_pips = _PipStrip.new()
	box.add_child(_pips)
	return box


func _build_summary() -> VBoxContainer:
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 4)
	box.size_flags_vertical = Control.SIZE_EXPAND_FILL

	var kicker := _mk_label(Loc.t("shift_complete"), 12, Palette.BRASS)
	box.add_child(kicker)

	_summary_headline = _mk_label("", 30, Palette.INK)
	_summary_headline.set_meta("test_id", "summary-score")  # GTH: the "N / N" the shift is graded on
	box.add_child(_summary_headline)

	_summary_sub = _mk_label("", 12, Palette.INK3)
	box.add_child(_summary_sub)

	var rule := ColorRect.new()
	rule.color = Palette.LINE2
	rule.custom_minimum_size = Vector2(0, 1)
	rule.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var rule_wrap := MarginContainer.new()
	rule_wrap.add_theme_constant_override("margin_top", 6)
	rule_wrap.add_theme_constant_override("margin_bottom", 6)
	rule_wrap.add_child(rule)
	box.add_child(rule_wrap)

	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.custom_minimum_size = Vector2(0, 240)
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	box.add_child(scroll)

	_ledger = VBoxContainer.new()
	_ledger.add_theme_constant_override("separation", 3)
	_ledger.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_ledger)
	return box


func _build_row(entry: Dictionary) -> Control:
	var right := bool(entry.get("right", false))
	var chosen := str(entry.get("chosen", ""))
	var correct := str(entry.get("correct", ""))
	var id := str(entry.get("id", "?"))
	var tint: Color = Palette.GREEN if right else Palette.RED

	var row := PanelContainer.new()
	row.add_theme_stylebox_override("panel", _row_style(tint))

	var inner := MarginContainer.new()
	inner.add_theme_constant_override("margin_left", 11)
	inner.add_theme_constant_override("margin_right", 10)
	inner.add_theme_constant_override("margin_top", 5)
	inner.add_theme_constant_override("margin_bottom", 5)
	row.add_child(inner)

	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 8)
	inner.add_child(h)

	# Prefer the authored name carried in the log entry; fall back to humanizing the id.
	var who := str(entry.get("name", ""))
	if who == "":
		who = Loc.humanize(id)
	var name_lbl := _mk_label(who, 13, Palette.INK)
	name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_lbl.clip_text = true
	h.add_child(name_lbl)

	if right:
		h.add_child(_mk_label(Loc.stamp_past(chosen), 13, Palette.GREEN))
		h.add_child(_mk_label("✓", 14, Palette.GREEN))
	else:
		h.add_child(_mk_label(Loc.stamp_past(chosen), 13, Palette.RED))
		h.add_child(_mk_label("→", 12, Palette.INK3))
		h.add_child(_mk_label(Loc.stamp_past(correct), 13, Palette.GREEN))
	return row


# ---------------------------------------------------------------- helpers

func _mk_label(text: String, font_size: int, color: Color) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", font_size)
	l.add_theme_color_override("font_color", color)
	return l


func _panel_style() -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = Palette.SURFACE
	sb.border_color = Palette.LINE
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(6)
	sb.set_content_margin_all(0)
	return sb


func _row_style(tint: Color) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(tint.r, tint.g, tint.b, 0.09)
	sb.border_color = tint
	sb.border_width_left = 3
	sb.set_corner_radius_all(3)
	sb.set_content_margin_all(0)
	return sb


# --- pip strip: position through the queue --------------------------------

class _PipStrip extends Control:
	var total: int = 0
	var index: int = 0

	func _init() -> void:
		custom_minimum_size = Vector2(120, 16)
		size_flags_horizontal = Control.SIZE_EXPAND_FILL

	func set_state(i: int, n: int) -> void:
		index = i
		total = n
		queue_redraw()

	func _draw() -> void:
		if total <= 0:
			return
		var w := size.x
		var y := size.y * 0.5
		var gap := w / float(total)
		var r := minf(4.0, gap * 0.34)
		for i in total:
			var cx := gap * (i + 0.5)
			var p := Vector2(cx, y)
			if i < index:
				draw_circle(p, r, Palette.INK3)          # verdict rendered
			elif i == index:
				draw_circle(p, r + 1.5, Palette.BRASS)    # at the counter now
			else:
				draw_arc(p, r, 0.0, TAU, 18, Palette.LINE2, 1.5, true)  # still to come

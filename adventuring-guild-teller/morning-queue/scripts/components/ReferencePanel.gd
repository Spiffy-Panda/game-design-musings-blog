extends PanelContainer
## ReferencePanel — the open reference book, ledger, postings, ciphers, drop table,
## season wheel and roster. The player's whole verification surface lives here: a
## left tab-list of the rulebook's tables and a scrolling, ledger-legible content pane.
## Fast, scannable lookup is the point, so it favours tabular alignment + Palette
## hairlines over decoration.
##
## AGENT-OWNED (owner: `reference`). Root stays Control-derived (PanelContainer) with
## this script attached. DO NOT change the public contract.
##
## Contract:
##   set_references(refs: Dictionary) -> void        load the rulebook (call once)
##   focus(consult: String, entry: String) -> void   jump to the tab/entry a check names
##                                                    (consult values per visitors.json)
##   set_inspection_target(visitor: Dictionary) -> void   ADDITIVE (INSPECTION-TOOLS.md
##       §6): refill the Glass/Scale tool pages with THIS visitor's `inspections`
##       readings. Does not touch set_references()/focus(); does not change the selected
##       tab (a player mid-read of a reference tab is not yanked away).
##
## Signal (ADDITIVE):
##   tile_requested(tile_id, title, body, tint)  emitted when the player clicks a tool
##       tab or a Quest Board row; Main wires this to VisitorCard.add_tile so the content
##       appears as a placed reference tile on the main desk.

## Emitted when the player clicks a tool tab (tool_id as tile_id, BRASS tint) or a
## Quest Board posting row (posting id as tile_id, GREEN tint). Main.gd wires this to
## VisitorCard.add_tile so the content lands on the desk.
signal tile_requested(tile_id: String, title: String, body: String, tint: Color)

## consult (from a visitor `check`) -> top-level references.json key (the tab).
const CONSULT_TO_TAB := {
	"book": "book",
	"posting": "postings",
	"ledger": "rank_ledger",
	"cipher": "cipher_table",
	"drop_table": "drop_table",
	"season": "season",
	"roster": "roster",
	"archive": "archive",
	"townee_directory": "townee_directory",
	"adventurer_directory": "adventurer_directory",
}

## The inspection tools that ship, in display order (INSPECTION-TOOLS.md §1). These are
## visitor-scoped tabs living in their own brass-accented group above the reference tabs.
const TOOLS := ["glass", "scale"]

var _refs: Dictionary = {}
var _buttons: Dictionary = {}          # tab key -> Button
var _pages: Dictionary = {}            # tab key -> page Control (child of _pages_holder)
var _rows: Dictionary = {}             # tab key -> { entry id -> row PanelContainer }
var _tab_group := ButtonGroup.new()
var _active_tab := ""
var _highlighted: PanelContainer = null

var _tab_list: VBoxContainer
var _scroll: ScrollContainer
var _pages_holder: VBoxContainer
var _empty_hint: Label

# --- inspection tools (INSPECTION-TOOLS.md §6) — visitor-scoped, additive ------
var _tool_buttons: Dictionary = {}     # tool id -> Button
var _tool_pages: Dictionary = {}       # tool id -> page Control (child of _pages_holder)
var _inspections: Dictionary = {}      # current visitor's `inspections` block
var _claim_against: String = ""        # current visitor's claimed standing-order id (or "")


func _ready() -> void:
	add_theme_stylebox_override("panel", _panel_style(Palette.SURFACE, Palette.LINE2))

	var root := VBoxContainer.new()
	root.add_theme_constant_override("separation", 10)
	add_child(root)

	var head := Label.new()
	head.text = Loc.t("reference_head")
	head.add_theme_color_override("font_color", Palette.INK3)
	head.add_theme_font_size_override("font_size", 13)
	root.add_child(head)

	var body := HBoxContainer.new()
	body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_theme_constant_override("separation", 12)
	root.add_child(body)

	# Left: the tab list (one row per table).
	var tab_frame := PanelContainer.new()
	tab_frame.add_theme_stylebox_override("panel", _panel_style(Palette.GROUND, Palette.LINE))
	tab_frame.custom_minimum_size = Vector2(158, 0)
	tab_frame.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_child(tab_frame)

	_tab_list = VBoxContainer.new()
	_tab_list.add_theme_constant_override("separation", 2)
	tab_frame.add_child(_tab_list)

	# Right: the scrolling content pane.
	_scroll = ScrollContainer.new()
	_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_child(_scroll)

	_pages_holder = VBoxContainer.new()
	_pages_holder.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.add_child(_pages_holder)

	_empty_hint = Label.new()
	_empty_hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_empty_hint.text = Loc.t("reference_empty")
	_empty_hint.add_theme_color_override("font_color", Palette.INK3)
	_pages_holder.add_child(_empty_hint)


# --- Contract ----------------------------------------------------------------

func set_references(refs: Dictionary) -> void:
	_refs = refs
	_build()


func focus(consult: String, entry: String) -> void:
	if _refs.is_empty():
		return
	var tab: String = CONSULT_TO_TAB.get(consult, consult)
	if not _pages.has(tab):
		return
	_select_tab(tab)
	var rows: Dictionary = _rows.get(tab, {})
	if rows.has(entry):
		_highlight_row(rows[entry])
	else:
		_clear_highlight()


## ADDITIVE (INSPECTION-TOOLS.md §6). Refill the Glass/Scale tool pages with THIS
## visitor's readings. Never changes the selected tab, so a player mid-read of a
## reference tab is not yanked away; a missing/empty `inspections` (e.g. the data-error
## sparse dict) falls back to `tool_empty` rather than crashing. Never surfaces the
## authored `relevant` flag.
func set_inspection_target(visitor: Dictionary) -> void:
	var insp: Variant = visitor.get("inspections", {})
	_inspections = insp if insp is Dictionary else {}
	# The claimed standing-order id (item_check visitors) — used ONLY to compute the
	# Scale's amount-vs-limit comparison (§4), never to reveal which check matters.
	var claim: Variant = visitor.get("claim", {})
	var asserts: Variant = claim.get("asserts", {}) if claim is Dictionary else {}
	_claim_against = str(asserts.get("against", "")) if asserts is Dictionary else ""
	_refill_tools()


# --- Build -------------------------------------------------------------------

func _build() -> void:
	for c in _tab_list.get_children():
		c.queue_free()
	for c in _pages_holder.get_children():
		c.queue_free()
	_buttons.clear()
	_pages.clear()
	_rows.clear()
	_tool_buttons.clear()
	_tool_pages.clear()
	_highlighted = null
	_active_tab = ""

	# INSPECTION TOOLS group first — visitor-scoped, brass-accented, its own header.
	_build_tools_group()

	# REFERENCE group — the fixed rulebook tables (unchanged).
	for key in _refs.keys():
		if key.begins_with("_"):
			continue
		var table: Variant = _refs[key]
		_add_tab_button(key, Loc.ref_tab(key))
		var page := _build_page(key, table)
		page.visible = false
		_pages_holder.add_child(page)
		_pages[key] = page

	# Open the first REFERENCE tab by default so the desk is never blank (and never
	# auto-jumps to a tool — tools are opened by the player).
	for key in _pages.keys():
		_select_tab(key)
		break

	# Fill the tool pages from whatever visitor is current (may be empty pre-start).
	_refill_tools()


## The brass-tinted INSPECTION TOOLS group: an eyebrow header, the two tool tabs, and a
## divider before the reference tabs. Reads as a different KIND than the reference tabs.
func _build_tools_group() -> void:
	var eyebrow := Label.new()
	eyebrow.text = Loc.t("tools_head")
	eyebrow.add_theme_color_override("font_color", Palette.BRASS)
	eyebrow.add_theme_font_size_override("font_size", 11)
	_tab_list.add_child(eyebrow)

	for tool_id in TOOLS:
		_add_tool_button(tool_id)
		var page := VBoxContainer.new()
		page.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		page.add_theme_constant_override("separation", 6)
		page.visible = false
		_pages_holder.add_child(page)
		_tool_pages[tool_id] = page

	# Divider between the tools group and the reference group.
	var gap := MarginContainer.new()
	gap.add_theme_constant_override("margin_top", 8)
	gap.add_theme_constant_override("margin_bottom", 5)
	gap.add_child(_hairline())
	_tab_list.add_child(gap)


func _add_tab_button(key: String, title: String) -> void:
	var btn := Button.new()
	btn.text = title
	btn.toggle_mode = true
	btn.button_group = _tab_group
	btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
	btn.focus_mode = Control.FOCUS_NONE
	btn.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	btn.add_theme_color_override("font_color", Palette.INK2)
	btn.add_theme_color_override("font_hover_color", Palette.INK)
	btn.add_theme_color_override("font_pressed_color", Palette.GREEN)
	btn.add_theme_color_override("font_hover_pressed_color", Palette.GREEN)
	btn.add_theme_color_override("font_focus_color", Palette.GREEN)
	btn.add_theme_font_size_override("font_size", 14)
	btn.add_theme_stylebox_override("normal", _tab_btn_style(false))
	btn.add_theme_stylebox_override("hover", _tab_btn_style(false, true))
	btn.add_theme_stylebox_override("focus", _tab_btn_style(false))
	btn.add_theme_stylebox_override("pressed", _tab_btn_style(true))
	btn.add_theme_stylebox_override("hover_pressed", _tab_btn_style(true))
	btn.pressed.connect(_select_tab.bind(key))
	_tab_list.add_child(btn)
	_buttons[key] = btn


## A tool tab (The Glass / The Scale). Shares the reference tabs' ButtonGroup so exactly
## one tab is ever selected, but is brass-accented so the two groups read as different.
func _add_tool_button(tool_id: String) -> void:
	var btn := Button.new()
	btn.text = Loc.tool_tab(tool_id)
	btn.toggle_mode = true
	btn.button_group = _tab_group
	btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
	btn.focus_mode = Control.FOCUS_NONE
	btn.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	btn.add_theme_color_override("font_color", Palette.INK2)
	btn.add_theme_color_override("font_hover_color", Palette.INK)
	btn.add_theme_color_override("font_pressed_color", Palette.BRASS)
	btn.add_theme_color_override("font_hover_pressed_color", Palette.BRASS)
	btn.add_theme_color_override("font_focus_color", Palette.BRASS)
	btn.add_theme_font_size_override("font_size", 14)
	btn.add_theme_stylebox_override("normal", _tool_btn_style(false))
	btn.add_theme_stylebox_override("hover", _tool_btn_style(false, true))
	btn.add_theme_stylebox_override("focus", _tool_btn_style(false))
	btn.add_theme_stylebox_override("pressed", _tool_btn_style(true))
	btn.add_theme_stylebox_override("hover_pressed", _tool_btn_style(true))
	btn.pressed.connect(_on_tool_pressed.bind(tool_id))
	_tab_list.add_child(btn)
	_tool_buttons[tool_id] = btn


func _select_tab(key: String) -> void:
	var is_tool: bool = _tool_pages.has(key)
	if not is_tool and not _pages.has(key):
		return
	var btn: Button = _tool_buttons[key] if is_tool else _buttons[key]
	if _active_tab == key and btn.button_pressed:
		return
	_active_tab = key
	# Exactly one page is visible across BOTH groups (tools + reference).
	for k in _pages.keys():
		_pages[k].visible = (k == key)
	for k in _tool_pages.keys():
		_tool_pages[k].visible = (k == key)
	# Keep the button state in sync when selected programmatically (no signal fires then).
	if not btn.button_pressed:
		btn.button_pressed = true
	_clear_highlight()
	_scroll.scroll_vertical = 0


# --- Pages -------------------------------------------------------------------

func _build_page(key: String, table: Variant) -> Control:
	var page := VBoxContainer.new()
	page.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	page.add_theme_constant_override("separation", 6)

	var inner := MarginContainer.new()
	inner.add_theme_constant_override("margin_left", 4)
	inner.add_theme_constant_override("margin_right", 8)
	page.add_child(inner)

	var col := VBoxContainer.new()
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	col.add_theme_constant_override("separation", 2)
	inner.add_child(col)

	# The table's own _tab string as a caption above the rows.
	if table is Dictionary and table.has("_tab"):
		var cap := Label.new()
		cap.text = str(table["_tab"])
		cap.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		cap.add_theme_color_override("font_color", Palette.INK3)
		cap.add_theme_font_size_override("font_size", 12)
		col.add_child(cap)
		col.add_child(_hairline())

	var rows := {}
	_rows[key] = rows

	match key:
		"postings":
			_render_postings(col, table, rows)
		"roster":
			_render_roster(col, table, rows)
		"season":
			_render_season(col, table)
		"payout":
			_render_payout(col, table)
		"rankup_thresholds":
			_render_thresholds(col, table, rows)
		_:
			# Most tables are entry-maps; a few reference keys (e.g. rank_order) are
			# a flat Array or scalar — render those as a single legible row.
			if table is Dictionary:
				_render_entry_map(col, table, rows)
			else:
				_render_list(col, key, table, rows)

	return page


## A non-map reference table (Array or scalar) rendered as one row. No title — the
## tab already names it (e.g. "Rank Ladder"); the value line carries the contents.
func _render_list(col: VBoxContainer, key: String, table: Variant, rows: Dictionary) -> void:
	var row := _make_row("", [["", _fmt(table), false]])
	col.add_child(row)
	rows[key] = row


## Generic: a table that is a map of entry-id -> record (book, postings, ledger,
## archive, cipher_table, drop_table). Each entry becomes a hairline-separated row.
func _render_entry_map(col: VBoxContainer, table: Dictionary, rows: Dictionary) -> void:
	for entry in table.keys():
		if entry == "_tab" or entry.begins_with("_"):
			continue
		var value: Variant = table[entry]
		var lines: Array = []
		if value == null:
			lines.append(["", Loc.t("no_card"), true])
		elif value is Dictionary:
			for f in value.keys():
				if f == "_tab" or (f is String and f.begins_with("_")):
					continue
				lines.append([Loc.humanize(str(f)), _fmt(value[f]), false])
		else:
			lines.append(["", _fmt(value), false])
		# `entry` stays the raw id in `rows` (focus() resolves against check.entry);
		# only the displayed title is humanized.
		var row := _make_row(Loc.humanize(entry), lines)
		col.add_child(row)
		rows[entry] = row


func _render_thresholds(col: VBoxContainer, table: Dictionary, rows: Dictionary) -> void:
	for entry in table.keys():
		if entry == "_tab":
			continue
		var line := Loc.t("threshold_line") % _fmt(table[entry])
		var row := _make_row(Loc.humanize(entry), [["", line, false]])
		col.add_child(row)
		rows[entry] = row


func _render_season(col: VBoxContainer, table: Dictionary) -> void:
	var current := str(table.get("current", "?"))
	var wheel: Array = table.get("wheel", [])
	var head := _make_row(Loc.t("current_season"), [["", current, false]])
	col.add_child(head)
	var wrow := HBoxContainer.new()
	wrow.add_theme_constant_override("separation", 6)
	var pad := MarginContainer.new()
	pad.add_theme_constant_override("margin_top", 4)
	pad.add_theme_constant_override("margin_left", 8)
	pad.add_child(wrow)
	col.add_child(pad)
	for s in wheel:
		var chip := Label.new()
		chip.text = str(s)
		var on := (str(s) == current)
		chip.add_theme_color_override("font_color", Palette.GREEN if on else Palette.INK3)
		chip.add_theme_font_size_override("font_size", 14 if on else 13)
		wrow.add_child(chip)
		if s != wheel[wheel.size() - 1]:
			var arrow := Label.new()
			arrow.text = "→"
			arrow.add_theme_color_override("font_color", Palette.LINE2)
			wrow.add_child(arrow)


func _render_payout(col: VBoxContainer, table: Dictionary) -> void:
	for f in table.keys():
		if f == "_tab":
			continue
		var kv := _kv_line(Loc.humanize(str(f)), _fmt(table[f]))
		col.add_child(kv)


func _render_roster(col: VBoxContainer, table: Dictionary, rows: Dictionary) -> void:
	var parties: Array = table.get("parties", [])
	for p in parties:
		if not (p is Dictionary):
			continue
		var pid := str(p.get("id", "party"))
		var lines: Array = []
		for f in ["lead", "rank", "reach_floor", "wards", "status", "location"]:
			if p.has(f):
				lines.append([Loc.humanize(f), _fmt(p[f]), false])
		# `pid` stays raw in `rows`; only the displayed title is humanized.
		var row := _make_row(Loc.humanize(pid), lines)
		col.add_child(row)
		rows[pid] = row
	if table.has("_note"):
		var note := Label.new()
		note.text = str(table["_note"])
		note.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		note.add_theme_color_override("font_color", Palette.INK3)
		note.add_theme_font_size_override("font_size", 12)
		var pad := MarginContainer.new()
		pad.add_theme_constant_override("margin_top", 8)
		pad.add_child(note)
		col.add_child(pad)


## Quest Board — postings grouped by type in collapsible foldout sections. Rows are
## clickable: clicking a posting emits tile_requested so Main posts a copy to the desk.
## Type render order: bounty first (the main quests), then survey/retrieval/collection/
## rescue, then standing_order; any unrecognised types append at the end.
const _POSTING_TYPE_ORDER := ["bounty", "survey", "retrieval", "collection", "rescue", "standing_order"]

func _render_postings(col: VBoxContainer, table: Dictionary, rows: Dictionary) -> void:
	# Group posting ids by their `type` field.
	var by_type: Dictionary = {}
	for entry in table.keys():
		if entry == "_tab" or entry.begins_with("_"):
			continue
		var value: Variant = table[entry]
		if not (value is Dictionary):
			continue
		var ptype := str((value as Dictionary).get("type", "bounty"))
		if not by_type.has(ptype):
			by_type[ptype] = []
		(by_type[ptype] as Array).append(entry)

	# Build render order: canonical types first, then any extras.
	var type_order: Array = []
	for t in _POSTING_TYPE_ORDER:
		if by_type.has(t):
			type_order.append(t)
	for t in by_type.keys():
		if not type_order.has(t):
			type_order.append(t)

	for ptype in type_order:
		var entries: Array = by_type[ptype]
		var foldout := _make_foldout_section(Loc.humanize(ptype))
		col.add_child(foldout["container"])
		for entry in entries:
			var value: Dictionary = table[entry]
			var lines: Array = []
			for f in value.keys():
				if f == "_tab" or (f is String and f.begins_with("_")) or f == "type":
					continue
				lines.append([Loc.humanize(str(f)), _fmt(value[f]), false])
			var row := _make_row(Loc.humanize(entry), lines)
			# Make each posting row clickable — emit a desk tile on click.
			row.mouse_filter = Control.MOUSE_FILTER_STOP
			row.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
			# Hover tint: subtle green wash signals interactivity before click.
			var normal_style: StyleBox = row.get_meta("normal_style")
			row.mouse_entered.connect(func() -> void:
				row.add_theme_stylebox_override("panel", _posting_row_hover_style())
			)
			row.mouse_exited.connect(func() -> void:
				row.add_theme_stylebox_override("panel", normal_style)
			)
			var captured_id: String = entry
			var captured_title: String = Loc.humanize(entry)
			var body_parts: PackedStringArray = PackedStringArray()
			for ln in lines:
				var lbl: String = str(ln[0])
				var val: String = str(ln[1])
				if lbl != "":
					body_parts.append("%s: %s" % [lbl, val])
				else:
					body_parts.append(val)
			var captured_body: String = "\n".join(body_parts)
			row.gui_input.connect(
				func(ev: InputEvent) -> void:
					if ev is InputEventMouseButton and (ev as InputEventMouseButton).pressed \
					   and (ev as InputEventMouseButton).button_index == MOUSE_BUTTON_LEFT:
						tile_requested.emit(captured_id, captured_title, captured_body, Palette.GREEN)
			)
			(foldout["body"] as VBoxContainer).add_child(row)
			rows[entry] = row


## A collapsible section header + body. Returns { "container": VBoxContainer,
## "body": VBoxContainer }. Starts expanded (▼). Clicking the header toggles the body.
func _make_foldout_section(label: String) -> Dictionary:
	var container := VBoxContainer.new()
	container.add_theme_constant_override("separation", 0)

	var header := Button.new()
	header.text = "▼  " + label
	header.toggle_mode = true
	header.button_pressed = true
	header.alignment = HORIZONTAL_ALIGNMENT_LEFT
	header.focus_mode = Control.FOCUS_NONE
	header.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	header.add_theme_color_override("font_color", Palette.INK2)
	header.add_theme_color_override("font_hover_color", Palette.INK)
	header.add_theme_color_override("font_pressed_color", Palette.INK)
	header.add_theme_color_override("font_hover_pressed_color", Palette.INK)
	header.add_theme_font_size_override("font_size", 13)
	var sb_h := StyleBoxFlat.new()
	sb_h.bg_color = Palette.GROUND
	sb_h.border_width_bottom = 1
	sb_h.border_color = Palette.LINE2
	sb_h.content_margin_left = 8
	sb_h.content_margin_right = 6
	sb_h.content_margin_top = 5
	sb_h.content_margin_bottom = 5
	for state in ["normal", "hover", "pressed", "focus", "hover_pressed", "disabled"]:
		header.add_theme_stylebox_override(state, sb_h)
	container.add_child(header)

	var body_margin := MarginContainer.new()
	body_margin.add_theme_constant_override("margin_left", 8)
	var body := VBoxContainer.new()
	body.add_theme_constant_override("separation", 1)
	body_margin.add_child(body)
	container.add_child(body_margin)

	header.toggled.connect(func(open: bool) -> void:
		body_margin.visible = open
		header.text = ("▼  " if open else "▶  ") + label
	)

	return { "container": container, "body": body }


# --- Inspection tools --------------------------------------------------------

## Pressed handler for a tool tab button. Selects the tab (existing behaviour) and
## also emits tile_requested so Main can post the reading to the main desk.
func _on_tool_pressed(tool_id: String) -> void:
	_select_tab(tool_id)
	var reading := _tool_reading(tool_id)
	if reading == "":
		return
	var title := Loc.tool_tab(tool_id)
	var body := reading
	if tool_id == "scale":
		var cmp := _scale_comparison(_tool_block("scale"))
		if cmp.get("key", "") != "":
			body = body + "\n" + Loc.t(cmp["key"])
	tile_requested.emit(tool_id, title, body, Palette.BRASS)


## DEV/TEST — fires tile_requested for the named posting id exactly as a player click
## would. Used by DeskFeatureHarness to exercise the tile plumbing without needing
## simulated mouse input. No-op if refs have not been loaded yet.
func dev_fire_tile_for_posting(posting_id: String) -> void:
	var postings: Variant = _refs.get("postings", {})
	if not (postings is Dictionary):
		return
	var value: Variant = (postings as Dictionary).get(posting_id, {})
	if not (value is Dictionary):
		return
	var parts: PackedStringArray = PackedStringArray()
	for f in (value as Dictionary).keys():
		if f == "_tab" or (f is String and f.begins_with("_")) or f == "type":
			continue
		parts.append("%s: %s" % [Loc.humanize(str(f)), _fmt(value[f])])
	tile_requested.emit(posting_id, Loc.humanize(posting_id), "\n".join(parts), Palette.GREEN)


## Refill both tool pages from the current visitor's `inspections`. No-op until the
## tools group exists (set_references builds it before the first visitor arrives).
func _refill_tools() -> void:
	if _tool_pages.is_empty():
		return
	_rebuild_tool_page(_tool_pages["glass"], "tool_glass_caption", _tool_reading("glass"), [])
	var scale_extra: Array = []
	var scale_block: Dictionary = _tool_block("scale")
	if _tool_reading("scale") != "":
		var cmp := _scale_comparison(scale_block)
		if cmp.get("key", "") != "":
			scale_extra.append({ "text": Loc.t(cmp["key"]), "color": cmp["color"] })
	_rebuild_tool_page(_tool_pages["scale"], "tool_scale_caption", _tool_reading("scale"), scale_extra)


## This visitor's block for one tool ({} when absent — the empty-guard path).
func _tool_block(tool_id: String) -> Dictionary:
	var b: Variant = _inspections.get(tool_id, {})
	return b if b is Dictionary else {}


## The tool's authored reading string, trimmed ("" triggers the tool_empty fallback).
func _tool_reading(tool_id: String) -> String:
	return str(_tool_block(tool_id).get("reading", "")).strip_edges()


## The claimed standing order ({} unless the visitor claims an order that carries a
## limit). Only `accept`/`total` postings qualify — a plain posting has neither key.
func _standing_order() -> Dictionary:
	if _claim_against == "":
		return {}
	var postings: Variant = _refs.get("postings", {})
	if not (postings is Dictionary):
		return {}
	var order: Variant = postings.get(_claim_against, {})
	if not (order is Dictionary):
		return {}
	if order.has("accept") or order.has("total"):
		return order
	return {}


## Scale amount vs the claimed order's limit (§4). Returns { key, color } where key is a
## Loc `amount_*` chrome key ("" = render no line). Derived purely from amount vs limit —
## it NEVER consults `relevant`. A real weight with no order to measure against reads as
## `amount_no_order` (the "weight when it is not needed" decoy texture, §3).
func _scale_comparison(scale_block: Dictionary) -> Dictionary:
	var amount: Variant = scale_block.get("amount", null)
	# Nothing to weigh -> no line (the reading itself already says so).
	if not (amount is int or amount is float):
		return { "key": "", "color": Palette.INK3 }
	var unit := str(scale_block.get("unit", ""))
	var order := _standing_order()
	if order.is_empty():
		return { "key": "amount_no_order", "color": Palette.INK3 }
	var a := float(amount)
	if order.has("accept"):
		var acc: Dictionary = order["accept"]
		if str(acc.get("unit", "")) != unit:
			return { "key": "amount_no_order", "color": Palette.INK3 }
		if a < float(acc.get("min", 0)):
			return { "key": "amount_under", "color": Palette.RED }
		if a > float(acc.get("max", 0)):
			return { "key": "amount_over", "color": Palette.RED }
		return { "key": "amount_within", "color": Palette.GREEN }
	if order.has("total"):
		var tot: Dictionary = order["total"]
		if str(tot.get("unit", "")) != unit:
			return { "key": "amount_no_order", "color": Palette.INK3 }
		if a >= float(tot.get("needed", 0)):
			return { "key": "amount_meets", "color": Palette.GREEN }
		return { "key": "amount_under", "color": Palette.RED }
	return { "key": "amount_no_order", "color": Palette.INK3 }


## (Re)build one tool page: a brass caption (the tool's job), the visitor's reading (or
## the tool_empty fallback), and any comparison lines. Clears the page first, so a
## visitor switch refills content in place without disturbing which tab is selected.
func _rebuild_tool_page(page: VBoxContainer, caption_key: String, reading: String, extra_lines: Array) -> void:
	for c in page.get_children():
		c.queue_free()

	var inner := MarginContainer.new()
	inner.add_theme_constant_override("margin_left", 4)
	inner.add_theme_constant_override("margin_right", 8)
	page.add_child(inner)

	var col := VBoxContainer.new()
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	col.add_theme_constant_override("separation", 4)
	inner.add_child(col)

	var cap := Label.new()
	cap.text = Loc.t(caption_key)
	cap.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	cap.add_theme_color_override("font_color", Palette.BRASS)
	cap.add_theme_font_size_override("font_size", 12)
	col.add_child(cap)
	col.add_child(_hairline())

	var read_lbl := Label.new()
	read_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	read_lbl.add_theme_font_size_override("font_size", 15)
	if reading == "":
		read_lbl.text = Loc.t("tool_empty")
		read_lbl.add_theme_color_override("font_color", Palette.INK3)
	else:
		read_lbl.text = reading
		read_lbl.add_theme_color_override("font_color", Palette.INK)
	var read_pad := MarginContainer.new()
	read_pad.add_theme_constant_override("margin_top", 4)
	read_pad.add_child(read_lbl)
	col.add_child(read_pad)

	for line in extra_lines:
		var extra := Label.new()
		extra.text = str(line.get("text", ""))
		extra.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		extra.add_theme_color_override("font_color", line.get("color", Palette.INK2))
		extra.add_theme_font_size_override("font_size", 13)
		var ep := MarginContainer.new()
		ep.add_theme_constant_override("margin_top", 2)
		ep.add_child(extra)
		col.add_child(ep)


# --- Row widgets -------------------------------------------------------------

## A single reference row: a heading (the entry id) plus aligned field lines.
## `lines` is an Array of [label, value, is_absent] triples.
func _make_row(title: String, lines: Array) -> PanelContainer:
	var row := PanelContainer.new()
	var normal := _row_style(false)
	row.add_theme_stylebox_override("panel", normal)
	row.set_meta("normal_style", normal)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 1)
	row.add_child(box)

	if title != "":
		var title_lbl := Label.new()
		title_lbl.text = title
		title_lbl.add_theme_color_override("font_color", Palette.INK)
		title_lbl.add_theme_font_size_override("font_size", 15)
		box.add_child(title_lbl)

	if not lines.is_empty():
		var grid := GridContainer.new()
		grid.columns = 2
		grid.add_theme_constant_override("h_separation", 10)
		grid.add_theme_constant_override("v_separation", 1)
		for ln in lines:
			var lbl := str(ln[0])
			var val := str(ln[1])
			var absent: bool = ln.size() > 2 and ln[2]
			var key_cell := Label.new()
			key_cell.text = lbl
			key_cell.add_theme_color_override("font_color", Palette.INK3)
			key_cell.add_theme_font_size_override("font_size", 13)
			key_cell.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
			grid.add_child(key_cell)
			var val_cell := Label.new()
			val_cell.text = val
			val_cell.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
			val_cell.add_theme_color_override("font_color", Palette.RED if absent else Palette.INK2)
			val_cell.add_theme_font_size_override("font_size", 13)
			val_cell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			grid.add_child(val_cell)
		box.add_child(grid)

	return row


func _kv_line(label: String, value: String) -> Control:
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 0)
	var l := Label.new()
	l.text = label
	l.add_theme_color_override("font_color", Palette.INK3)
	l.add_theme_font_size_override("font_size", 13)
	box.add_child(l)
	var v := Label.new()
	v.text = value
	v.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	v.add_theme_color_override("font_color", Palette.INK2)
	v.add_theme_font_size_override("font_size", 14)
	box.add_child(v)
	var pad := MarginContainer.new()
	pad.add_theme_constant_override("margin_bottom", 4)
	pad.add_child(box)
	return pad


func _hairline() -> Control:
	var line := Panel.new()
	line.custom_minimum_size = Vector2(0, 1)
	var sb := StyleBoxFlat.new()
	sb.bg_color = Palette.LINE
	line.add_theme_stylebox_override("panel", sb)
	return line


# --- Highlight ---------------------------------------------------------------

func _highlight_row(row: PanelContainer) -> void:
	_clear_highlight()
	_highlighted = row
	row.add_theme_stylebox_override("panel", _row_style(true))
	# Scroll after layout settles (the page may have just become visible).
	_scroll_to.call_deferred(row)


func _clear_highlight() -> void:
	if _highlighted != null and is_instance_valid(_highlighted):
		var normal: StyleBox = _highlighted.get_meta("normal_style")
		_highlighted.add_theme_stylebox_override("panel", normal)
	_highlighted = null


func _scroll_to(row: Control) -> void:
	if is_instance_valid(row) and row.is_visible_in_tree():
		_scroll.ensure_control_visible(row)


# --- Styles + formatting -----------------------------------------------------

func _panel_style(fill: Color, border: Color) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = fill
	sb.set_border_width_all(1)
	sb.border_color = border
	sb.set_corner_radius_all(3)
	sb.content_margin_left = 12
	sb.content_margin_right = 12
	sb.content_margin_top = 10
	sb.content_margin_bottom = 10
	return sb


func _row_style(highlighted: bool) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.content_margin_left = 8
	sb.content_margin_right = 8
	sb.content_margin_top = 5
	sb.content_margin_bottom = 5
	sb.border_width_bottom = 1
	sb.border_color = Palette.LINE
	if highlighted:
		sb.bg_color = Color(Palette.BRASS, 0.14)
		sb.border_width_left = 3
		sb.border_color = Palette.BRASS
		sb.set_corner_radius_all(2)
	else:
		sb.bg_color = Color(1, 1, 1, 0)
	return sb


## Subtle hover style for clickable Quest Board rows — signals interactivity.
func _posting_row_hover_style() -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.content_margin_left = 8
	sb.content_margin_right = 8
	sb.content_margin_top = 5
	sb.content_margin_bottom = 5
	sb.border_width_bottom = 1
	sb.border_width_left = 2
	sb.border_color = Palette.GREEN
	sb.bg_color = Color(Palette.GREEN, 0.07)
	sb.set_corner_radius_all(2)
	return sb


func _tab_btn_style(pressed: bool, hover: bool = false) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.content_margin_left = 8
	sb.content_margin_right = 6
	sb.content_margin_top = 5
	sb.content_margin_bottom = 5
	sb.set_corner_radius_all(2)
	if pressed:
		sb.bg_color = Palette.SURFACE
		sb.border_width_left = 3
		sb.border_color = Palette.GREEN
	elif hover:
		sb.bg_color = Color(Palette.LINE, 0.5)
	else:
		sb.bg_color = Color(1, 1, 1, 0)
	return sb


## Tool tab style — mirrors _tab_btn_style but brass-accented, so the INSPECTION TOOLS
## group reads as a different kind than the green-pressed reference tabs. A faint brass
## wash marks even the resting state.
func _tool_btn_style(pressed: bool, hover: bool = false) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.content_margin_left = 8
	sb.content_margin_right = 6
	sb.content_margin_top = 5
	sb.content_margin_bottom = 5
	sb.set_corner_radius_all(2)
	if pressed:
		sb.bg_color = Color(Palette.BRASS, 0.12)
		sb.border_width_left = 3
		sb.border_color = Palette.BRASS
	elif hover:
		sb.bg_color = Color(Palette.BRASS, 0.07)
	else:
		sb.bg_color = Color(Palette.BRASS, 0.03)
	return sb


## Legible scalar/array formatting for a reference value.
func _fmt(v: Variant) -> String:
	match typeof(v):
		TYPE_NIL:
			return "—"
		TYPE_BOOL:
			return "yes" if v else "no"
		TYPE_FLOAT:
			if v == floor(v):
				return str(int(v))
			return str(v)
		TYPE_ARRAY:
			if (v as Array).is_empty():
				return "—"
			var parts: Array = []
			for e in v:
				parts.append(_fmt(e))
			return " · ".join(parts)
		TYPE_DICTIONARY:
			var kv: Array = []
			for k in v.keys():
				kv.append("%s %s" % [Loc.humanize(str(k)), _fmt(v[k])])
			return " · ".join(kv)
		TYPE_STRING, TYPE_STRING_NAME:
			# Reference values are authored content, rendered verbatim — EXCEPT a bare
			# snake_case token (no space, has "_"), which is a machine identifier: this
			# data's prose never uses underscores, so route it through the one humanizer
			# (e.g. a posting's type "standing_order" -> "Standing Order"). Kebab tokens
			# stay verbatim — the book's hyphenated tells ("twin-barbed") are content.
			var s := str(v)
			if not s.contains(" ") and s.contains("_"):
				return Loc.humanize(s)
			return s
		_:
			return str(v)

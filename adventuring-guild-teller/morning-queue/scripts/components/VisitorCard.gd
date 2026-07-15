extends PanelContainer
## VisitorCard — the counter: who is here and what they are claiming.
##
## The first thing the player reads each visitor. A parchment desk-card: an
## "AT THE COUNTER" eyebrow, the visitor's name in ink, a line of
## affiliation · profession · task, and their claim laid out as a document on the
## counter — a bordered paper carrying the summary and, if present, the claim's
## asserts as filled-in fields.
##
## DO NOT change the public contract below — Main.gd and Session depend on it.
##
## Contract:
##   show_visitor(v: Dictionary) -> void   present visitor v (fields per visitors.json)
## Signal:
##   papers_examined()                     emitted when the player taps the papers

signal papers_examined

const _EYEBROW_SIZE := 12
const _NAME_SIZE := 30
const _SUB_SIZE := 15
const _DOC_EYEBROW_SIZE := 11
const _CLAIM_SIZE := 15
const _KEY_SIZE := 13
const _VAL_SIZE := 14

var _name_label: Label
var _sub_label: Label
var _claim_label: Label
var _fields_rule: ColorRect
var _fields: VBoxContainer
var _tiles_area: VBoxContainer    # always-visible desk section
var _tiles_scroll: ScrollContainer  # height-capped scroll wrapper for tile cards
var _tiles_list: VBoxContainer    # individual tile cards
var _tiles_hint: Label            # shown when no tiles; hidden when tiles exist
var _tile_map: Dictionary = {}    # tile_id -> PanelContainer (replace-on-reuse)


func _ready() -> void:
	add_theme_stylebox_override("panel", _card_stylebox())

	var col := VBoxContainer.new()
	col.add_theme_constant_override("separation", 6)
	add_child(col)

	col.add_child(_eyebrow(Loc.t("counter_eyebrow"), _EYEBROW_SIZE))

	_name_label = Label.new()
	_name_label.text = "—"
	_name_label.add_theme_font_size_override("font_size", _NAME_SIZE)
	_name_label.add_theme_color_override("font_color", Palette.INK)
	_name_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	col.add_child(_name_label)

	_sub_label = Label.new()
	_sub_label.add_theme_font_size_override("font_size", _SUB_SIZE)
	_sub_label.add_theme_color_override("font_color", Palette.INK2)
	_sub_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	col.add_child(_sub_label)

	# A breath of counter before the paper is laid down.
	var gap := Control.new()
	gap.custom_minimum_size = Vector2(0, 4)
	col.add_child(gap)

	# The document — the claim laid on the counter as a sheet of paper.
	var doc := PanelContainer.new()
	doc.add_theme_stylebox_override("panel", _doc_stylebox())
	doc.mouse_filter = Control.MOUSE_FILTER_STOP
	doc.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	doc.gui_input.connect(_on_doc_input)
	col.add_child(doc)

	var doc_col := VBoxContainer.new()
	doc_col.add_theme_constant_override("separation", 8)
	doc.add_child(doc_col)

	doc_col.add_child(_eyebrow(Loc.t("claim_eyebrow"), _DOC_EYEBROW_SIZE))

	_claim_label = Label.new()
	_claim_label.add_theme_font_size_override("font_size", _CLAIM_SIZE)
	_claim_label.add_theme_color_override("font_color", Palette.INK2)
	_claim_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	doc_col.add_child(_claim_label)

	_fields_rule = _hairline()
	doc_col.add_child(_fields_rule)

	_fields = VBoxContainer.new()
	_fields.add_theme_constant_override("separation", 5)
	doc_col.add_child(_fields)

	# ── Tiles area (always visible) ──────────────────────────────────────────
	# Shows a first-use hint when empty; a height-capped ScrollContainer with
	# individual dismiss buttons when reference tiles have been placed.
	_tiles_area = VBoxContainer.new()
	_tiles_area.add_theme_constant_override("separation", 4)
	col.add_child(_tiles_area)

	var tiles_head := Label.new()
	tiles_head.text = Loc.t("desk_tiles_head")
	tiles_head.add_theme_color_override("font_color", Palette.INK3)
	tiles_head.add_theme_font_size_override("font_size", 11)
	_tiles_area.add_child(tiles_head)

	_tiles_hint = Label.new()
	_tiles_hint.text = Loc.t("desk_tiles_hint")
	_tiles_hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_tiles_hint.add_theme_color_override("font_color", Palette.INK3)
	_tiles_hint.add_theme_font_size_override("font_size", 12)
	_tiles_area.add_child(_tiles_hint)

	# ScrollContainer caps tile accumulation at 170px — prevents tiles from
	# overflowing the card and pushing the VerdictBar off-screen.
	_tiles_scroll = ScrollContainer.new()
	_tiles_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_tiles_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	_tiles_scroll.custom_minimum_size = Vector2(0, 170)
	_tiles_scroll.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	_tiles_scroll.visible = false
	_tiles_area.add_child(_tiles_scroll)

	_tiles_list = VBoxContainer.new()
	_tiles_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_tiles_list.add_theme_constant_override("separation", 5)
	_tiles_scroll.add_child(_tiles_list)


## FROZEN — Main.gd calls this on the scene root for every visitor (and once with a
## sparse {name, claim.summary} dict on data error), so every field is read defensively.
func show_visitor(v: Dictionary) -> void:
	if v.is_empty():
		return

	_name_label.text = str(v.get("name", "?"))

	var parts := PackedStringArray()
	var aff := Loc.affiliation(str(v.get("affiliation", "")))
	if aff != "":
		parts.append(aff)
	var prof := str(v.get("profession", ""))
	if prof != "":
		parts.append(prof)
	var task := Loc.task_type(str(v.get("task_type", "")))
	if task != "":
		parts.append(task)
	_sub_label.text = "   ·   ".join(parts)
	_sub_label.visible = parts.size() > 0

	var claim: Dictionary = v.get("claim", {})
	_claim_label.text = "“%s”" % str(claim.get("summary", ""))
	_populate_fields(claim.get("asserts", {}))


# --- papers ------------------------------------------------------------------

func _on_doc_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		papers_examined.emit()


func _populate_fields(asserts: Variant) -> void:
	for child in _fields.get_children():
		child.queue_free()

	var has_fields := asserts is Dictionary and not (asserts as Dictionary).is_empty()
	_fields_rule.visible = has_fields
	_fields.visible = has_fields
	if not has_fields:
		return

	for key in (asserts as Dictionary):
		_fields.add_child(_field_row(Loc.humanize(str(key)), Loc.pretty_value(asserts[key])))


func _field_row(key_text: String, value_text: String) -> HBoxContainer:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)

	var key_label := Label.new()
	key_label.text = key_text
	key_label.add_theme_font_size_override("font_size", _KEY_SIZE)
	key_label.add_theme_color_override("font_color", Palette.INK3)
	key_label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	key_label.custom_minimum_size = Vector2(132, 0)
	row.add_child(key_label)

	var value_label := Label.new()
	value_label.text = value_text
	value_label.add_theme_font_size_override("font_size", _VAL_SIZE)
	value_label.add_theme_color_override("font_color", Palette.INK2)
	value_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	value_label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	value_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(value_label)

	return row


# --- desk tiles (ADDITIVE) ---------------------------------------------------

## Clear all tiles from the desk (called by Main on visitor change).
func clear_tiles() -> void:
	for c in _tiles_list.get_children():
		c.queue_free()
	_tile_map.clear()
	_tiles_scroll.visible = false
	_tiles_hint.visible = true


## Place a reference tile on the desk below the claim. If `tile_id` already has a
## tile, the old one is replaced — so clicking The Glass twice gives one tile.
## `tint` sets the left-border accent and header colour (BRASS for tools, GREEN for
## postings). Called by Main when ReferencePanel.tile_requested fires.
func add_tile(tile_id: String, title: String, body: String, tint: Color) -> void:
	if _tile_map.has(tile_id):
		var old: Variant = _tile_map[tile_id]
		if old is Node and is_instance_valid(old):
			(old as Node).queue_free()
		_tile_map.erase(tile_id)
	var tile := _build_tile(tile_id, title, body, tint)
	_tiles_list.add_child(tile)
	_tile_map[tile_id] = tile
	_tiles_hint.visible = false
	_tiles_scroll.visible = true


## Called by the × button on an individual tile.
func _dismiss_tile(tile_id: String) -> void:
	if not _tile_map.has(tile_id):
		return
	var tile: Variant = _tile_map[tile_id]
	_tile_map.erase(tile_id)
	if tile is Node and is_instance_valid(tile):
		(tile as Node).queue_free()
	if _tile_map.is_empty():
		_tiles_scroll.visible = false
		_tiles_hint.visible = true


func _build_tile(tile_id: String, title: String, body: String, tint: Color) -> PanelContainer:
	var tile := PanelContainer.new()
	var sb := StyleBoxFlat.new()
	sb.bg_color = Palette.SURFACE
	sb.border_color = tint
	sb.border_width_left = 3
	sb.border_width_top = 1
	sb.border_width_right = 1
	sb.border_width_bottom = 1
	sb.set_corner_radius_all(2)
	sb.content_margin_left = 12
	sb.content_margin_right = 10
	sb.content_margin_top = 7
	sb.content_margin_bottom = 9
	sb.shadow_color = Color(Palette.INK.r, Palette.INK.g, Palette.INK.b, 0.07)
	sb.shadow_size = 2
	sb.shadow_offset = Vector2(1, 1)
	tile.add_theme_stylebox_override("panel", sb)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 3)
	tile.add_child(box)

	# Header row: title + dismiss button
	var header_row := HBoxContainer.new()
	header_row.add_theme_constant_override("separation", 4)
	box.add_child(header_row)

	var title_lbl := Label.new()
	title_lbl.text = title
	title_lbl.add_theme_color_override("font_color", tint)
	title_lbl.add_theme_font_size_override("font_size", 12)
	title_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header_row.add_child(title_lbl)

	var dismiss_btn := Button.new()
	dismiss_btn.text = "×"
	dismiss_btn.flat = true
	dismiss_btn.add_theme_font_size_override("font_size", 16)
	dismiss_btn.add_theme_color_override("font_color", Palette.INK3)
	dismiss_btn.add_theme_color_override("font_hover_color", Palette.INK)
	dismiss_btn.focus_mode = Control.FOCUS_NONE
	dismiss_btn.custom_minimum_size = Vector2(22, 0)
	dismiss_btn.pressed.connect(_dismiss_tile.bind(tile_id))
	header_row.add_child(dismiss_btn)

	var body_lbl := Label.new()
	body_lbl.text = body
	body_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	body_lbl.add_theme_color_override("font_color", Palette.INK2)
	body_lbl.add_theme_font_size_override("font_size", 14)
	box.add_child(body_lbl)

	return tile


# --- construction helpers ----------------------------------------------------

func _eyebrow(text: String, px: int) -> Label:
	var label := Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", px)
	label.add_theme_color_override("font_color", Palette.INK3)
	return label


func _hairline() -> ColorRect:
	var line := ColorRect.new()
	line.color = Palette.LINE
	line.custom_minimum_size = Vector2(0, 1)
	return line


func _card_stylebox() -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = Palette.GROUND
	sb.border_color = Palette.LINE2
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(3)
	sb.content_margin_left = 20
	sb.content_margin_right = 20
	sb.content_margin_top = 16
	sb.content_margin_bottom = 18
	return sb


func _doc_stylebox() -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = Palette.SURFACE
	sb.border_color = Palette.LINE
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(2)
	sb.content_margin_left = 16
	sb.content_margin_right = 16
	sb.content_margin_top = 12
	sb.content_margin_bottom = 14
	sb.shadow_color = Color(Palette.INK.r, Palette.INK.g, Palette.INK.b, 0.10)
	sb.shadow_size = 3
	sb.shadow_offset = Vector2(1, 2)
	return sb

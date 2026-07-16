extends Control
## The observatory — the only "gameplay" in v0: data readouts of the fish-bowl plus debug
## knobs and the creation menus. All state comes from the FishbowlBridge autoload as JSON;
## this script never contains sim logic (GDScript is node scripting only).

var bridge: Node
var selected_id: String = ""
var view_day: int = 1

var clock_label: Label
var hash_label: Label
var seed_label: Label
var seed_spin: SpinBox
var roster_tree: Tree
var places_box: GridContainer
var chronicle_tree: Tree
var inspector_box: VBoxContainer
var summary_box: VBoxContainer
var register_label: Label
var stats_label: Label

const DRIVES := ["purse", "trade", "heart", "restlessness"]

func _ready() -> void:
	bridge = get_node("/root/FishbowlBridge")
	# C# [Signal] delegates keep their PascalCase names in GDScript (unlike built-in signals).
	bridge.SlotTicked.connect(_on_slot_ticked)
	bridge.DawnReady.connect(_on_dawn_ready)
	bridge.HashReady.connect(func(_d, _h): _refresh_top())
	_build_ui()
	_refresh_all()

# --- JSON helper ---------------------------------------------------------------------------

func _j(s: String) -> Variant:
	return JSON.parse_string(s)

# In-engine viewport capture harness (readable .captures/ folder; ships as a manual key, never
# the OS screenshotter). Godot validation workflow: press F9 to grab the current observatory.
func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.keycode == KEY_F9:
		var img := get_viewport().get_texture().get_image()
		DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://.captures"))
		img.save_png("res://.captures/observatory.png")
		print("[capture] res://.captures/observatory.png")

# --- UI construction -----------------------------------------------------------------------

func _build_ui() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 6)
	add_child(root)

	root.add_child(_build_top_bar())

	var body := HBoxContainer.new()
	body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_theme_constant_override("separation", 8)
	root.add_child(body)

	body.add_child(_col("Roster", 0.30, func(v): _build_roster(v)))
	body.add_child(_col("Town", 0.40, func(v): _build_center(v)))
	body.add_child(_col("Read", 0.30, func(v): _build_right(v)))

func _build_top_bar() -> Control:
	var panel := PanelContainer.new()
	var bar := HBoxContainer.new()
	bar.add_theme_constant_override("separation", 10)
	panel.add_child(bar)

	clock_label = _mk_label("", 18)
	clock_label.set_meta("test_id", "clock")
	hash_label = _mk_label("", 13)
	hash_label.set_meta("test_id", "hash")
	seed_label = _mk_label("", 13)
	seed_label.set_meta("test_id", "seed")
	bar.add_child(clock_label)
	bar.add_child(_sep())
	bar.add_child(hash_label)
	bar.add_child(_sep())
	bar.add_child(seed_label)
	bar.add_child(_sep())

	bar.add_child(_btn("Step", _on_step, "btn-step"))
	bar.add_child(_btn("Run to Dawn", _on_dawn, "btn-dawn"))
	bar.add_child(_btn("Run 3 Days", func(): _run_days(3), "btn-run3"))

	bar.add_child(_sep())
	seed_spin = SpinBox.new()
	seed_spin.min_value = 0
	seed_spin.max_value = 999999
	seed_spin.value = 1123
	seed_spin.set_meta("test_id", "seed-spin")
	bar.add_child(seed_spin)
	bar.add_child(_btn("Reseed", _on_reseed, "btn-reseed"))

	bar.add_child(_sep())
	bar.add_child(_btn("Generate…", _open_generator, "btn-generate"))
	bar.add_child(_btn("New Townee…", _open_townee_creator, "btn-townee"))
	bar.add_child(_btn("New Place…", _open_place_creator, "btn-place"))
	bar.add_child(_btn("Storylets…", _open_storylets, "btn-storylets"))
	return panel

func _col(_title: String, ratio: float, builder: Callable) -> Control:
	var v := VBoxContainer.new()
	v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	v.size_flags_stretch_ratio = ratio
	builder.call(v)
	return v

func _build_roster(v: VBoxContainer) -> void:
	v.add_child(_header("Roster"))
	roster_tree = Tree.new()
	roster_tree.set_meta("test_id", "roster")
	roster_tree.columns = 5
	roster_tree.hide_root = true
	roster_tree.column_titles_visible = true
	roster_tree.set_column_title(0, "Name")
	roster_tree.set_column_title(1, "Role")
	roster_tree.set_column_title(2, "Place now")
	roster_tree.set_column_title(3, "Doing")
	roster_tree.set_column_title(4, "Top")
	roster_tree.size_flags_vertical = Control.SIZE_EXPAND_FILL
	roster_tree.item_selected.connect(_on_roster_selected)
	v.add_child(roster_tree)

func _build_center(v: VBoxContainer) -> void:
	v.add_child(_header("Place board"))
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 210)
	places_box = GridContainer.new()
	places_box.columns = 2
	places_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(places_box)
	v.add_child(scroll)

	v.add_child(_header("Chronicle — expand for the because-list"))
	chronicle_tree = Tree.new()
	chronicle_tree.set_meta("test_id", "chronicle")
	chronicle_tree.hide_root = true
	chronicle_tree.size_flags_vertical = Control.SIZE_EXPAND_FILL
	v.add_child(chronicle_tree)

func _build_right(v: VBoxContainer) -> void:
	v.add_child(_header("Dawn summary"))
	register_label = _mk_label("", 12)
	register_label.set_meta("test_id", "register")
	v.add_child(register_label)
	summary_box = VBoxContainer.new()
	summary_box.set_meta("test_id", "summary")
	v.add_child(summary_box)
	v.add_child(_build_knobs())
	stats_label = _mk_label("", 12)
	stats_label.set_meta("test_id", "stats")
	v.add_child(stats_label)

	v.add_child(_header("Inspector"))
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	inspector_box = VBoxContainer.new()
	inspector_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(inspector_box)
	v.add_child(scroll)

func _build_knobs() -> Control:
	var box := VBoxContainer.new()
	box.add_child(_header("Debug knobs"))
	box.add_child(_slider("actionability", 0.0, 1.0, 0.01, 0.5, func(x): _knob("actionability", x)))
	box.add_child(_slider("storylet_rate", 0.0, 3.0, 0.05, 1.0, func(x): _knob("storylet_rate", x)))
	box.add_child(_slider("pressure_rates.trade", 0.0, 3.0, 0.05, 1.0, func(x): _knob("pressure_rates.trade", x)))
	box.add_child(_slider("summary_lines", 3, 7, 1, 5, func(x): _knob("summary_lines", x)))
	box.add_child(_check("hearsay_required", true, func(on): _knob("hearsay_required", 1.0 if on else 0.0)))
	box.add_child(_check("bio_marks_enabled", true, func(on): _knob("bio_marks_enabled", 1.0 if on else 0.0)))
	return box

# --- refresh -------------------------------------------------------------------------------

func _refresh_all() -> void:
	_refresh_top()
	_refresh_roster()
	_refresh_places()
	_refresh_chronicle()
	_refresh_summary()
	_refresh_stats()
	_refresh_inspector()

func _refresh_top() -> void:
	var c = _j(bridge.GetClock())
	clock_label.text = "Day %d · %s (slot %d)" % [c.day, c.clock, c.slot]
	hash_label.text = "hash %s" % c.hash
	seed_label.text = "seed %d" % int(c.seed)

func _refresh_roster() -> void:
	roster_tree.clear()
	var root := roster_tree.create_item()
	var data = _j(bridge.GetRoster())
	for t in data.townees:
		var it := roster_tree.create_item(root)
		it.set_text(0, t.name)
		it.set_text(1, t.role)
		it.set_text(2, "away" if t.away else t.place_name)
		it.set_text(3, t.activity)
		it.set_text(4, "%s %.2f" % [t.top_drive, t.top_value])
		it.set_metadata(0, t.id)

func _refresh_places() -> void:
	for c in places_box.get_children():
		c.queue_free()
	var data = _j(bridge.GetPlaces())
	for p in data.places:
		if not p.board:
			continue
		var card := PanelContainer.new()
		var vb := VBoxContainer.new()
		var title = p.name + (" (shut)" if p.shut else "")
		vb.add_child(_mk_label(title, 13))
		var who := ""
		for o in p.occupants:
			who += "• %s\n" % o.name
		vb.add_child(_mk_label(who if who != "" else "—", 11))
		card.add_child(vb)
		card.custom_minimum_size = Vector2(0, 92)
		places_box.add_child(card)

func _refresh_chronicle() -> void:
	chronicle_tree.clear()
	var root := chronicle_tree.create_item()
	var data = _j(bridge.GetChronicle(view_day))
	for e in data.events:
		var it := chronicle_tree.create_item(root)
		it.set_text(0, "[%s] %s" % [e.clock, e.gossip])
		it.collapsed = true
		var because := chronicle_tree.create_item(it)
		because.set_text(0, "because —")
		for b in e.because:
			var bi := chronicle_tree.create_item(because)
			bi.set_text(0, "  %s: %s" % [b.label, b.value])
		var variants := chronicle_tree.create_item(it)
		variants.set_text(0, "reads —")
		for key in ["hearsay", "gossip", "report"]:
			var vi := chronicle_tree.create_item(variants)
			vi.set_text(0, "  %s: %s" % [key, e[key]])

func _refresh_summary() -> void:
	for c in summary_box.get_children():
		c.queue_free()
	var s = _j(bridge.GetSummary(view_day))
	register_label.text = "Day %d · register: %s" % [view_day, s.register]
	for line in s.lines:
		summary_box.add_child(_mk_label("• " + line.text, 12))

func _refresh_stats() -> void:
	var s = _j(bridge.GetStats(view_day))
	var warn := "  ⚠ starvation" if s.starvation else ""
	stats_label.text = "events %d · distinct types %d%s" % [s.events, s.distinct_candidates, warn]

func _refresh_inspector() -> void:
	for c in inspector_box.get_children():
		c.queue_free()
	if selected_id == "":
		inspector_box.add_child(_mk_label("Select a townee.", 12))
		return
	var t = _j(bridge.GetTownee(selected_id))
	inspector_box.add_child(_mk_label("%s — %s" % [t.name, t.role], 16))
	var bio := _mk_label(t.bio, 12)
	bio.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	inspector_box.add_child(bio)
	inspector_box.add_child(_mk_label("traits: " + ", ".join(t.traits), 11))
	if t.marks.size() > 0:
		inspector_box.add_child(_header("marks (bio)"))
		for m in t.marks:
			inspector_box.add_child(_mk_label("Day %d: %s" % [m.day, m.line], 11))

	inspector_box.add_child(_header("pressures (trailing 3 days)"))
	for d in DRIVES:
		var row := HBoxContainer.new()
		row.add_child(_mk_label("%s %.2f" % [d, float(t.pressures[d])], 11))
		var spark := Sparkline.new()
		spark.custom_minimum_size = Vector2(120, 26)
		var series = _j(bridge.GetPressureSeries(selected_id, d))
		spark.set_values(series.values)
		row.add_child(spark)
		inspector_box.add_child(row)

	inspector_box.add_child(_header("regard — outgoing"))
	for r in t.regard_out:
		inspector_box.add_child(_mk_label("→ %s  %.2f  [%s]" % [r.name, float(r.score), ", ".join(r.tags)], 11))
	inspector_box.add_child(_header("regard — incoming"))
	for r in t.regard_in:
		inspector_box.add_child(_mk_label("← %s  %.2f  [%s]" % [r.name, float(r.score), ", ".join(r.tags)], 11))

# --- actions -------------------------------------------------------------------------------

func _on_step() -> void:
	bridge.StepSlot()
	_refresh_all()

func _on_dawn() -> void:
	bridge.RunToDawn()
	view_day = int(bridge.CurrentDay()) - 1
	_refresh_all()

func _run_days(n: int) -> void:
	for i in range(n):
		bridge.RunToDawn()
	view_day = int(bridge.CurrentDay()) - 1
	_refresh_all()

func _on_reseed() -> void:
	bridge.Reseed(int(seed_spin.value))
	_refresh_top()

func _on_roster_selected() -> void:
	var it := roster_tree.get_selected()
	if it:
		selected_id = str(it.get_metadata(0))
		_refresh_inspector()

func _knob(knob: String, value: float) -> void:
	bridge.SetKnob(knob, value)
	_refresh_summary()
	_refresh_stats()
	_refresh_roster()

func _on_slot_ticked(_day: int, _slot: int) -> void:
	_refresh_top()

func _on_dawn_ready(day: int, _summary: String) -> void:
	view_day = day

# --- creation menus (compact dialogs) ------------------------------------------------------

func _open_generator() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "Generate town (seeded)"
	var box := VBoxContainer.new()
	var seed_sp := _spin_row(box, "seed", 0, 999999, 1123)
	var count := _spin_row(box, "count", 4, 40, 12)
	var density := _spin_row(box, "relationship density %", 0, 100, 15)
	dlg.add_child(box)
	add_child(dlg)
	dlg.confirmed.connect(func():
		var cfg := {"seed": int(seed_sp.value), "count": int(count.value), "relationship_density": density.value / 100.0}
		var res = _j(bridge.GenerateTown(JSON.stringify(cfg)))
		_toast(res)
		selected_id = ""
		_refresh_all())
	dlg.popup_centered(Vector2i(360, 200))

func _open_townee_creator() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "New townee"
	var box := VBoxContainer.new()
	var id_e := _text_row(box, "id", "new-townee")
	var name_e := _text_row(box, "name", "New Townee")
	var role_e := _text_row(box, "role", "fisher")
	var places = _j(bridge.GetPlaces()).places
	var home := _option_row(box, "home", places)
	var dayplan_e := _text_row(box, "dayplan", "fisher-default")
	dlg.add_child(box)
	add_child(dlg)
	dlg.confirmed.connect(func():
		var dto := {
			"id": id_e.text, "name": name_e.text, "role": role_e.text, "adventurer": false,
			"traits": ["patient"], "dayplan": dayplan_e.text,
			"home": home.get_item_metadata(home.selected), "work": home.get_item_metadata(home.selected),
			"haunts": [], "pressures": {"purse": 0.4, "trade": 0.4, "heart": 0.5, "restlessness": 0.3},
			"regard": {}, "teller_regard": 0.5, "bio": name_e.text + " just arrived in the village.",
		}
		_toast(_j(bridge.CreateTownee(JSON.stringify(dto))))
		_refresh_all())
	dlg.popup_centered(Vector2i(380, 260))

func _open_place_creator() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "New place"
	var box := VBoxContainer.new()
	var id_e := _text_row(box, "id", "new-place")
	var name_e := _text_row(box, "name", "New Place")
	var kind_e := _text_row(box, "kind", "shop")
	var cap := _spin_row(box, "capacity", 1, 40, 8)
	dlg.add_child(box)
	add_child(dlg)
	dlg.confirmed.connect(func():
		var dto := {
			"id": id_e.text, "name": name_e.text, "kind": kind_e.text,
			"hours": {"open": 14, "close": 34}, "capacity": int(cap.value), "board": true,
		}
		_toast(_j(bridge.CreatePlace(JSON.stringify(dto))))
		_refresh_all())
	dlg.popup_centered(Vector2i(360, 240))

func _open_storylets() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "Storylet browser — force-fire (debug)"
	var box := VBoxContainer.new()
	for s in _j(bridge.GetStorylets()).storylets:
		var row := HBoxContainer.new()
		var must := " ★" if s.must_fire else ""
		row.add_child(_mk_label("%s  (w%.1f cd%d)%s" % [s.id, float(s.weight), int(s.cooldown), must], 12))
		var sid: String = s.id
		row.add_child(_btn("fire", func(): _toast(_j(bridge.InjectStorylet(sid, "[]"))); _refresh_all()))
		box.add_child(row)
	dlg.add_child(box)
	add_child(dlg)
	dlg.popup_centered(Vector2i(420, 420))

func _toast(res: Variant) -> void:
	if typeof(res) == TYPE_DICTIONARY and res.has("error"):
		push_warning("bridge error: " + str(res.error))

# --- tiny widget helpers -------------------------------------------------------------------

func _mk_label(text: String, font_size: int) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", font_size)
	return l

func _header(text: String) -> Label:
	var l := _mk_label(text.to_upper(), 11)
	l.modulate = Color(0.6, 0.75, 0.85)
	return l

func _sep() -> Control:
	var s := VSeparator.new()
	return s

func _btn(text: String, cb: Callable, tid := "") -> Button:
	var b := Button.new()
	b.text = text
	b.pressed.connect(cb)
	if tid != "":
		b.set_meta("test_id", tid)  # GTH: stable handle for the test harness
	return b

func _slider(knob: String, lo: float, hi: float, step: float, val: float, cb: Callable) -> Control:
	var row := HBoxContainer.new()
	var lbl := _mk_label("%s %.2f" % [knob, val], 11)
	lbl.custom_minimum_size = Vector2(160, 0)
	row.add_child(lbl)
	var sl := HSlider.new()
	sl.set_meta("test_id", "knob-" + knob)  # GTH: the VFB.Q1 tuning surface needs a stable handle
	sl.min_value = lo
	sl.max_value = hi
	sl.step = step
	sl.value = val
	sl.custom_minimum_size = Vector2(120, 0)
	sl.value_changed.connect(func(x):
		lbl.text = "%s %.2f" % [knob, x]
		cb.call(x))
	row.add_child(sl)
	return row

func _check(knob: String, on: bool, cb: Callable) -> Control:
	var c := CheckBox.new()
	c.set_meta("test_id", "knob-" + knob)  # GTH: stable handle (see _slider)
	c.text = knob
	c.button_pressed = on
	c.toggled.connect(cb)
	return c

func _spin_row(box: VBoxContainer, label: String, lo: float, hi: float, val: float) -> SpinBox:
	var row := HBoxContainer.new()
	row.add_child(_mk_label(label, 12))
	var sp := SpinBox.new()
	sp.min_value = lo
	sp.max_value = hi
	sp.value = val
	row.add_child(sp)
	box.add_child(row)
	return sp

func _text_row(box: VBoxContainer, label: String, val: String) -> LineEdit:
	var row := HBoxContainer.new()
	row.add_child(_mk_label(label, 12))
	var e := LineEdit.new()
	e.text = val
	e.custom_minimum_size = Vector2(200, 0)
	row.add_child(e)
	box.add_child(row)
	return e

func _option_row(box: VBoxContainer, label: String, places: Array) -> OptionButton:
	var row := HBoxContainer.new()
	row.add_child(_mk_label(label, 12))
	var opt := OptionButton.new()
	for p in places:
		opt.add_item(p.name)
		opt.set_item_metadata(opt.item_count - 1, p.id)
	row.add_child(opt)
	box.add_child(row)
	return opt

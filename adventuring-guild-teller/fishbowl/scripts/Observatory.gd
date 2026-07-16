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

# --- roster glyphs ---------------------------------------------------------------------------
# Presentation only. Every table keys off a *stable* field on the roster projection — the role id,
# the place KIND, the clockwork MODE — never the authored display prose, which is free text that
# gets re-authored per day-plan. Unknown keys fall through to a neutral glyph rather than an empty
# cell, because the generator and the "New Townee…"/"New Place…" dialogs can both mint values that
# were never in data/.
#
# Every one of these columns is emoji-only, which is illegible to anyone who does not know the key.
# The mitigation is that _refresh_roster puts the ORIGINAL WORD in each cell's tooltip — the tooltip
# is the old column, one hover away. Nothing the table used to say has been destroyed.

const ROLE_GLYPH := {
	"innkeep": "🍺", "herbalist": "🌿", "smith": "🔨", "apprentice": "🎓",
	"adventurer": "⚔️", "courier": "📨", "landlady": "🔑", "fisher": "🎣",
	"miller": "🌾", "baker": "🍞", "market warden": "🛡️",
}

# Place kind → glyph. "away" is deliberately NOT a place kind: it is absence, not a building, and it
# gets its own glyph so an out-of-town adventurer never reads as "somewhere in the village".
const PLACE_GLYPH := {
	"inn": "🍺", "shop": "🏪", "workshop": "🔨", "market": "🛒",
	"work": "🌊", "landmark": "🏛️", "home": "🏠",
}
const PLACE_GLYPH_AWAY := "🧭"

# Clockwork mode → glyph, with asleep winning over the mode (a sleeping townee's mode is still
# "home", and "asleep" is the more useful thing to know at a glance).
#
# haunt is 💬 ("off duty, out among people") and deliberately NOT 🍻, for two reasons found in the
# slot-40 capture: 🍻 sat directly beside the inn's 🍺 in the Place column and the two amber mugs
# were near-indistinguishable at 20px — different facts that looked alike, which is worse than
# redundancy. And a haunt is not always the pub: Tam haunts the Guildhall Steps "watching the road",
# where 🍻 was not merely vague but wrong.
const MODE_GLYPH := {
	"work": "💼", "home": "🏠", "haunt": "💬", "away": "🧭",
}
const MODE_GLYPH_ASLEEP := "😴"

const DRIVE_GLYPH := {
	"purse": "💰", "trade": "🤝", "heart": "❤️", "restlessness": "🌀",
}

const GLYPH_UNKNOWN := "❔"

# Honorific-led names are the one case where the given-name+initial rule destroys the only
# distinguishing token: "Widow Karsk" → "Widow K." identifies *less* than the full name does,
# because "Widow" is a shared class and "Karsk" is the actual handle. Keep those whole.
const NAME_HONORIFICS := ["widow", "widower", "old", "young", "goodwife", "goodman",
	"father", "mother", "sister", "brother", "captain", "sergeant"]

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
	# Titles stay WORDS even though every cell under them is a glyph. The header is the only legend
	# on screen: if it went emoji too there would be no anchor anywhere for a reader who doesn't know
	# the key, and it costs nothing to keep — it renders once, not once per row, so it buys no density.
	roster_tree.set_column_title(0, "Name")
	roster_tree.set_column_title(1, "Role")
	# "Place now" → "Place": a Tree column can never be narrower than its own title, so the longest
	# title was taxing the narrowest column and starving Name. "now" was redundant anyway — the whole
	# table is a now-readout, and the clock sits directly above it.
	roster_tree.set_column_title(2, "Place")
	roster_tree.set_column_title(3, "Doing")
	roster_tree.set_column_title(4, "Top")
	# Four of the five columns are now a single glyph (or a glyph + a short number), so hand the
	# reclaimed width back to Name — the only column left carrying variable-length text. Ratios only
	# split the *leftover* after minimums, so Top also gets an explicit floor: it must always fit
	# "💰 0.60", and a clipped readout value would be worse than a clipped name.
	roster_tree.set_column_expand_ratio(0, 8)
	roster_tree.set_column_expand_ratio(1, 1)
	roster_tree.set_column_expand_ratio(2, 1)
	roster_tree.set_column_expand_ratio(3, 1)
	roster_tree.set_column_expand_ratio(4, 2)
	roster_tree.set_column_custom_minimum_width(0, 110)
	roster_tree.set_column_custom_minimum_width(4, 74)
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

		# Name — given name + surname initial; tooltip restores the full name.
		it.set_text(0, _short_name(t.name))
		it.set_tooltip_text(0, t.name)

		# Role — tooltip carries the role word verbatim.
		it.set_text(1, ROLE_GLYPH.get(t.role, GLYPH_UNKNOWN))
		it.set_tooltip_text(1, t.role)

		# Place now — keyed off the place kind, not the id, so a generated or hand-made place still
		# maps. `away` keeps the projection's own semantics (it is the field the old code read).
		if t.away:
			it.set_text(2, PLACE_GLYPH_AWAY)
			it.set_tooltip_text(2, "away")
		else:
			it.set_text(2, PLACE_GLYPH.get(t.place_kind, GLYPH_UNKNOWN))
			it.set_tooltip_text(2, t.place_name)

		# Doing — keyed off the clockwork mode, but the tooltip carries the authored activity prose
		# ("tending the room"), which is richer than the mode word it was keyed from.
		it.set_text(3, MODE_GLYPH_ASLEEP if t.asleep else MODE_GLYPH.get(t.mode, GLYPH_UNKNOWN))
		it.set_tooltip_text(3, t.activity)

		# Top — a readout, so the number stays; only the drive name becomes a glyph.
		it.set_text(4, "%s %.2f" % [DRIVE_GLYPH.get(t.top_drive, GLYPH_UNKNOWN), t.top_value])
		it.set_tooltip_text(4, "%s %.2f" % [t.top_drive, t.top_value])

		it.set_metadata(0, t.id)

## "Odile Vance" → "Odile V."  ·  "Petch" → "Petch" (mononym — no stray period)
## "Widow Karsk" → "Widow Karsk" (honorific + name; see NAME_HONORIFICS)
func _short_name(full: String) -> String:
	var parts := full.strip_edges().split(" ", false)
	if parts.size() == 0:
		return full
	if parts.size() == 1:
		return parts[0]
	if parts.size() == 2 and NAME_HONORIFICS.has(parts[0].to_lower()):
		return "%s %s" % [parts[0], parts[1]]
	return "%s %s." % [parts[0], parts[parts.size() - 1].substr(0, 1).to_upper()]

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
		# Without autowrap a Label's minimum width is its whole single-line text, so one long
		# summary line (~670px) became the Read column's minimum and the body HBox's stretch
		# ratios could only divide what was left — starving the roster to ~250px and pushing its
		# Top column out of view. The inspector's bio label two functions down already does this;
		# the summary was just missed. Not part of the roster work — see the report.
		var lbl := _mk_label("• " + line.text, 12)
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		summary_box.add_child(lbl)

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

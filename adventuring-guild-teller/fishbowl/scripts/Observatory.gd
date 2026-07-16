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
var places_box: VBoxContainer
var chronicle_box: VBoxContainer
var chronicle_header: Label
var summary_header: Label
var inspector_box: VBoxContainer
var summary_box: VBoxContainer
var register_label: Label
var stats_label: Label
var _mono_font: SystemFont

const DRIVES := ["purse", "trade", "heart", "restlessness"]

# --- roster encoding -------------------------------------------------------------------------
# ONE column is glyphs. That is a deliberate retreat from four, and the reason is not taste.
#
# The old encoding ran Role / Place / Doing / Top through four separate glyph tables, and those
# tables SHARED SYMBOLS: 🍺 was both ROLE_GLYPH["innkeep"] and PLACE_GLYPH["inn"], so the innkeep's
# row read `🍺 | 🍺`; 🔨 was both "smith" and "workshop"; 🧭 was both the away-place and the away-mode.
# A legend maps symbol → meaning. That encoding had no such map — the meaning depended on which
# column you were in — so a legend for it would have had to be one legend PER COLUMN, i.e. three
# more tables on screen to explain the four already there. The legend was not expensive; it was
# ill-defined. Removing the collisions is what makes a legend possible at all.
#
# And the density the glyphs bought was never needed: the roster is ~46% empty vertically in every
# state (there are always 12 townees) and was over-allocated ~130px horizontally. It traded
# legibility for space it already had. Words fit. The tooltips stay, but they are no longer the
# mitigation — a published screenshot has no hover, so nothing may depend on one.
#
# What survives as glyphs is `Doing`, and only `Doing`: 5 values, changing every slot, genuinely
# scanned as a column ("who is awake yet?"). One glyph column means one vocabulary, which means the
# five-symbol legend under the roster is well-defined and fits on two lines.

# Closed vocabularies — these are the valid values, and the creation dialogs offer exactly these
# (a free-text `role` typo used to mint a townee that no table could render). Keeping them as the
# dialogs' option lists is what makes the dropdown self-documenting.
const ROLES := ["innkeep", "herbalist", "smith", "apprentice", "adventurer", "courier",
	"landlady", "fisher", "miller", "baker", "market warden"]
const PLACE_KINDS := ["inn", "shop", "workshop", "market", "work", "landmark", "home"]

# Clockwork mode → glyph, with asleep winning over the mode (a sleeping townee's mode is still
# "home", and "asleep" is the more useful thing to know at a glance).
#
# haunt is 💬 ("off duty, out among people") and deliberately NOT 🍻: a haunt is not always the pub —
# Tam haunts the Guildhall Steps "watching the road", where 🍻 was not merely vague but wrong.
const MODE_GLYPH := {
	"work": "💼", "home": "🏠", "haunt": "💬", "away": "🧭",
}
const MODE_GLYPH_ASLEEP := "😴"

# The on-screen legend for the one glyph column. Lives here, next to the table it explains, so the
# two cannot drift apart.
const DOING_LEGEND := "😴 asleep · 💼 working · 🏠 at home · 💬 out among people · 🧭 away"

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

## The frame. THREE regions, and only ONE of them is allowed to negotiate its width.
##
## The bug this shape exists to make impossible: a bare Label reports its whole single-line text as
## a minimum width, an HBox's minimum is the sum of its children's, and a Control's size is clamped
## to at least its own minimum — so a minimum BEATS a PRESET_FULL_RECT anchor. The `hash` readout
## grew 47px → 155px when it populated at first dawn, and that 108px walked up hash_label → top-bar
## HBox → PanelContainer → root VBox and forced the root to 1371px inside a 1290px viewport. `body`
## then re-divided 1371 by stretch ratio, so the two emptiest regions on screen GAINED width (roster
## +24, chronicle +33) and the deliverable — the Dawn Summary — LOST exactly their sum (−57) purely
## because it was the rightmost child. A debug readout resized the application and starved its
## primary output, and column order picked the victim.
##
## Everything below follows from one rule: things that must never resize with data get a FIXED rail;
## the deliverable absorbs the slack. Two rails are constants, the reading pane is
## SIZE_EXPAND_FILL, and the pane's width is therefore DERIVED (viewport − rails − gutters), never
## declared — so this arithmetic is correct at 1290 and at 1280 without a second number to keep in
## sync. A min-width shock anywhere is now absorbed by the one region built to absorb it.
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

	# Both rails are the SUM OF THEIR CONTENTS' WORST CASES, measured against the running app — not
	# chosen and then defended. And the left one carries a finding the two studies could not reach
	# separately, because it only appears when you try to satisfy both at once:
	#
	#   Study B measured the roster 45.9% empty in every state and ~132px OVER-allocated, and sized a
	#   320px rail from its ~274px demand. Study A independently showed that same roster is 29
	#   undocumented glyphs a stranger cannot read, and prescribed words — costing its fix at "four
	#   set_text lines, contained".
	#
	#   Both are right, and they are incompatible. The roster's ~274px demand was an ARTIFACT OF THE
	#   GLYPH ENCODING. Rendered in words, its honest demand is ~464px (measured, not estimated: the
	#   widest value each column can hold) — MORE than the 406px Study B called over-allocation. The
	#   horizontal surplus was never real; it was the illegibility, priced in pixels. Study A's fix
	#   is not four lines and does not cost nothing: it costs ~156px of rail, and the only account it
	#   can be drawn from is the reading pane.
	#
	# Paid, deliberately. A wider summary next to a roster nobody can read fails the same publication
	# test as a narrow one — and the summary still goes 326 (clipped mid-word) → ~500 (complete).
	# The right rail funds part of it: the knobs and inspector demand ~270, not the 320 they held.
	#
	# The reading pane is what remains, and that is the entire point: it is the only region whose
	# width is a RESULT rather than a decision.
	body.add_child(_rail(488, func(v): _build_left(v)))
	body.add_child(_fluid(func(v): _build_center(v)))
	body.add_child(_rail(290, func(v): _build_right(v)))

	root.add_child(_build_status_strip())

func _build_top_bar() -> Control:
	var panel := PanelContainer.new()
	var bar := HBoxContainer.new()
	bar.add_theme_constant_override("separation", 10)
	panel.add_child(bar)

	# The clock is a fixed-format readout that was rendered proportionally, so it re-flowed the bar
	# on every tick ("11:11" is narrower than "00:00") and would jump again at "Day 10". Mono +
	# a reserved slot sized for the format's worst case ("Day 999 · 23:30 (slot 47)") means the
	# buttons to its right never move for any day, slot or seed.
	clock_label = _readout("clock", 16, 226)
	bar.add_child(clock_label)
	bar.add_child(_sep())

	bar.add_child(_btn("Step", _on_step, "btn-step"))
	bar.add_child(_btn("Run to Dawn", _on_dawn, "btn-dawn"))
	bar.add_child(_btn("Run 3 Days", func(): _run_days(3), "btn-run3"))

	bar.add_child(_sep())
	# "1123" used to appear three times in three roles with nothing distinguishing them: the seed you
	# ARE running, this pending value, and the generator's field. The readout moved to the status
	# strip; this one says what it does.
	bar.add_child(_mk_label("reseed to", 12))
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

## hash · seed · stats — every one a fixed-format machine readout, none of them the deliverable, and
## all three previously wedged into width-negotiating containers where their TEXT was a layout
## demand. Here they are full-width, monospaced and clipped, so no value they can ever hold reaches
## the layout. `stats` joins them because it is a debug readout: it belongs beside hash/seed, not
## above the inspector in the column that holds the summary.
func _build_status_strip() -> Control:
	var panel := PanelContainer.new()
	var bar := HBoxContainer.new()
	bar.add_theme_constant_override("separation", 16)
	panel.add_child(bar)

	hash_label = _readout("hash", 13, 158)
	seed_label = _readout("seed", 13, 90)
	stats_label = _readout("stats", 13, 1)
	# stats takes whatever the strip has left, so it cannot clip while the strip has room and cannot
	# demand width when it doesn't. The one genuinely variable-length string on the strip is the one
	# that is free to grow into the slack.
	stats_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	bar.add_child(hash_label)
	bar.add_child(_sep())
	bar.add_child(seed_label)
	bar.add_child(_sep())
	bar.add_child(stats_label)
	return panel

## A fixed rail: exactly `width` px, forever. No expand flag, so the HBox hands it its minimum, and
## custom_minimum_size IS that minimum.
func _rail(width: int, builder: Callable) -> Control:
	var v := VBoxContainer.new()
	v.custom_minimum_size = Vector2(width, 0)
	builder.call(v)
	return v

## The one negotiable region — it gets the viewport minus the rails minus the gutters, whatever that
## is, and it is the region holding the thing the app is FOR.
func _fluid(builder: Callable) -> Control:
	var v := VBoxContainer.new()
	v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	builder.call(v)
	return v

## The "now" rail: who exists and where they are. Both tabular, both bounded, neither the
## deliverable — so they share the fixed rail and the reading pane keeps the slack.
func _build_left(v: VBoxContainer) -> void:
	_build_roster(v)
	_build_places(v)

func _build_roster(v: VBoxContainer) -> void:
	v.add_child(_header("Roster"))
	roster_tree = Tree.new()
	roster_tree.set_meta("test_id", "roster")
	roster_tree.columns = 5
	roster_tree.hide_root = true
	roster_tree.column_titles_visible = true
	# 12 at font 12: this is a tabular now-readout, not the deliverable. It used to inherit Godot's
	# unstyled 16 — larger than the Dawn Summary's 12 — which told the reader the raw table mattered
	# more than the curated output. It doesn't.
	roster_tree.add_theme_font_size_override("font_size", 12)
	roster_tree.set_column_title(0, "Name")
	roster_tree.set_column_title(1, "Role")
	# "Place now" → "Place": a Tree column can never be narrower than its own title, so the longest
	# title was taxing the narrowest column and starving Name. "now" was redundant anyway — the whole
	# table is a now-readout, and the clock sits directly above it.
	roster_tree.set_column_title(2, "Place")
	roster_tree.set_column_title(3, "Doing")
	roster_tree.set_column_title(4, "Top drive")
	# Minimums are the widest value each column CAN hold, and every one of these was MEASURED off the
	# running app, because my first estimates were all ~20% low and every column silently ellipsised.
	# Sizing column 4 for "purse 0.50" is the hash's own error one scope down — the widest drive is
	# "restlessness 0.45", 17 characters, and the format is what has to fit, not the sample:
	#   Name  "Brindle A." / "Widow Karsk" (honorifics survive _short_name whole)
	#   Role  "market warden" (widest of the 11)
	#   Place "the Millet House" (widest after _short_place drops the comma tail)
	#   Doing one glyph, floored by its own title — a Tree column can never be narrower than that
	#   Top   "restlessness 0.45" (the widest DRIVE, not the widest sample)
	# Ratios only divide the LEFTOVER after minimums, so the floor is the only thing that actually
	# guarantees a column renders; the leftover goes to Name.
	roster_tree.set_column_expand_ratio(0, 3)
	roster_tree.set_column_expand_ratio(1, 1)
	roster_tree.set_column_expand_ratio(2, 1)
	roster_tree.set_column_expand_ratio(3, 0)
	roster_tree.set_column_expand_ratio(4, 0)
	roster_tree.set_column_custom_minimum_width(0, 92)
	roster_tree.set_column_custom_minimum_width(1, 95)
	roster_tree.set_column_custom_minimum_width(2, 115)
	roster_tree.set_column_custom_minimum_width(3, 60)
	roster_tree.set_column_custom_minimum_width(4, 118)
	# Tall enough for all 12 (VFB.D4 fixes the cast size) plus the header, so the v-scrollbar never
	# appears and never eats 12px off the widest column. The roster's height was the one dimension it
	# genuinely had to spare; spending a little of it to buy horizontal room is the trade this whole
	# rail is making.
	roster_tree.custom_minimum_size = Vector2(0, 360)
	roster_tree.size_flags_vertical = Control.SIZE_EXPAND_FILL
	roster_tree.item_selected.connect(_on_roster_selected)
	v.add_child(roster_tree)

	# The legend for the one glyph column. On screen, not in a tooltip: these screenshots are
	# published, and a PNG has no hover.
	var legend := _mk_label(DOING_LEGEND, 11)
	legend.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	legend.modulate = Color(0.62, 0.72, 0.8)
	v.add_child(legend)

## The place board, as a list rather than a card grid. The grid sized each column to its widest
## card's MINIMUM and the cards carried no expand flag, so the two columns were literally the pixel
## widths of the two longest place names — 298px of cards in a 501px viewport, 203px of it
## unreachable — while 6 cards × 92px of content was clipped to a 210px scroll viewport, so the
## bottom row ("the Long Table", "the Millpond") showed its title bar and nothing else in EVERY shot
## of the corpus. It wasted width and hid height at the same time, in one widget. A list in the rail
## is smaller AND complete.
func _build_places(v: VBoxContainer) -> void:
	v.add_child(_header("Place board"))
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	# Autowrapped children need the horizontal axis pinned, or the ScrollContainer offers them
	# unbounded width and they never wrap.
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	places_box = VBoxContainer.new()
	places_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(places_box)
	v.add_child(scroll)

## The reading pane: the summary the app exists to produce, and the chronicle it was drawn from.
## Both are prose, both were truncated, and this is the region that now holds the slack.
func _build_center(v: VBoxContainer) -> void:
	summary_header = _header("Dawn summary")
	v.add_child(summary_header)
	register_label = _mk_label("", 13)
	register_label.set_meta("test_id", "register")
	v.add_child(register_label)
	summary_box = VBoxContainer.new()
	summary_box.set_meta("test_id", "summary")
	v.add_child(summary_box)

	chronicle_header = _header("Chronicle")
	v.add_child(chronicle_header)
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	# The handle goes on the SCROLL VIEWPORT, not the content VBox inside it. The chronicle used to
	# be a Tree, which scrolls internally, so `chronicle` meant "the panel" and read on_screen:true.
	# Tagging the inner VBox instead would silently redefine the handle to mean "the content's
	# extent" — which is taller than its viewport by design, so it reports clipped:[bottom],
	# on_screen:false, visible_fraction:0.69 as its NORMAL state. That is a harness handle that cries
	# wolf forever, and FISHBOWL.md tabulates this name.
	scroll.set_meta("test_id", "chronicle")
	chronicle_box = VBoxContainer.new()
	chronicle_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(chronicle_box)
	v.add_child(scroll)

func _build_right(v: VBoxContainer) -> void:
	v.add_child(_build_knobs())
	v.add_child(_header("Inspector"))
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	inspector_box = VBoxContainer.new()
	inspector_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(inspector_box)
	v.add_child(scroll)

func _build_knobs() -> Control:
	var box := VBoxContainer.new()
	box.add_child(_header("Debug knobs"))
	# Two kinds of knob, two semantics, and the split is the whole point: a RENDERING knob
	# re-presents an already-simulated day, so it applies to what you are looking at right now.
	# A SIMULATION knob changes what happens, so it cannot apply retroactively without re-running
	# the day — which would move the day-hash. Grouped because "actionability responds instantly
	# while storylet_rate does nothing until dawn" is more confusing than uniform deadness unless
	# the UI says which is which.
	#
	# Display names are separate from knob KEYS. The key is the bridge/simconfig identifier and the
	# GTH test_id (`knob-<key>`) and neither may drift; the label is for the reader. Two earned a
	# rename: `actionability` is the register dial — it is why the line above the summary says
	# "gossip" — and shared no morpheme with the word it changes; `pressure_rates.trade` is a dotted
	# path implying siblings (`.purse`, `.heart`) that no slider offers, so it read as a truncated UI
	# rather than a deliberate choice.
	box.add_child(_header("Rendering — applies now"))
	box.add_child(_slider("actionability", "register (actionability)", 0.0, 1.0, 0.01, 0.5, func(x): _knob("actionability", x)))
	box.add_child(_slider("summary_lines", "summary lines", 3, 7, 1, 5, func(x): _knob("summary_lines", x)))
	box.add_child(_check("hearsay_required", "hearsay required", true, func(on): _knob("hearsay_required", 1.0 if on else 0.0)))
	box.add_child(_header("Simulation — applies next dawn"))
	box.add_child(_slider("storylet_rate", "storylet rate", 0.0, 3.0, 0.05, 1.0, func(x): _knob("storylet_rate", x)))
	box.add_child(_slider("pressure_rates.trade", "trade drift rate", 0.0, 3.0, 0.05, 1.0, func(x): _knob("pressure_rates.trade", x)))
	# Not a display toggle, despite reading like one next to hearsay_required: this writes hashed
	# bio Marks at storylet-fire time, so it affects the day-hash and must stay next-dawn.
	box.add_child(_check("bio_marks_enabled", "bio marks enabled", true, func(on): _knob("bio_marks_enabled", 1.0 if on else 0.0)))
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

## True when the panels driven by `view_day` are showing the day the clock is showing. After any
## dawn they are NOT: `view_day` becomes yesterday and stays there (nothing in the UI moves it), so
## the same panels silently change meaning at the dawn boundary with no visual cue. That flip is
## what made an expert reading 35 captures conclude "the chronicle resets each day" — it never
## resets; it was showing yesterday. Every panel that reads `view_day` now says which day it is and
## whether that day is still running.
func _viewing_live() -> bool:
	return view_day >= int(bridge.CurrentDay())

func _view_tense() -> String:
	return "today, live" if _viewing_live() else "yesterday"

func _refresh_roster() -> void:
	roster_tree.clear()
	var root := roster_tree.create_item()
	var data = _j(bridge.GetRoster())
	for t in data.townees:
		var it := roster_tree.create_item(root)

		# Name — given name + surname initial; tooltip restores the full name.
		it.set_text(0, _short_name(t.name))
		it.set_tooltip_text(0, t.name)

		# Role — the word. Role never changes during a run, so there is nothing to scan for and 11
		# arbitrary symbols bought nothing; and its glyphs collided with the Place column's twice
		# over (the innkeep at the inn read `🍺 | 🍺`, the smith at the workshop `🔨 | 🔨`).
		it.set_text(1, t.role)
		it.set_tooltip_text(1, t.role)

		# Place — the NAME, which is the interesting fact and was already computed for the tooltip.
		# The glyph was keyed off `place_kind`, so three different answers to "where is she?" lived
		# in one cell: the header said Place, the glyph said kind, the tooltip said name. At slot 0
		# the inn's 🍺 rendered for three people asleep in their own beds — the adventurers lodge at
		# the inn and Odile keeps it — which reads as three people out drinking at midnight. Not a
		# degenerate column: a column that was wrong in a way that read as right.
		if t.away:
			it.set_text(2, "away")
			it.set_tooltip_text(2, "away")
		else:
			it.set_text(2, _short_place(t.place_name))
			it.set_tooltip_text(2, "%s (%s)" % [t.place_name, t.place_kind])

		# Doing — the one surviving glyph column. Tooltip carries the authored activity prose
		# ("tending the room"), which is richer than the mode word it was keyed from.
		it.set_text(3, MODE_GLYPH_ASLEEP if t.asleep else MODE_GLYPH.get(t.mode, GLYPH_UNKNOWN))
		it.set_tooltip_text(3, t.activity)

		# Top drive — the cell ALREADY carried text ("0.50"), so the glyph saved ~30px and cost the
		# whole meaning of the number beside it. The worst trade in the table.
		it.set_text(4, "%s %.2f" % [t.top_drive, t.top_value])
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

## "Bray & Daughter, Farriers" → "Bray & Daughter"  ·  "the Long Table" → "the Long Table"
##
## The roster's Place column is a glance-readout in a fixed rail, so its content is bounded HERE, by
## construction, rather than left to be ellipsised by whatever width survives a layout negotiation —
## the same trick `_short_name` already plays on people. A trailing qualifier after a comma is the
## droppable part of every place name in the town: the head identifies, the tail elaborates. The
## board and the tooltip still carry the full name, so nothing is destroyed.
func _short_place(full: String) -> String:
	var head := full.split(",", false)[0] if full.find(",") != -1 else full
	return head.strip_edges()

func _refresh_places() -> void:
	for c in places_box.get_children():
		c.queue_free()
	var data = _j(bridge.GetPlaces())
	for p in data.places:
		if not p.board:
			continue
		var row := VBoxContainer.new()
		var title = p.name + (" (shut)" if p.shut else "")
		var t := _mk_label(title, 12)
		t.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(t)
		var who := ""
		for o in p.occupants:
			who += "• %s  " % o.name
		var w := _mk_label(who.strip_edges() if who != "" else "—", 11)
		w.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		w.modulate = Color(0.72, 0.78, 0.84)
		row.add_child(w)
		places_box.add_child(row)

## The chronicle was a `Tree`, and a Tree cannot wrap — it ellipsises. So every entry was cut
## mid-sentence: the day-1 farewell renders 135 characters and the widest state showed 65 of them,
## hiding 52% of the line. Widening cannot fix that (one line needs ~1124px; the column had 542) —
## only wrapping can. Meanwhile the tree itself sat 27–100% empty, so the app hid half its prose
## inside its largest void. Same class as the roster, opposite conclusion: Tree is right for the
## tabular glyph readout and wrong for prose.
##
## The trade, stated out loud: ~8–10 complete entries with a scrollbar, instead of 12 truncated
## ones. That is the right way round for a panel whose entries are sentences.
func _refresh_chronicle() -> void:
	for c in chronicle_box.get_children():
		c.queue_free()
	chronicle_header.text = "CHRONICLE — DAY %d (%s) · CLICK A ROW FOR WHY IT FIRED" % [view_day, _view_tense()]
	var data = _j(bridge.GetChronicle(view_day))
	var idx := 0
	for e in data.events:
		# The first entry opens by default. The because-list is the single best explanatory artifact
		# in the app — predicates on the left, all three register renderings side by side on the
		# right — and it teaches the whole model at a glance. It was hidden behind one character on
		# every row, so the app booted showing the LEAST useful projection of its best feature, in
		# the most space. One open row costs ~279px of a void that had 519 and puts the model in
		# front of the reader without telling them to go looking.
		chronicle_box.add_child(_chronicle_entry(e, idx == 0))
		idx += 1

func _chronicle_entry(e: Variant, start_open: bool) -> Control:
	var box := VBoxContainer.new()

	var row := HBoxContainer.new()
	var tog := Button.new()
	tog.toggle_mode = true
	tog.button_pressed = start_open
	tog.text = "▾" if start_open else "▸"
	tog.custom_minimum_size = Vector2(26, 0)
	tog.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	row.add_child(tog)
	var lbl := _mk_label("[%s] %s" % [e.clock, e.gossip], 12)
	lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(lbl)
	box.add_child(row)

	var detail := MarginContainer.new()
	detail.add_theme_constant_override("margin_left", 26)
	detail.add_theme_constant_override("margin_bottom", 6)
	detail.visible = start_open
	var dv := VBoxContainer.new()
	detail.add_child(dv)

	dv.add_child(_header("because —"))
	for b in e.because:
		dv.add_child(_wrapped("%s: %s" % [b.label, b.value], 11))
	dv.add_child(_header("reads —"))
	for key in ["hearsay", "gossip", "report"]:
		dv.add_child(_wrapped("%s: %s" % [key, e[key]], 11))
	box.add_child(detail)

	tog.toggled.connect(func(on):
		detail.visible = on
		tog.text = "▾" if on else "▸")
	return box

func _refresh_summary() -> void:
	for c in summary_box.get_children():
		c.queue_free()
	var s = _j(bridge.GetSummary(view_day))
	# "Day N" was ambiguous next to a clock reading a DIFFERENT N. "Summary OF Day N" is a claim
	# about which day the lines below describe, not about what time it is now.
	summary_header.text = "DAWN SUMMARY — DAY %d (%s)" % [view_day, _view_tense()]
	register_label.text = "Summary of Day %d · register: %s" % [view_day, s.register]
	for line in s.lines:
		# Without autowrap a Label's minimum width is its whole single-line text, so one long
		# summary line (~670px) became the Read column's minimum and the body HBox's stretch
		# ratios could only divide what was left. Autowrap keeps that demand at ~1; the fixed rails
		# above are what stop anything ELSE in the tree doing the same thing to this column.
		#
		# 15px, and this is now the largest body text in the app. It is the deliverable — the thing
		# VFB.Q1 asks a question about — and it used to render at 12 while two unstyled debug
		# checkboxes rendered at Godot's default 16 beside it. Visual weight is a reader's first and
		# cheapest signal about what matters, and it was pointing at the debug toggles.
		var lbl := _mk_label("• " + line.text, 15)
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		summary_box.add_child(lbl)

func _refresh_stats() -> void:
	var s = _j(bridge.GetStats(view_day))
	var warn := "  ⚠ starvation" if s.starvation else ""
	stats_label.text = "events %d · tellable %d / pool %d%s" % [s.events, s.tellable, s.pool, warn]

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
	inspector_box.add_child(_wrapped("traits: " + ", ".join(t.traits), 11))
	if t.marks.size() > 0:
		inspector_box.add_child(_header("marks (bio)"))
		for m in t.marks:
			# `m.line` is AUTHORED PROSE of unbounded length. Unwrapped it was a min-width demand
			# waiting to happen — the same defect as the hash, shielded only by the ScrollContainer
			# it happens to sit in.
			inspector_box.add_child(_wrapped("Day %d: %s" % [m.day, m.line], 11))

	inspector_box.add_child(_header("pressures (trailing 3 days)"))
	for d in DRIVES:
		var row := HBoxContainer.new()
		# Four series that exist to be COMPARED did not share a left edge: purse/trade/heart aligned
		# only by the coincidence that all three are five-letter words (and not even exactly — the
		# value renders proportionally too, so purse sat 2px off), while `restlessness` started 34px
		# right. The boxes were always all exactly 120px, so the x-scale was already shared and the
		# slopes always were comparable — the defect was purely this label column having no width.
		row.add_child(_label_col("%s %.2f" % [d, float(t.pressures[d])], 11, 100))
		var spark := Sparkline.new()
		spark.custom_minimum_size = Vector2(120, 26)
		var series = _j(bridge.GetPressureSeries(selected_id, d))
		spark.set_values(series.values)
		row.add_child(spark)
		inspector_box.add_child(row)

	# An empty header is a claim that the section exists and is blank. Under a gossip-carrier's bio
	# ("she hears everything and repeats most of it"), two empty REGARD headers read as "the
	# relationship model doesn't exist" rather than "no regard recorded yet".
	inspector_box.add_child(_header("regard — outgoing"))
	if t.regard_out.size() == 0:
		inspector_box.add_child(_mk_label("none yet", 11))
	for r in t.regard_out:
		inspector_box.add_child(_wrapped("→ %s  %.2f  [%s]" % [r.name, float(r.score), ", ".join(r.tags)], 11))
	inspector_box.add_child(_header("regard — incoming"))
	if t.regard_in.size() == 0:
		inspector_box.add_child(_mk_label("none yet", 11))
	for r in t.regard_in:
		inspector_box.add_child(_wrapped("← %s  %.2f  [%s]" % [r.name, float(r.score), ", ".join(r.tags)], 11))

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
	# This button sits in the same row, in the same styling, as three additive "New …" buttons, and
	# an AcceptDialog gives it an affirmative OK with no Cancel — so the destructive path wore the
	# friendly button and the safe path was an unlabelled X. The title is the only place left to say
	# what OK actually does.
	dlg.title = "Generate a NEW town (REPLACES the current cast)"
	var box := VBoxContainer.new()
	box.add_child(_wrapped("Discards every townee, place and relationship in the current town and "
		+ "builds a fresh one from this seed. The chronicle so far is lost.", 11))
	var seed_sp := _spin_row(box, "seed", 0, 999999, 1123)
	var count := _spin_row(box, "townees", 4, 40, 12)
	var density := _spin_row(box, "relationship density %", 0, 100, 15)
	dlg.add_child(box)
	add_child(dlg)
	dlg.confirmed.connect(func():
		var cfg := {"seed": int(seed_sp.value), "count": int(count.value), "relationship_density": density.value / 100.0}
		var res = _j(bridge.GenerateTown(JSON.stringify(cfg)))
		_toast(res)
		selected_id = ""
		_refresh_all())
	dlg.popup_centered(Vector2i(420, 260))

func _open_townee_creator() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "New townee"
	var box := VBoxContainer.new()
	var id_e := _text_row(box, "id", "new-townee")
	var name_e := _text_row(box, "name", "New Townee")
	var role_e := _option_row(box, "role", ROLES)
	var places = _j(bridge.GetPlaces()).places
	var place_names := []
	var place_ids := []
	for p in places:
		place_names.append(p.name)
		place_ids.append(p.id)
	var home := _option_row(box, "home", place_names, place_ids)
	var dayplan_e := _text_row(box, "dayplan", "fisher-default")
	dlg.add_child(box)
	add_child(dlg)
	dlg.confirmed.connect(func():
		var dto := {
			"id": id_e.text, "name": name_e.text,
			"role": role_e.get_item_metadata(role_e.selected), "adventurer": false,
			"traits": ["patient"], "dayplan": dayplan_e.text,
			"home": home.get_item_metadata(home.selected), "work": home.get_item_metadata(home.selected),
			"haunts": [], "pressures": {"purse": 0.4, "trade": 0.4, "heart": 0.5, "restlessness": 0.3},
			"regard": {}, "teller_regard": 0.5, "bio": name_e.text + " just arrived in the village.",
		}
		_toast(_j(bridge.CreateTownee(JSON.stringify(dto))))
		_refresh_all())
	dlg.popup_centered(Vector2i(420, 300))

func _open_place_creator() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "New place"
	var box := VBoxContainer.new()
	var id_e := _text_row(box, "id", "new-place")
	var name_e := _text_row(box, "name", "New Place")
	var kind_e := _option_row(box, "kind", PLACE_KINDS)
	var cap := _spin_row(box, "capacity", 1, 40, 8)
	# `board` is hardcoded true and there is no field for it or for `shut`, so a place made here is
	# ALWAYS boarded and can never be un-boarded — the sweep read the missing field as "cannot be put
	# on the board", which is the inverse of what the code does. Saying so is a one-liner; adding the
	# fields is a data-contract change and belongs with whoever owns CreatePlace.
	box.add_child(_wrapped("Always created on the board, open 07:00–17:00.", 11))
	dlg.add_child(box)
	add_child(dlg)
	dlg.confirmed.connect(func():
		var dto := {
			"id": id_e.text, "name": name_e.text,
			"kind": kind_e.get_item_metadata(kind_e.selected),
			"hours": {"open": 14, "close": 34}, "capacity": int(cap.value), "board": true,
		}
		_toast(_j(bridge.CreatePlace(JSON.stringify(dto))))
		_refresh_all())
	dlg.popup_centered(Vector2i(420, 300))

func _open_storylets() -> void:
	var dlg := AcceptDialog.new()
	dlg.title = "Storylet browser — the town's 12 story rules (force-fire is debug)"
	var box := VBoxContainer.new()
	# This list is the town's design doc: it says, in one screen, that the place runs on twelve named
	# rules with weights and cooldowns — which nothing else in the app says. `w0.6 cd1 ★` said it in
	# a code that needed the source to decode.
	box.add_child(_wrapped("Each rule fires when its predicates match. Weight biases the draw; "
		+ "cooldown is the days it must wait before firing again; ★ = must-fire (exempt from the "
		+ "awake gate).", 11))
	for s in _j(bridge.GetStorylets()).storylets:
		var row := HBoxContainer.new()
		var must := "  ★ must-fire" if s.must_fire else ""
		row.add_child(_label_col(s.id, 12, 190))
		row.add_child(_label_col("weight %.1f · cooldown %dd%s" % [float(s.weight), int(s.cooldown), must], 11, 170))
		var sid: String = s.id
		row.add_child(_btn("fire", func(): _toast(_j(bridge.InjectStorylet(sid, "[]"))); _refresh_all()))
		box.add_child(row)
	dlg.add_child(box)
	add_child(dlg)
	dlg.popup_centered(Vector2i(560, 500))

func _toast(res: Variant) -> void:
	if typeof(res) == TYPE_DICTIONARY and res.has("error"):
		push_warning("bridge error: " + str(res.error))

# --- tiny widget helpers -------------------------------------------------------------------

func _mk_label(text: String, font_size: int) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", font_size)
	return l

## A label that cannot be a layout demand: autowrap drops a Label's minimum width to ~1.
func _wrapped(text: String, font_size: int) -> Label:
	var l := _mk_label(text, font_size)
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return l

## A label with a FIXED width, for putting things in a column. One helper, because "the label has no
## width so the control after it starts wherever the text ended" was one bug at five sites: the
## sparkline rows, and every row of the townee / place / generate / storylet dialogs (12 `fire`
## buttons at 12 different x). The codebase already knew the answer — `_slider` sets exactly this on
## its label, which is the only reason the knob rows line up.
func _label_col(text: String, font_size: int, width: int) -> Label:
	var l := _mk_label(text, font_size)
	l.custom_minimum_size = Vector2(width, 0)
	return l

## The monospace face for machine readouts. Resolved once, from the system, with a fallback chain.
func _mono() -> SystemFont:
	if _mono_font == null:
		_mono_font = SystemFont.new()
		_mono_font.font_names = PackedStringArray(
			["Consolas", "DejaVu Sans Mono", "Liberation Mono", "Courier New", "monospace"])
	return _mono_font

## A fixed-format machine readout: monospace, clipped, and given a reserved slot.
##
## Both halves are load-bearing, and they fix DIFFERENT things:
##
##  - `clip_text` is the structural fix. It drops the Label's minimum width to ~1, so its TEXT can
##    never again propagate a demand up the tree. This is what makes the class of bug impossible
##    rather than merely unlikely — it holds for any font, any string, any future edit.
##  - monospace is the appearance fix. The hash is a fixed-LENGTH hex string rendered in a
##    PROPORTIONAL font, so its width was a sample, not a constant: three observed hashes rendered
##    at ~152/155/156px, which is why four documents in this repo record four different rects for
##    the same button. In mono, 16 hex digits have one width for every seed, every day, forever.
##
## `min_w` is chosen from the FORMAT's worst case, never measured from today's data — a fix tuned to
## a number the data can move is fiction.
func _readout(tid: String, font_size: int, min_w: int) -> Label:
	var l := Label.new()
	l.add_theme_font_override("font", _mono())
	l.add_theme_font_size_override("font_size", font_size)
	l.clip_text = true
	l.custom_minimum_size = Vector2(min_w, 0)
	l.set_meta("test_id", tid)
	return l

## 13 and undimmed. These name every panel on screen and were the smallest, lowest-contrast text in
## the app — 11px at 60% luminance, below the debug checkboxes they sat above.
func _header(text: String) -> Label:
	var l := _mk_label(text.to_upper(), 13)
	l.modulate = Color(0.74, 0.85, 0.95)
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

## `knob` is the bridge/simconfig KEY and the GTH handle; `display` is what the reader sees. An
## integer knob renders as an integer — `summary_lines 5.00` implied a precision the knob does not
## have (its step is 1).
func _slider(knob: String, display: String, lo: float, hi: float, step: float, val: float, cb: Callable) -> Control:
	var row := HBoxContainer.new()
	var fmt := "%s %d" if step >= 1.0 else "%s %.2f"
	var lbl := _label_col(fmt % [display, val], 11, 140)
	row.add_child(lbl)
	var sl := HSlider.new()
	sl.set_meta("test_id", "knob-" + knob)  # GTH: the VFB.Q1 tuning surface needs a stable handle
	sl.min_value = lo
	sl.max_value = hi
	sl.step = step
	sl.value = val
	sl.custom_minimum_size = Vector2(120, 0)
	sl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	sl.value_changed.connect(func(x):
		lbl.text = fmt % [display, x]
		cb.call(x))
	row.add_child(sl)
	return row

## 11px, matching the knob labels it sits among. These are two debug toggles; unstyled, they kept
## Godot's default 16 and rendered 33% LARGER than the Dawn Summary — the loudest thing in the
## column that holds the app's deliverable.
func _check(knob: String, display: String, on: bool, cb: Callable) -> Control:
	var c := CheckBox.new()
	c.set_meta("test_id", "knob-" + knob)  # GTH: stable handle (see _slider)
	c.text = display
	c.add_theme_font_size_override("font_size", 11)
	c.button_pressed = on
	c.toggled.connect(cb)
	return c

const DLG_LABEL_W := 120

func _spin_row(box: VBoxContainer, label: String, lo: float, hi: float, val: float) -> SpinBox:
	var row := HBoxContainer.new()
	row.add_child(_label_col(label, 12, DLG_LABEL_W))
	var sp := SpinBox.new()
	sp.min_value = lo
	sp.max_value = hi
	sp.value = val
	row.add_child(sp)
	box.add_child(row)
	return sp

func _text_row(box: VBoxContainer, label: String, val: String) -> LineEdit:
	var row := HBoxContainer.new()
	row.add_child(_label_col(label, 12, DLG_LABEL_W))
	var e := LineEdit.new()
	e.text = val
	e.custom_minimum_size = Vector2(200, 0)
	row.add_child(e)
	box.add_child(row)
	return e

## Options carry a metadata id distinct from their display text. Used for `home` (places) and now
## for `role` / `kind`, which were free text over closed vocabularies: a typo'd role minted a townee
## no table could render, and the dialog already used a dropdown for the ONE field where a wrong
## value was recoverable. As a bonus the dropdown IS the vocabulary — it lists the valid roles and
## place kinds, on screen, which is a legend the app never had.
func _option_row(box: VBoxContainer, label: String, items: Array, ids: Array = []) -> OptionButton:
	var row := HBoxContainer.new()
	row.add_child(_label_col(label, 12, DLG_LABEL_W))
	var opt := OptionButton.new()
	for i in items.size():
		opt.add_item(str(items[i]))
		opt.set_item_metadata(opt.item_count - 1, ids[i] if i < ids.size() else items[i])
	row.add_child(opt)
	box.add_child(row)
	return opt

extends Node
## Deck (autoload) — loads and validates the shift's data.
##
## Owns data/visitors.json (the curated day-0 queue) and data/references.json (the rulebook
## the desk checks against), PLUS the week-of-shifts banks: data/townees.json (the Townee
## Directory), data/adventurers.json (the Adventurer Directory) and data/generation.json
## (the procedural-generator config). Parses them at startup, injects the two directories
## into `references` as ordinary lookup tabs, does a light schema sanity pass, and exposes
## everything read-only to every scene. This is a stable-interface file: scenes and
## sub-agents depend on these signatures — extend, do not rename.
##
## SHIFT SELECTION (CONTENT-BANKS.md §5): `day == 0` loads the curated tutorial shift from
## visitors.json unchanged; `day > 0` composes the shift procedurally with
## `ShiftGenerator.generate_shift(day)` (deterministic — seed = day), so a "week" is seven
## reproducible days. Flip `day` (or call `load_day`) to switch; a later hub increments it.
##
## Contract (what other scripts may call):
##   Deck.visitors            -> Array[Dictionary]   the queue, in `order`
##   Deck.references          -> Dictionary          the rulebook truth tables (+ the two
##                                                    injected directory tables)
##   Deck.townees             -> Dictionary          townee id -> record (generator pool)
##   Deck.adventurers         -> Dictionary          adventurer id -> record (generator pool)
##   Deck.generation          -> Dictionary          generation.json config
##   Deck.day                 -> int                  the loaded shift's day (0 = curated)
##   Deck.count()             -> int                  number of visitors
##   Deck.get_visitor(i)      -> Dictionary          i-th visitor (0-based), or {}
##   Deck.load_day(d)         -> void                 reload the queue for day d (re-emits loaded)
##   Deck.pay_dues(id)        -> void                 mark a townee's dues as paid (runtime only)
##   Deck.ok                  -> bool                 true if the banks + shift loaded & sane
##   Deck.load_errors         -> Array[String]       human-readable problems, if any
## Signal:
##   loaded(ok: bool)         emitted once after the startup parse (and after load_day)

signal loaded(ok: bool)

const VISITORS_PATH := "res://data/visitors.json"
const REFERENCES_PATH := "res://data/references.json"
const TOWNEES_PATH := "res://data/townees.json"
const ADVENTURERS_PATH := "res://data/adventurers.json"
const GENERATION_PATH := "res://data/generation.json"

## The day the desk opens on. 0 = the curated tutorial shift (visitors.json); a value > 0
## generates a deterministic shift. A later shift-select hub sets this before the desk builds.
const START_DAY := 0

## Dev aid: at boot, generate + validate days 1..7 and report, WITHOUT disturbing the live
## shift. Proves the generator emits schema-valid shifts across a week. Off = no self-check.
const GEN_SELFCHECK := true

# Schema validation (banks + shift + inspections + standing-order limits) and the
# scale-verdict derive pass now live in MorningQueue.Core, reached through the C#
# CoreBridge (JSON text in, JSON text / string[] out; one Validate per boot, one
# PrepareShift per loaded day). The enum vocabularies TASK_TYPES / FAILURE_AXES /
# ACTOR_POOLS moved with the checks — they are Core's Validator constants now.

## The GDScript <-> .NET seam. Instantiated once at boot. Because CoreBridge is a C#
## [GlobalClass], the global-class cache must be regenerated once after it is added
## (open the editor, or `godot --headless --path . --import`) before this resolves.
var _bridge = null

var visitors: Array = []
var references: Dictionary = {}
var townees: Dictionary = {}
var adventurers: Dictionary = {}
var generation: Dictionary = {}
var day: int = START_DAY
var ok: bool = false
var load_errors: Array = []


func _ready() -> void:
	_bridge = CoreBridge.new()
	_load_all()
	loaded.emit(ok)
	if GEN_SELFCHECK and OS.is_debug_build():
		_selfcheck_generated()


## Lazily ensure the Core bridge exists (load_day may run before/without _ready in tests).
func _get_bridge():
	if _bridge == null:
		_bridge = CoreBridge.new()
	return _bridge


func count() -> int:
	return visitors.size()


func get_visitor(i: int) -> Dictionary:
	if i < 0 or i >= visitors.size():
		return {}
	return visitors[i]


## Reload the queue for a given day (0 = curated, >0 = generated) and re-emit `loaded`.
## The banks (references + directories + generation) are already loaded; only the shift
## changes. Used by a shift-select hub and by the generated-day verification.
func load_day(d: int) -> void:
	day = d
	load_errors.clear()
	_load_shift(day)
	_prepare_shift_core()
	ok = load_errors.is_empty()
	if not ok:
		for e in load_errors:
			push_error("[Deck] " + e)
	loaded.emit(ok)


## Mark a townee's dues as paid so the next generated shift treats them as current.
## Called by the floor interlude when the player accepts a townee's dues payment.
## Mutates the runtime `townees` dict only; the source file is unchanged.
func pay_dues(townee_id: String) -> void:
	if townees.has(townee_id):
		townees[townee_id]["dues"] = "current"


func _load_all() -> void:
	load_errors.clear()
	var rdata: Variant = _read_json(REFERENCES_PATH)
	var tdata: Variant = _read_json(TOWNEES_PATH)
	var adata: Variant = _read_json(ADVENTURERS_PATH)
	var gdata: Variant = _read_json(GENERATION_PATH)

	if rdata is Dictionary:
		references = rdata
	else:
		load_errors.append("references.json: not an object")

	# Directory banks: keep the inner id->record maps for the generator, and inject a
	# clean lookup table (just `_tab` + the id rows) into `references` so the ReferencePanel
	# renders each as an ordinary tab and `consult: "..._directory"` resolves like any table.
	townees = _inject_directory(tdata, "townee_directory", "townees")
	adventurers = _inject_directory(adata, "adventurer_directory", "adventurers")
	generation = gdata if gdata is Dictionary else {}
	if not (gdata is Dictionary):
		load_errors.append("generation.json: not an object")

	_run_banks_validation()
	_load_shift(day)
	_prepare_shift_core()

	ok = load_errors.is_empty()
	if not ok:
		for e in load_errors:
			push_error("[Deck] " + e)


## Build the injected directory table and return the inner id->record map. A malformed
## bank yields an empty map (guarded downstream) plus a load error.
func _inject_directory(src: Variant, table_key: String, inner_key: String) -> Dictionary:
	if not (src is Dictionary) or not (src as Dictionary).has(inner_key):
		load_errors.append("%s bank: missing '%s' map" % [table_key, inner_key])
		return {}
	var inner: Dictionary = (src as Dictionary)[inner_key]
	var table := { "_tab": str((src as Dictionary).get("_tab", table_key)) }
	for id in inner.keys():
		table[id] = inner[id]
	references[table_key] = table
	return inner


## Populate `visitors` for the given day: curated file for day 0, generated otherwise.
func _load_shift(d: int) -> void:
	if d == 0:
		var vdata: Variant = _read_json(VISITORS_PATH)
		if vdata is Dictionary and vdata.has("visitors"):
			visitors = (vdata as Dictionary)["visitors"]
			visitors.sort_custom(func(a, b): return int(a.get("order", 0)) < int(b.get("order", 0)))
		else:
			visitors = []
			load_errors.append("visitors.json: missing top-level 'visitors' array")
	else:
		visitors = ShiftGenerator.generate_shift(d)
		if visitors.is_empty():
			load_errors.append("generator produced no visits for day %d" % d)


# --- validation + derive (routed to MorningQueue.Core via CoreBridge) ---------

## Boot-time bank validation (dues enums, owns/chapter/archive_id resolution, generation
## knob domains, standing-order limits). Serializes the banks once and hands them to
## Core.Validator through the bridge; appends any problems to `load_errors`. This replaces
## the old GDScript bank + standing-order validators (now Core.Validator.ValidateBanks).
func _run_banks_validation() -> void:
	var payload := JSON.stringify({
		"references": references,
		"townees": townees,
		"adventurers": adventurers,
		"generation": generation,
	})
	var errs: PackedStringArray = _get_bridge().Validate(payload)
	for e in errs:
		load_errors.append(str(e))


## Per-day shift preparation: validate the loaded shift (required fields + inspection
## readings) AND derive each visit's inspections.scale.verdict against its claimed order —
## a single Core.PrepareShift call. The returned visitors carry the derived verdict; every
## authored field is preserved. This replaces the old GDScript shift + inspection validators
## and the scale-verdict rule formerly duplicated in ShiftGenerator / ReferencePanel.
func _prepare_shift_core() -> void:
	var refs_json := JSON.stringify(references)
	var vis_json := JSON.stringify({ "visitors": visitors })
	var out_text: String = _get_bridge().PrepareShift(refs_json, vis_json)
	var parsed: Variant = JSON.parse_string(out_text)
	if not (parsed is Dictionary):
		load_errors.append("core PrepareShift returned malformed result")
		return
	if (parsed as Dictionary).has("visitors"):
		visitors = (parsed as Dictionary)["visitors"]
	for e in (parsed as Dictionary).get("errors", []):
		load_errors.append(str(e))


## Dev self-check (CONTENT-BANKS §5.4): generate every day of the week, hold each visit to
## the same inspections + required-field contract as the curated shift, and report. Never
## mutates the live shift. Any failure is a real problem and prints as an error line.
func _selfcheck_generated() -> void:
	var required := ["id", "name", "affiliation", "profession", "task_type", "claim", "truth"]
	var total := 0
	var problems := 0
	for d in range(1, 8):
		var shift: Array = ShiftGenerator.generate_shift(d)
		if shift.is_empty():
			push_error("[gen-selfcheck] day %d produced no visits" % d)
			problems += 1
			continue
		for v in shift:
			total += 1
			for key in required:
				if not v.has(key):
					push_error("[gen-selfcheck] day %d visit '%s' missing '%s'" % [d, v.get("id", "?"), key])
					problems += 1
			var insp: Variant = v.get("inspections", null)
			if not (insp is Dictionary):
				push_error("[gen-selfcheck] day %d visit '%s' missing inspections" % [d, v.get("id", "?")])
				problems += 1
				continue
			for tool in ["glass", "scale"]:
				var t: Variant = insp.get(tool, null)
				var reading: Variant = t.get("reading", "") if t is Dictionary else ""
				if not (reading is String) or (reading as String).is_empty():
					push_error("[gen-selfcheck] day %d visit '%s' %s reading empty" % [d, v.get("id", "?"), tool])
					problems += 1
	print("[gen-selfcheck] 7 days, %d visits, %d problems" % [total, problems])


func _read_json(path: String) -> Variant:
	if not FileAccess.file_exists(path):
		load_errors.append("%s: file not found" % path)
		return null
	var text := FileAccess.get_file_as_string(path)
	var parsed: Variant = JSON.parse_string(text)
	if parsed == null:
		load_errors.append("%s: JSON parse failed" % path)
	return parsed

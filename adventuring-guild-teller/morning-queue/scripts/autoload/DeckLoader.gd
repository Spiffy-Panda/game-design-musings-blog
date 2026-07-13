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

const TASK_TYPES := ["item_check", "rank_gate", "quest_file", "completion_claim", "rank_up", "roster_change", "dungeon_drop"]
const FAILURE_AXES := ["identity", "rank", "unverifiable", "claimant", "authenticity", "paperwork", "duplicate", "fieldability", "season", "reach", "dues", "amount"]
const ACTOR_POOLS := ["townee_walkin", "townee_owner", "townee_directory", "adventurer_directory", "mixed"]

var visitors: Array = []
var references: Dictionary = {}
var townees: Dictionary = {}
var adventurers: Dictionary = {}
var generation: Dictionary = {}
var day: int = START_DAY
var ok: bool = false
var load_errors: Array = []


func _ready() -> void:
	_load_all()
	loaded.emit(ok)
	if GEN_SELFCHECK and OS.is_debug_build():
		_selfcheck_generated()


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
	_validate_shift()
	ok = load_errors.is_empty()
	if not ok:
		for e in load_errors:
			push_error("[Deck] " + e)
	loaded.emit(ok)


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

	_validate_banks()
	_load_shift(day)
	_validate_shift()

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


# --- validation --------------------------------------------------------------

## Light sanity pass over the loaded shift: every visitor has the fields scenes rely on and
## carries the two inspection-tool readings the Glass/Scale surface needs. Runs over whatever
## `visitors` currently holds (curated or generated), so a generated shift is held to the
## same contract as the curated one.
func _validate_shift() -> void:
	var required := ["id", "name", "affiliation", "profession", "task_type", "claim", "truth"]
	for v in visitors:
		for key in required:
			if not v.has(key):
				load_errors.append("visitor '%s' missing field '%s'" % [v.get("id", "?"), key])
		_validate_inspections(v)
	_validate_standing_orders()


## Every visitor must carry `inspections.glass.reading` and `inspections.scale.reading`
## as non-empty strings — the Glass/Scale tools have nothing to show otherwise.
func _validate_inspections(v: Dictionary) -> void:
	var vid: String = str(v.get("id", "?"))
	var insp: Variant = v.get("inspections", null)
	if not (insp is Dictionary):
		load_errors.append("visitor '%s' missing 'inspections' object" % vid)
		return
	for tool in ["glass", "scale"]:
		var t: Variant = (insp as Dictionary).get(tool, null)
		if not (t is Dictionary):
			load_errors.append("visitor '%s' inspections missing '%s'" % [vid, tool])
			continue
		var reading: Variant = (t as Dictionary).get("reading", "")
		if not (reading is String) or (reading as String).is_empty():
			load_errors.append("visitor '%s' %s reading is empty" % [vid, tool])


## Every posting of type standing_order must carry a limit the Scale measures against:
## an `accept` window ({min,max,unit}) or a `total` ({needed,unit}).
func _validate_standing_orders() -> void:
	var postings: Variant = references.get("postings", null)
	if not (postings is Dictionary):
		return
	for entry in (postings as Dictionary).keys():
		if entry is String and (entry as String).begins_with("_"):
			continue
		var posting: Variant = (postings as Dictionary)[entry]
		if not (posting is Dictionary):
			continue
		if str((posting as Dictionary).get("type", "")) != "standing_order":
			continue
		var has_accept: bool = (posting as Dictionary).has("accept")
		var has_total: bool = (posting as Dictionary).has("total")
		if not (has_accept or has_total):
			load_errors.append("standing_order '%s' has no accept/total limit" % str(entry))


## Validate the three new banks against the rulebook they cross-reference (CONTENT-BANKS §5):
## dues enums, `owns`/`chapter`/`archive_id` resolution, and the generation knob domains.
func _validate_banks() -> void:
	var postings: Dictionary = references.get("postings", {})
	var ciphers: Dictionary = references.get("cipher_table", {})
	var archive: Dictionary = references.get("archive", {})
	var rank_order: Array = references.get("rank_order", [])

	for tid in townees.keys():
		var t: Dictionary = townees[tid]
		if not (str(t.get("dues", "")) in ["current", "owing"]):
			load_errors.append("townee '%s' dues not current|owing" % tid)
		if not (t.get("owed", 0) is int or t.get("owed", 0) is float):
			load_errors.append("townee '%s' owed is not a number" % tid)
		for oid in t.get("owns", []):
			if not postings.has(oid):
				load_errors.append("townee '%s' owns unknown posting '%s'" % [tid, oid])

	for aid in adventurers.keys():
		var a: Dictionary = adventurers[aid]
		if not (str(a.get("rank", "")) in rank_order):
			load_errors.append("adventurer '%s' rank '%s' not in rank_order" % [aid, a.get("rank", "")])
		if not (str(a.get("dues", "")) in ["current", "owing"]):
			load_errors.append("adventurer '%s' dues not current|owing" % aid)
		if not ciphers.has(str(a.get("chapter", ""))):
			load_errors.append("adventurer '%s' chapter '%s' not in cipher_table" % [aid, a.get("chapter", "")])
		var lb: Variant = a.get("logbook", {})
		if lb is Dictionary and not archive.has(str((lb as Dictionary).get("archive_id", ""))):
			load_errors.append("adventurer '%s' logbook archive_id not in archive" % aid)

	if generation.is_empty():
		return
	for tk in generation.get("task_weights", {}).keys():
		if tk is String and (tk as String).begins_with("_"):
			continue
		if not (tk in TASK_TYPES):
			load_errors.append("generation task_weights has unknown task '%s'" % tk)
	var per_task: Dictionary = generation.get("per_task", {})
	for tk in per_task.keys():
		var spec: Dictionary = per_task[tk]
		if not (str(spec.get("actor_pool", "")) in ACTOR_POOLS):
			load_errors.append("generation per_task['%s'] actor_pool invalid" % tk)
		for ax in spec.get("failure_axes", []):
			if not (ax in FAILURE_AXES):
				load_errors.append("generation per_task['%s'] axis '%s' not in enum" % [tk, ax])
	var wheel: Array = generation.get("season_schedule", {}).get("wheel", [])
	for dk in generation.get("season_schedule", {}).get("by_day", {}).keys():
		if dk is String and (dk as String).begins_with("_"):
			continue
		var s: String = str(generation["season_schedule"]["by_day"][dk])
		if not (s in wheel):
			load_errors.append("generation season_schedule.by_day['%s'] = '%s' not in wheel" % [dk, s])
	_check_rate(generation.get("invalid_rate", 0.45), "invalid_rate")
	for dk in generation.get("invalid_rate_by_day", {}).keys():
		if dk is String and (dk as String).begins_with("_"):
			continue
		_check_rate(generation["invalid_rate_by_day"][dk], "invalid_rate_by_day['%s']" % dk)


func _check_rate(v: Variant, label: String) -> void:
	if not (v is float or v is int) or float(v) < 0.0 or float(v) > 1.0:
		load_errors.append("generation %s not in [0,1]" % label)


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

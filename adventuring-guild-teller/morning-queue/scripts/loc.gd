class_name Loc
extends RefCounted
## Loc — the single string + localization layer for The Morning Queue.
##
## Two layers live in this project, and only ONE of them lives here:
##   (a) TRANSLATABLE  — UI chrome + the finite enum/slug vocabulary (task types,
##       affiliations, verdicts, reference-table titles, headings). It all lives in
##       `_LOCALES` below, keyed, one dictionary per locale.
##   (b) CONTENT       — visitor names, claim summaries, player_story, failure reasons.
##       That is procedural prose; it stays in data/*.json (the content bundle) and is
##       never hardcoded here. This module never reaches into that layer.
##
## IDENTIFIER vs DISPLAY — the rule the whole codebase obeys:
##   JSON keys, enum values and slug ids (`item_check`, `cistern-wisp-swarm`,
##   `rank_order`) are IDENTIFIERS: used for logic + lookup, NEVER shown raw. This is
##   the one place an identifier is turned into human text. Never mutate the identifiers
##   themselves — renaming a key breaks `checks[].entry` → references.json resolution.
##
## Registered via `class_name`, exactly like Palette / ThemeFactory — call statically
## (`Loc.t("...")`, `Loc.humanize("...")`); no autoload, no preload. Because it is a new
## class_name script, the global-class cache must be regenerated once after adding it
## (`godot --headless --path . --import`, or open the editor) or the project won't parse.
##
## ADDING A LOCALE is a data change, not a code change: add one dictionary to `_LOCALES`
## (its own `chrome` / `vocab` / `overrides` sub-maps) and set `Loc.locale`. English is
## the only shipped locale today; the humanizer is the English auto-fallback.

const DEFAULT_LOCALE := "en"

## The active locale. Flip this (once, at startup) to switch the whole UI. A missing
## key falls back to the default locale, then to a humanized form of the raw id, so a
## half-translated locale still renders — never a raw identifier.
static var locale: String = DEFAULT_LOCALE

## Locale data lives in data/locales/<locale>.json (see ADDING A LOCALE above); this
## cache is populated once, lazily, on first access. A missing/broken file degrades to
## the humanizer rather than crashing (components call Loc during _ready).
const _LOCALES_PATH := "res://data/locales/en.json"
static var _locales_cache: Dictionary = {}
static var _locales_loaded := false


static func _locales() -> Dictionary:
	if _locales_loaded:
		return _locales_cache
	_locales_loaded = true
	var text := FileAccess.get_file_as_string(_LOCALES_PATH)
	if text.is_empty():
		push_warning("Loc: could not read " + _LOCALES_PATH + "; falling back to humanizer")
		return _locales_cache
	var parsed = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		push_warning("Loc: malformed JSON in " + _LOCALES_PATH + "; falling back to humanizer")
		return _locales_cache
	_locales_cache = parsed
	return _locales_cache


# --- locale sub-table accessors -------------------------------------------------

static func _table() -> Dictionary:
	var locales := _locales()
	return locales.get(locale, locales.get(DEFAULT_LOCALE, {}))


static func _chrome() -> Dictionary:
	return _table().get("chrome", {})


static func _vocab() -> Dictionary:
	return _table().get("vocab", {})


static func _overrides() -> Dictionary:
	return _table().get("overrides", {})


# --- public API -----------------------------------------------------------------

## Fixed UI chrome by key (a missing key returns the key itself, never a crash).
static func t(key: String) -> String:
	return str(_chrome().get(key, key))


## Turn any identifier (JSON key, slug, enum value) into human Title-Case text.
## Overrides win; otherwise `->` becomes an arrow and `-`/`_` become spaces. This is
## the ONE slug->display implementation — no component keeps its own copy.
static func humanize(raw: String) -> String:
	var ov := _overrides()
	if ov.has(raw):
		return str(ov[raw])
	var s := raw.replace("->", " → ").replace("-", " ").replace("_", " ")
	var words := s.split(" ", false)
	var out := PackedStringArray()
	for w in words:
		if w == "→":
			out.append("→")
		else:
			out.append(w.substr(0, 1).to_upper() + w.substr(1))
	return " ".join(out)


## Look up a vocab identifier, falling back to a humanized form of the raw id.
static func _vlookup(key: String, raw: String) -> String:
	var v := _vocab()
	return str(v[key]) if v.has(key) else humanize(raw)


## affiliation enum -> display ("" for a missing/blank affiliation).
static func affiliation(a: String) -> String:
	return "" if a == "" else _vlookup("affiliation." + a, a)


## task_type enum -> display ("" for a missing/blank task).
static func task_type(tt: String) -> String:
	return "" if tt == "" else _vlookup("task." + tt, tt)


## verdict stamp -> the button face (uppercase verb).
static func stamp_button(stamp: String) -> String:
	var v := _vocab()
	var key := "stamp_btn." + stamp
	return str(v[key]) if v.has(key) else humanize(stamp).to_upper()


## verdict stamp -> the past-tense ledger form ("Approved", "Held", …).
static func stamp_past(stamp: String) -> String:
	return _vlookup("stamp_past." + stamp, stamp)


## references.json top-level table key -> its tab title (never the raw key).
static func ref_tab(key: String) -> String:
	return _vlookup("ref_tab." + key, key)


## inspection-tool id ("glass"|"scale") -> its tab title. Same identifier->display
## path as ref_tab, so a tool title never leaks its raw slug.
static func tool_tab(key: String) -> String:
	return _vlookup("tool_tab." + key, key)


## Legible display for a claim-assert value: booleans as Yes/No, whole floats as ints,
## arrays joined, and string slugs humanized. The single value formatter for the card.
static func pretty_value(value: Variant) -> String:
	match typeof(value):
		TYPE_BOOL:
			return t("bool_yes") if value else t("bool_no")
		TYPE_INT:
			return str(value)
		TYPE_FLOAT:
			var f: float = value
			return str(int(f)) if is_equal_approx(f, floor(f)) else str(f)
		TYPE_ARRAY:
			var out := PackedStringArray()
			for element in value:
				out.append(pretty_value(element))
			return ", ".join(out)
		TYPE_STRING, TYPE_STRING_NAME:
			return humanize(str(value))
		_:
			return str(value)

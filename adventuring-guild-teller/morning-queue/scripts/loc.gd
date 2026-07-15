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

const _LOCALES := {
	"en": {
		# --- (a) UI chrome: fixed strings the components used to hardcode -----------
		"chrome": {
			"counter_eyebrow": "AT THE COUNTER",
			"claim_eyebrow": "CLAIM",
			"reference_head": "REFERENCE DESK",
			"reference_empty": "(rulebook not loaded)",
				"tools_head": "INSPECTION TOOLS",
				"tool_empty": "(nothing to examine)",
				"tool_glass_caption": "Examine — what the item actually is",
				"tool_scale_caption": "Weigh — the measured amount",
				"amount_within": "within the order's limit",
				"amount_over": "over the order's limit",
				"amount_under": "under the order's limit",
				"amount_meets": "meets the order",
				"amount_no_order": "no standing order to measure against",
			"no_card": "no card on file",
			"dues_current": "dues current",
			"dues_owing": "dues owing",
			"current_season": "Current Season",
			"threshold_line": "%s sealed completions",
			"shift_complete": "SHIFT  COMPLETE",
			"day_label_tutorial": "Day 0  ·  Tutorial shift",
			"day_label": "Day %d",
			"skip_tutorial": "Skip tutorial  →",
			"next_day": "Open Day %d  →",
			"week_done": "The week is done — a clean seven days.",
			"progress_line": "Visitor %d / %d    ·    correct: %d",
			"summary_sub_clean": "verdicts stamped true — a clean book",
			"summary_sub_one": "verdicts stamped true — 1 you'll hear about",
			"summary_sub_many": "verdicts stamped true — %d you'll hear about",
			"data_error": "Data error",
			"bool_yes": "Yes",
			"bool_no": "No",
			"desk_tiles_head": "ON THE DESK",
			"desk_tiles_hint": "Click an inspection tool or quest listing to place it here.",
			"floor_head": "THE FLOOR",
			"floor_dues_intro": "Owing accounts — settle dues before the next shift opens.",
			"floor_no_dues": "All accounts current — no dues to collect.",
			"floor_accept_btn": "Accept %dg",
			"floor_paid": "Paid ✓",
		},

		# --- (a) the finite enum / slug vocabulary: identifier -> display -----------
		"vocab": {
			# affiliation (visitors.json `affiliation`)
			"affiliation.townee": "Townee",
			"affiliation.adventure": "Adventurer",
			# task_type (visitors.json `task_type`)
			"task.item_check": "Item Check",
			"task.rank_gate": "Rank Gate",
			"task.quest_file": "Quest File",
			"task.completion_claim": "Completion Claim",
			"task.rank_up": "Rank Up",
			"task.roster_change": "Roster Change",
			"task.dungeon_drop": "Dungeon Drop",
			# stamp — the button face (VerdictBar)
			"stamp_btn.approve": "APPROVE",
			"stamp_btn.reject": "REJECT",
			"stamp_btn.hold": "HOLD",
			"stamp_btn.conditional": "CONDITIONAL",
			# stamp — the past-tense ledger form (Scoreboard summary)
			"stamp_past.approve": "Approved",
			"stamp_past.reject": "Rejected",
			"stamp_past.hold": "Held",
			"stamp_past.conditional": "Conditional",
			# inspection-tool tab titles (routes like ref_tab: identifier -> display)
				"tool_tab.glass": "The Glass",
				"tool_tab.scale": "The Scale",
				# reference-table tab titles (references.json top-level keys)
			"ref_tab.rank_order": "Rank Ladder",
			"ref_tab.book": "Reference Book",
			"ref_tab.postings": "Quest Board",
			"ref_tab.rank_ledger": "Rank Ledger",
			"ref_tab.rankup_thresholds": "Rank-Up Schedule",
			"ref_tab.archive": "Completion Archive",
			"ref_tab.cipher_table": "Chapter Ciphers",
			"ref_tab.drop_table": "Dungeon Drops",
			"ref_tab.season": "Season Wheel",
			"ref_tab.payout": "Payout Rule",
			"ref_tab.roster": "Active Roster",
			"ref_tab.townee_directory": "Townee Directory",
			"ref_tab.adventurer_directory": "Adventurer Directory",
			# failure-axis display (only surfaced if the verdict names the axis)
			"failure_axis.dues": "Dues",
			"failure_axis.amount": "Amount",
		},

		# --- proper-noun overrides for the humanizer -------------------------------
		# Slugs the generic Title-Case humanizer would get wrong (hyphenated proper
		# nouns, acronyms). Keyed by the raw identifier; the id is never mutated.
		"overrides": {
			"hulbr-odd-eye": "Hulbr Odd-Eye",
			"odd-eyes-party": "Odd-Eyes Party",
		},
	},
}


# --- locale sub-table accessors -------------------------------------------------

static func _table() -> Dictionary:
	return _LOCALES.get(locale, _LOCALES[DEFAULT_LOCALE])


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

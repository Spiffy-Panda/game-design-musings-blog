extends Control
## Main — the integrator. Assembles the four component scenes into the desk layout and
## wires them to the Session/Deck autoloads. Owns NO game rules; it is pure plumbing, so
## component sub-agents can work behind stable signals without ever editing this file.
##
## Layout:  [ booth column: VisitorCard / VerdictBar / Scoreboard ] | [ ReferencePanel ]

const VisitorCardScene := preload("res://scenes/VisitorCard.tscn")
const ReferencePanelScene := preload("res://scenes/ReferencePanel.tscn")
const VerdictBarScene := preload("res://scenes/VerdictBar.tscn")
const ScoreboardScene := preload("res://scenes/Scoreboard.tscn")

var _card: Node
var _reference: Node
var _verdict: Node
var _score: Node


func _ready() -> void:
	theme = ThemeFactory.build()  # parchment desk theme (cascades to every child Control)
	_build_layout()
	_wire()

	if not Deck.ok:
		_card.show_visitor({ "name": Loc.t("data_error"), "claim": { "summary": "\n".join(Deck.load_errors) } })
		return

	_reference.set_references(Deck.references)
	Session.start()

	# Dev-only: hand the validation harness the same handler a stamp-press fires, so it can
	# auto-step the shift and screenshot each state. Untick DevHarness.Enabled to play manually.
	var dev := get_node_or_null("DevHarness")
	if dev:
		dev.begin(_on_stamp_chosen)


func _build_layout() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var margin := MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	for side in ["left", "top", "right", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 20)
	add_child(margin)

	var split := HBoxContainer.new()
	split.add_theme_constant_override("separation", 20)
	margin.add_child(split)

	# Booth column (left) — the counter and the commit controls.
	var booth := VBoxContainer.new()
	booth.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	booth.add_theme_constant_override("separation", 16)
	split.add_child(booth)

	_card = VisitorCardScene.instantiate()
	_card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_verdict = VerdictBarScene.instantiate()
	_score = ScoreboardScene.instantiate()
	booth.add_child(_card)
	booth.add_child(_verdict)
	booth.add_child(_score)

	# Reference column (right) — the whole lookup surface.
	_reference = ReferencePanelScene.instantiate()
	_reference.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	split.add_child(_reference)


func _wire() -> void:
	Session.visitor_changed.connect(_on_visitor_changed)
	Session.verdict_recorded.connect(_on_verdict_recorded)
	Session.shift_complete.connect(_on_shift_complete)
	_verdict.stamp_chosen.connect(_on_stamp_chosen)


func _on_visitor_changed(v: Dictionary) -> void:
	_card.show_visitor(v)
	_reference.set_inspection_target(v)  # refill the Glass/Scale tool pages for this visitor
	_score.set_progress(Session.index, Deck.count(), Session.score)
	_verdict.set_enabled(true)


func _on_stamp_chosen(stamp: String) -> void:
	_verdict.set_enabled(false)
	Session.submit(stamp)
	Session.advance()


func _on_verdict_recorded(entry: Dictionary) -> void:
	var mark := "right" if entry.get("right", false) else "WRONG (wanted %s)" % entry.get("correct", "?")
	print("[verdict] %s -> %s : %s" % [entry.get("id", "?"), entry.get("chosen", "?"), mark])


func _on_shift_complete(summary: Dictionary) -> void:
	_verdict.set_enabled(false)
	_score.show_summary(summary)
	print("[shift] complete: %d / %d correct" % [summary.get("correct", 0), summary.get("total", 0)])

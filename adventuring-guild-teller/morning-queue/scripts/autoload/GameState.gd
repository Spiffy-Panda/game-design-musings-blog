extends Node
## Session (autoload) — the shift's flow state machine and scorekeeper.
##
## The single source of truth for "who is at the counter, what have I stamped, how am I
## doing." Scenes never advance the queue themselves — they call Session and react to its
## signals. This is a stable-interface file: extend, do not rename.
##
## Flow:  start() -> visitor_changed -> [player looks things up] -> submit(stamp)
##        -> verdict_recorded -> advance() -> visitor_changed ... -> shift_complete
##
## Contract (what other scripts may call):
##   Session.start()                     begin the shift at visitor 0
##   Session.current()      -> Dictionary  the visitor at the counter (or {})
##   Session.index          -> int         0-based position in the queue
##   Session.submit(stamp: String)         record the player's stamp for the current visitor
##   Session.advance()                     move to the next visitor (or finish the shift)
##   Session.score          -> int         running correct-count
##   Session.verdict_log    -> Array        per-visitor {id, name, chosen, correct, right}
## Signals (what scenes listen to):
##   visitor_changed(visitor: Dictionary)
##   verdict_recorded(entry: Dictionary)   {id, name, chosen, correct, right: bool}
##   shift_complete(summary: Dictionary)   {total, correct, log}
##
## `stamp` values are the four desk verdicts: "approve" | "reject" | "hold" | "conditional".
## Correctness is judged against visitor.truth.stamp; a strict two-stamp mode can instead
## judge against visitor.truth.binary (see STRICT_BINARY).

signal visitor_changed(visitor: Dictionary)
signal verdict_recorded(entry: Dictionary)
signal shift_complete(summary: Dictionary)

## When true, the desk is approve/reject only and correctness uses truth.binary
## (collapsing hold/conditional to reject). This is the AGT.5 "is the desk strictly
## binary?" dial — flip it to compare the two feels. Currently ON (INSPECTION-TOOLS.md
## §7): the desk ships binary. Flip back to false to restore the four-verdict feel; the
## hold/conditional code paths and Loc entries are kept intact for exactly that.
const STRICT_BINARY := true

var index: int = -1
var score: int = 0
var verdict_log: Array = []
var _finished: bool = false


func start() -> void:
	index = -1
	score = 0
	verdict_log.clear()
	_finished = false
	advance()


func current() -> Dictionary:
	return Deck.get_visitor(index)


func submit(stamp: String) -> void:
	var v := current()
	if v.is_empty() or _finished:
		return
	var truth: Dictionary = v.get("truth", {})
	var correct: String = str(truth.get("binary", "reject")) if STRICT_BINARY else str(truth.get("stamp", "reject"))
	var right := stamp == correct
	if right:
		score += 1
	var entry := { "id": v.get("id", "?"), "name": v.get("name", ""), "chosen": stamp, "correct": correct, "right": right }
	verdict_log.append(entry)
	verdict_recorded.emit(entry)


func advance() -> void:
	if _finished:
		return
	index += 1
	if index >= Deck.count():
		_finished = true
		shift_complete.emit({ "total": Deck.count(), "correct": score, "log": verdict_log })
		return
	visitor_changed.emit(current())

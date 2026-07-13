class_name Palette
extends RefCounted
## Shared color tokens for The Morning Queue — the Adventuring Guild Teller palette
## (parchment ground, approval-green, wax-red, brass), matching ../pitch.html. FROZEN
## shared contract: every component reads these; do not fork the values. Use as
## `Palette.GREEN` etc. (registered via class_name, no preload needed).

const GROUND := Color("f1ecdc")   # page ground
const SURFACE := Color("f9f6ec")  # card / panel fill
const LINE := Color("dcd2b6")     # hairlines
const LINE2 := Color("c3b795")    # stronger separators
const INK := Color("2a2419")      # primary text
const INK2 := Color("57503f")     # secondary text
const INK3 := Color("7a6f58")     # muted / labels
const GREEN := Color("2f6b4f")    # approve · links · primary accent
const RED := Color("9c3122")      # reject · seals · gaps
const BRASS := Color("8a6d1f")    # hold/conditional · read-tags · ribbons

## Canonical color for a desk verdict stamp.
static func verdict_color(stamp: String) -> Color:
	match stamp:
		"approve": return GREEN
		"reject": return RED
		"hold", "conditional": return BRASS
		_: return INK2

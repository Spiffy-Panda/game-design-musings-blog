extends RefCounted
## GTH · SceneProbe — element resolution, the predictive hit-stack, clickability/geometry
## reports, and the interactable snapshot. All read-only introspection.
##
## Two hit-stack modes. **Mode A (predictive, default)** replays Godot's GUI pick order — children
## before parents, later siblings before earlier (topmost first), IGNORE skipped — and marks the
## first STOP as the consumer; the PASS controls ahead of it "received before consume". Cheap,
## deterministic, headless-safe. **Mode B (`trace: true`)** watches every Control's `gui_input`
## during a real click and reports who actually received it, then scores that against Mode A's
## prediction. Use it whenever a click's outcome surprises you: a prediction cannot falsify itself.
##
## Two rules this file exists to keep, both learned the hard way (see GTH.B1/B4 on the plan):
##
##  1. `on_screen` means FULLY on screen. A partly-clipped control reports `visible_fraction`
##     and its `clipped` edges instead of rounding up to a reassuring boolean, and its click
##     anchor is CLAMPED into the visible part. So `clickable` means "a click aimed at this
##     would land on it" — never "4 of this button's 90 pixels overlap the viewport".
##  2. Embedded Windows (popup dialogs) are their own viewports, and they are not skipped.
##     A point over one is reported against that window's own contents; a main-viewport
##     control sitting underneath one is reported as occluded by it. Attributing the click to
##     the control underneath is a WRONG ANSWER, not a gap.
##
## Both rules point the same way: this harness may be wrong, but it may not be confidently
## wrong. A false "reachable" is the same failure direction as a false "unchanged".
##
## Pure-engine: NO project-specific references — reusable across any Godot project.

var _h: Node          # the harness autoload (tree / viewport access)
var _meta_key := "test_id"

func _init(harness: Node, meta_key := "test_id") -> void:
	_h = harness
	_meta_key = meta_key

func _root() -> Node: return _h.get_tree().root
func _vp() -> Viewport: return _h.get_tree().root
func _vsize() -> Vector2: return _vp().get_visible_rect().size

# --- resolution ---------------------------------------------------------------------------

## Resolve an element handle to candidate nodes (never guesses; returns all matches).
## handle: {test_id} | {path} | {group} | {text} (exact) | {contains} (substring)
func resolve(handle: Dictionary) -> Array:
	var root := _root()
	if handle.has("test_id"):
		return _by_meta(root, str(handle["test_id"]))
	if handle.has("group"):
		return _h.get_tree().get_nodes_in_group(str(handle["group"]))
	if handle.has("path"):
		var n := root.get_node_or_null(NodePath(str(handle["path"])))
		return [n] if n != null else []
	if handle.has("text"):
		return _by_text(root, str(handle["text"]), true)
	if handle.has("contains"):
		return _by_text(root, str(handle["contains"]), false)
	return []

func _by_meta(root: Node, id: String) -> Array:
	var out := []
	for n in _walk(root):
		if (n.has_meta(_meta_key) and str(n.get_meta(_meta_key)) == id) or n.is_in_group("test:" + id):
			out.append(n)
	return out

func _by_text(root: Node, needle: String, exact: bool) -> Array:
	var out := []
	for n in _walk(root):
		var t := _text_of(n)
		if t == "":
			continue
		if (exact and t == needle) or (not exact and needle in t):
			out.append(n)
	return out

func _walk(node: Node) -> Array:
	var out := [node]
	for c in node.get_children():
		out.append_array(_walk(c))
	return out

# --- viewports ----------------------------------------------------------------------------
# A Control's rect is expressed in the coordinate space of the Viewport that owns it. The
# root is a Viewport; so is every embedded Window and SubViewport. Measuring a dialog's
# button against the ROOT's size would be its own quiet lie, so every geometry call below
# resolves the owning viewport first.

## The Viewport (root Window / embedded Window / SubViewport) whose space `n`'s rect lives in.
func _viewport_of(n: Node) -> Viewport:
	var p: Node = n
	while p != null:
		if p is Viewport:
			return p as Viewport
		p = p.get_parent()
	return _vp()

func _vsize_of(vp: Viewport) -> Vector2:
	return vp.get_visible_rect().size

## The embedded Window ancestor of `n`, or null when `n` lives in the main viewport.
func _window_of(n: Node) -> Window:
	var p: Node = n
	while p != null:
		if p is Window and p != _root():
			return p as Window
		p = p.get_parent()
	return null

## Visible embedded Windows covering `point` (root-viewport px), back-to-front.
## An embedded Window's `position` is in its embedder's coordinate space.
func _embedded_windows_at(point: Vector2) -> Array:
	var out := []
	for n in _walk(_root()):
		if not (n is Window) or n == _root():
			continue
		var w := n as Window
		if not w.visible or not w.is_inside_tree() or not w.is_embedded():
			continue
		if Rect2(Vector2(w.position), Vector2(w.size)).has_point(point):
			out.append(w)
	return out

func _window_info(w: Window) -> Dictionary:
	return {path = _path(w), title = str(w.title), type = w.get_class(),
		rect_px = [w.position.x, w.position.y, w.size.x, w.size.y]}

# --- geometry (GTH.B1: honest about partial visibility) ------------------------------------

func _vrect_of(vp: Viewport) -> Rect2:
	return Rect2(Vector2.ZERO, _vsize_of(vp))

## The part of `grect` actually inside the viewport (zero-sized when wholly outside).
func _visible_part(grect: Rect2, vp: Viewport) -> Rect2:
	return _vrect_of(vp).intersection(grect)

func _visible_fraction(grect: Rect2, vp: Viewport) -> float:
	var area := grect.size.x * grect.size.y
	if area <= 0.0:
		return 0.0
	var vis := _visible_part(grect, vp)
	return clampf((vis.size.x * vis.size.y) / area, 0.0, 1.0)

## STRICT. A control with 4px showing is NOT on_screen — it is partially visible, and the
## report says so via `visible_fraction` / `clipped`. This is the GTH.B1 fix: `intersects()`
## here is what let a button at x=1276-1366 in a 1280 viewport certify itself on-screen.
func _onscreen(grect: Rect2, vp: Viewport) -> bool:
	return _vrect_of(vp).encloses(grect)

func _clipped_edges(grect: Rect2, vp: Viewport) -> Array:
	var vs := _vsize_of(vp)
	var out := []
	if grect.position.x < 0.0: out.append("left")
	if grect.position.y < 0.0: out.append("top")
	if grect.position.x + grect.size.x > vs.x: out.append("right")
	if grect.position.y + grect.size.y > vs.y: out.append("bottom")
	return out

## The point a click will ACTUALLY be aimed at: the requested anchor, clamped into the
## on-screen part of the rect. Unclamped, a centre anchor on a clipped control aims outside
## the window — the click lands nowhere while the hit-stack, testing that same outside point
## against the control's own (partly-offscreen) rect, still happily certifies `is_top_hit`.
## That pair is exactly how GTH.B1 reported an unreachable button as clickable.
func anchor_point(grect: Rect2, vp: Viewport, anchor := Vector2(0.5, 0.5)) -> Vector2:
	var raw := grect.position + grect.size * anchor
	var vis := _visible_part(grect, vp)
	if vis.size.x <= 0.0 or vis.size.y <= 0.0:
		return raw  # nothing of it is on screen — return the honest, unreachable point
	var lo := vis.position
	var hi := vis.position + vis.size - Vector2.ONE  # stay a pixel inside the visible part
	return Vector2(clampf(raw.x, lo.x, maxf(lo.x, hi.x)), clampf(raw.y, lo.y, maxf(lo.y, hi.y)))

# --- hit-stack (Mode A predictive) --------------------------------------------------------

## Front-to-back list of Controls under `point_px` that could receive the event, in the space
## of `vp` (the root viewport by default).
func hit_stack(point_px: Vector2, vp: Viewport = null) -> Array:
	var out := []
	_pick(vp if vp != null else _root(), point_px, out)
	return out

func _pick(node: Node, point: Vector2, out: Array) -> void:
	var skip_children := false
	if node is Control:
		var cc := node as Control
		if cc.clip_contents and not cc.get_global_rect().has_point(point):
			skip_children = true  # a clipping parent hides children outside its rect
	if not skip_children:
		var ch := node.get_children()
		for i in range(ch.size() - 1, -1, -1):  # reverse: later siblings draw on top
			var k = ch[i]
			if k is CanvasItem and not (k as CanvasItem).visible:
				continue
			if k is Window:
				continue  # its own viewport — handled by the caller, never silently merged
			_pick(k, point, out)
	if node is Control:
		var c := node as Control
		if c.is_visible_in_tree() and c.mouse_filter != Control.MOUSE_FILTER_IGNORE \
				and c.get_global_rect().has_point(point):
			out.append(c)

func hit_report(point_px: Vector2) -> Dictionary:
	var vs := _vsize()
	var out := {
		point_px = [point_px.x, point_px.y],
		point_norm = [point_px.x / vs.x, point_px.y / vs.y],
	}

	# GTH.B4 — an embedded Window over the point OWNS the point. Controls in the main
	# viewport beneath it never see the click, so reporting them would be a wrong answer.
	var wins := _embedded_windows_at(point_px)
	var stack: Array
	if not wins.is_empty():
		var w: Window = wins[wins.size() - 1]  # topmost
		var local := point_px - Vector2(w.position)
		stack = []
		_pick(w, local, stack)
		out["viewport"] = "embedded_window"
		out["window"] = _window_info(w)
		out["point_in_window_px"] = [local.x, local.y]
		out["note"] = "point is over an embedded Window (its own viewport); the stack below is " \
			+ "that window's contents in ITS coords — main-viewport controls beneath it cannot " \
			+ "receive this click"
		if wins.size() > 1:
			out["windows_over_point"] = wins.map(func(x): return _path(x))
	else:
		stack = hit_stack(point_px)
		out["viewport"] = "root"
		out["note"] = "predictive (Mode A) — Godot GUI pick order; no embedded Window covers this point"

	var entries := []
	var received := []
	var consumer = null
	var consumed := false
	for c in stack:
		var e := _describe(c)
		e["mouse_filter"] = _filter_name(c.mouse_filter)
		var stop: bool = c.mouse_filter == Control.MOUSE_FILTER_STOP
		if not consumed:
			e["received"] = true
			received.append(e["path"])
			e["consumed_here"] = stop
			if stop:
				consumer = e["path"]
				consumed = true
		else:
			e["received"] = false
			e["consumed_here"] = false
		entries.append(e)
	out["stack"] = entries
	out["consumer"] = consumer
	out["received_before_consume"] = received
	return out

# --- hit-stack (Mode B: observed trace) ---------------------------------------------------
# Mode A predicts the chain; Mode B watches it. They are deliberately independent: begin_trace
# connects to EVERY eligible Control, not merely Mode A's candidates -- a trace that can only
# see what the prediction already predicted cannot falsify it, and falsifying it is the whole
# point. GTH.B4 was Mode A naming a consumer, with total confidence, that never got the event.

var _trace_conns := []
var _trace_log := []

## Connect to every visible, non-IGNORE Control's `gui_input`. Returns the count watched.
func begin_trace() -> int:
	end_trace()  # never leave a previous run's connections behind
	_trace_log.clear()
	for n in _walk(_root()):
		if not (n is Control):
			continue
		var c := n as Control
		if not c.is_visible_in_tree() or c.mouse_filter == Control.MOUSE_FILTER_IGNORE:
			continue
		var cb := _on_traced.bind(c)
		c.gui_input.connect(cb)
		_trace_conns.append([c, cb])
	return _trace_conns.size()

func _on_traced(event: InputEvent, c: Control) -> void:
	if not (event is InputEventMouseButton) or not (event as InputEventMouseButton).pressed:
		return
	# Godot emits `gui_input` BEFORE running the Control's own `_gui_input`, so this flag says
	# whether the event had ALREADY been consumed when it arrived here — not whether THIS control
	# consumed it. Hence the consumer is inferred from where the chain stops (see trace_report).
	_trace_log.append({
		path = _path(c), type = c.get_class(),
		test_id = str(c.get_meta(_meta_key)) if c.has_meta(_meta_key) else null,
		handled_on_arrival = c.get_viewport().is_input_handled(),
	})

func end_trace() -> Array:
	for t in _trace_conns:
		var c = t[0]
		if is_instance_valid(c) and c.gui_input.is_connected(t[1]):
			c.gui_input.disconnect(t[1])
	_trace_conns.clear()
	return _trace_log.duplicate()

## Turn an observed run into a report, and compare it against what Mode A predicted.
func trace_report(observed: Array, predicted_consumer = null) -> Dictionary:
	var out := {
		mode = "B — observed gui_input trace",
		received = observed,
		note = "best-effort. Godot emits `gui_input` before a Control runs its own `_gui_input`, "
			+ "so the consumer is inferred from where the chain STOPS, not from a per-control flag. "
			+ "Anything consuming input outside the GUI dispatch (a raw _input handler) is invisible here.",
	}
	var seen = null
	if not observed.is_empty():
		seen = observed[observed.size() - 1]["path"]
	out["consumer_observed"] = seen
	if predicted_consumer != null or seen != null:
		out["consumer_predicted"] = predicted_consumer
		out["agrees_with_mode_a"] = (predicted_consumer == seen)
		if predicted_consumer != seen:
			out["disagreement"] = "Mode A predicted %s; the event was actually last seen at %s. " \
				% [str(predicted_consumer), str(seen)] \
				+ "Believe the trace — it watched, the prediction guessed."
	return out

# --- clickability + geometry --------------------------------------------------------------

func clickability(node: Node, anchor := Vector2(0.5, 0.5)) -> Dictionary:
	if node == null:
		return {exists = false}
	var d := _describe(node)
	d["exists"] = true
	if not (node is Control):
		d["clickable"] = false
		d["reason"] = "not a Control (no screen hit-box)"
		return d
	var c := node as Control
	var grect := c.get_global_rect()
	var vp := _viewport_of(c)
	var vs := _vsize_of(vp)
	var own_win := _window_of(c)
	var vis := c.is_visible_in_tree()
	var disabled := (c is BaseButton) and (c as BaseButton).disabled
	var filt_ok := c.mouse_filter != Control.MOUSE_FILTER_IGNORE
	var fully_on := _onscreen(grect, vp)
	var frac := _visible_fraction(grect, vp)

	# the point we would REALLY click — clamped into whatever part of it is on screen
	var point := anchor_point(grect, vp, anchor)
	var anchor_on := _vrect_of(vp).has_point(point)

	var stack := hit_stack(point, vp)
	var occluders := []
	var reached := false
	for s in stack:
		if s == c:
			reached = true
			break
		if s.mouse_filter == Control.MOUSE_FILTER_STOP:
			occluders.append(_path(s))

	# GTH.B4 — an embedded Window over our anchor blocks us, unless we are inside it.
	# Only meaningful for main-viewport controls: a dialog's own coords are its own space.
	var blocking_window = null
	if own_win == null:
		for w in _embedded_windows_at(point):
			blocking_window = _window_info(w)
			occluders.append(_path(w))
			break

	var is_top_hit: bool = reached and occluders.is_empty()
	d["clickable"] = vis and not disabled and filt_ok and anchor_on and is_top_hit
	d["factors"] = {
		in_tree = c.is_inside_tree(), visible = vis, enabled = not disabled,
		mouse_filter = _filter_name(c.mouse_filter), on_screen = fully_on,
		anchor_on_screen = anchor_on, is_top_hit = is_top_hit,
	}
	d["rect_px"] = [grect.position.x, grect.position.y, grect.size.x, grect.size.y]
	d["rect_norm"] = [grect.position.x / vs.x, grect.position.y / vs.y, grect.size.x / vs.x, grect.size.y / vs.y]
	d["screen_size_px"] = [grect.size.x, grect.size.y]
	d["viewport_px"] = [vs.x, vs.y]
	d["on_screen"] = fully_on                                   # STRICT: fully inside
	d["visible_fraction"] = snappedf(frac, 0.001)
	d["clipped"] = _clipped_edges(grect, vp)
	if not fully_on:
		d["offscreen_reason"] = _offscreen_reason(grect, vp)
	d["focus_mode"] = c.focus_mode
	d["z_index"] = c.z_index
	d["canvas_layer"] = _canvas_layer_of(c)
	d["anchor_point_px"] = [point.x, point.y]
	d["anchor_clamped"] = point != (grect.position + grect.size * anchor)
	d["occluded_by"] = occluders
	if own_win != null:
		d["in_embedded_window"] = _window_info(own_win)
	if blocking_window != null:
		d["blocked_by_window"] = blocking_window
	return d

# --- snapshot -----------------------------------------------------------------------------

## Compact interactable tree — the token-cheap "what's on screen" channel (no pixels).
## `clickable` here is the SAME predicate query_element uses, occlusion included. It used to
## be a weaker one that omitted is_top_hit, so the two calls could disagree about the same
## button and neither said which to believe (GTH.B1).
func snapshot(filter := {}) -> Dictionary:
	var only_interactable: bool = filter.get("interactable", true)
	var include_offscreen: bool = filter.get("offscreen", false)
	var vs := _vsize()
	var items := []
	for n in _walk(_root()):
		if not (n is Control):
			continue
		var c := n as Control
		if not c.is_visible_in_tree():
			continue
		if only_interactable and not _interactable(c) and not c.has_meta(_meta_key):
			continue
		var vp := _viewport_of(c)
		var grect := c.get_global_rect()
		var frac := _visible_fraction(grect, vp)
		# NB: inclusion tests "any part visible", NOT the strict on_screen — else the very
		# control a layout bug has shoved half out of the window would vanish from the
		# snapshot that exists to find it.
		if frac <= 0.0 and not include_offscreen:
			continue
		var vsz := _vsize_of(vp)
		var rep := clickability(c)
		var item := {
			test_id = str(c.get_meta(_meta_key)) if c.has_meta(_meta_key) else null,
			path = _path(c),
			type = c.get_class(),
			text = _text_of(c),
			rect_norm = [snappedf(grect.position.x / vsz.x, 0.001), snappedf(grect.position.y / vsz.y, 0.001),
				snappedf(grect.size.x / vsz.x, 0.001), snappedf(grect.size.y / vsz.y, 0.001)],
			on_screen = rep.get("on_screen", false),
			clickable = rep.get("clickable", false),
		}
		if frac < 1.0:
			item["visible_fraction"] = snappedf(frac, 0.001)
			item["clipped"] = _clipped_edges(grect, vp)
		var owner_win := _window_of(c)
		if owner_win != null:
			item["in_embedded_window"] = _path(owner_win)
		items.append(item)
	return {count = items.size(), viewport = [vs.x, vs.y], elements = items}

# --- helpers ------------------------------------------------------------------------------

func _describe(n: Node) -> Dictionary:
	return {
		path = _path(n),
		test_id = str(n.get_meta(_meta_key)) if n.has_meta(_meta_key) else null,
		type = n.get_class(),
		text = _text_of(n),
	}

func _path(n: Node) -> String:
	return str(_root().get_path_to(n))

func _text_of(n: Node) -> String:
	if n is Button or n is Label or n is LineEdit or n is TextEdit or n is LinkButton:
		return str(n.get("text"))
	return ""

func _interactable(c: Control) -> bool:
	return c is BaseButton or c is Range or c is LineEdit or c is TextEdit \
		or c is Tree or c is ItemList or c is TabBar

func _offscreen_reason(grect: Rect2, vp: Viewport) -> String:
	var vs := _vsize_of(vp)
	if grect.position.x + grect.size.x <= 0.0: return "entirely left of the viewport"
	if grect.position.y + grect.size.y <= 0.0: return "entirely above the viewport"
	if grect.position.x >= vs.x: return "entirely right of the viewport"
	if grect.position.y >= vs.y: return "entirely below the viewport"
	var pct := int(round(_visible_fraction(grect, vp) * 100.0))
	return "clipped %s — only %d%% of it is inside the %dx%d viewport" \
		% [", ".join(_clipped_edges(grect, vp)), pct, int(vs.x), int(vs.y)]

func _filter_name(f: int) -> String:
	match f:
		Control.MOUSE_FILTER_STOP: return "stop"
		Control.MOUSE_FILTER_PASS: return "pass"
		Control.MOUSE_FILTER_IGNORE: return "ignore"
	return "?"

func _canvas_layer_of(n: Node) -> int:
	var p := n.get_parent()
	while p != null:
		if p is CanvasLayer:
			return (p as CanvasLayer).layer
		p = p.get_parent()
	return 0

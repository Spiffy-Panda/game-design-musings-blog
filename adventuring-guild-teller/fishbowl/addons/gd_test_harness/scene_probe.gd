extends RefCounted
## GTH · SceneProbe — element resolution, the predictive hit-stack, clickability/geometry
## reports, and the interactable snapshot. All read-only introspection.
##
## Hit-stack fidelity is Mode A (predictive): it replays Godot's GUI pick order — children
## before parents, later siblings before earlier (topmost first), IGNORE skipped — and marks
## the first STOP as the consumer; the PASS controls ahead of it "received before consume".
## Single main-viewport; embedded Windows (popup dialogs) are their own viewports (a v0
## limitation, flagged in every report). Mode B (active gui_input trace) is future work.
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

# --- hit-stack (Mode A predictive) --------------------------------------------------------

## Front-to-back list of Controls under `point_px` that could receive the event.
func hit_stack(point_px: Vector2) -> Array:
	var out := []
	_pick(_root(), point_px, out)
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
				continue  # embedded subwindows are separate viewports (v0 limitation)
			_pick(k, point, out)
	if node is Control:
		var c := node as Control
		if c.is_visible_in_tree() and c.mouse_filter != Control.MOUSE_FILTER_IGNORE \
				and c.get_global_rect().has_point(point):
			out.append(c)

func hit_report(point_px: Vector2) -> Dictionary:
	var stack := hit_stack(point_px)
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
	var vs := _vsize()
	return {
		point_px = [point_px.x, point_px.y],
		point_norm = [point_px.x / vs.x, point_px.y / vs.y],
		stack = entries,
		consumer = consumer,
		received_before_consume = received,
		note = "predictive (Mode A) — Godot GUI pick order, main viewport only; embedded Windows excluded",
	}

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
	var vs := _vsize()
	var vis := c.is_visible_in_tree()
	var disabled := (c is BaseButton) and (c as BaseButton).disabled
	var filt_ok := c.mouse_filter != Control.MOUSE_FILTER_IGNORE
	var onscreen := _onscreen(grect)
	var point := grect.position + grect.size * anchor
	var stack := hit_stack(point)
	var occluders := []
	var reached := false
	for s in stack:
		if s == c:
			reached = true
			break
		if s.mouse_filter == Control.MOUSE_FILTER_STOP:
			occluders.append(_path(s))
	var is_top_hit: bool = reached and occluders.is_empty()
	d["clickable"] = vis and not disabled and filt_ok and onscreen and is_top_hit
	d["factors"] = {
		in_tree = c.is_inside_tree(), visible = vis, enabled = not disabled,
		mouse_filter = _filter_name(c.mouse_filter), on_screen = onscreen, is_top_hit = is_top_hit,
	}
	d["rect_px"] = [grect.position.x, grect.position.y, grect.size.x, grect.size.y]
	d["rect_norm"] = [grect.position.x / vs.x, grect.position.y / vs.y, grect.size.x / vs.x, grect.size.y / vs.y]
	d["screen_size_px"] = [grect.size.x, grect.size.y]
	d["on_screen"] = onscreen
	if not onscreen:
		d["offscreen_reason"] = _offscreen_reason(grect)
	d["focus_mode"] = c.focus_mode
	d["z_index"] = c.z_index
	d["canvas_layer"] = _canvas_layer_of(c)
	d["anchor_point_px"] = [point.x, point.y]
	d["occluded_by"] = occluders
	return d

# --- snapshot -----------------------------------------------------------------------------

## Compact interactable tree — the token-cheap "what's on screen" channel (no pixels).
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
		var grect := c.get_global_rect()
		var onscreen := _onscreen(grect)
		if not onscreen and not include_offscreen:
			continue
		items.append({
			test_id = str(c.get_meta(_meta_key)) if c.has_meta(_meta_key) else null,
			path = _path(c),
			type = c.get_class(),
			text = _text_of(c),
			rect_norm = [snappedf(grect.position.x / vs.x, 0.001), snappedf(grect.position.y / vs.y, 0.001),
				snappedf(grect.size.x / vs.x, 0.001), snappedf(grect.size.y / vs.y, 0.001)],
			on_screen = onscreen,
			clickable = c.is_visible_in_tree() and not ((c is BaseButton) and (c as BaseButton).disabled) \
				and c.mouse_filter != Control.MOUSE_FILTER_IGNORE and onscreen,
		})
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

func _onscreen(grect: Rect2) -> bool:
	return Rect2(Vector2.ZERO, _vsize()).intersects(grect)

func _offscreen_reason(grect: Rect2) -> String:
	var vs := _vsize()
	if grect.position.x + grect.size.x <= 0: return "left of viewport"
	if grect.position.y + grect.size.y <= 0: return "above viewport"
	if grect.position.x >= vs.x: return "right of viewport"
	if grect.position.y >= vs.y: return "below viewport"
	return "partially clipped"

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

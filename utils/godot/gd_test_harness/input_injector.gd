extends RefCounted
## GTH · InputInjector — synthesises mouse / key / action events into Godot's real input
## pipeline via `Input.parse_input_event` (the same entry point OS input takes), so a
## Button's `pressed`, a Control's `_gui_input`, and InputMap actions all fire as if a
## user acted. For a SubViewport target, routes through `viewport.push_input(ev, true)`.
##
## Pure-engine: NO project-specific references — reusable across any Godot project.

# button_index → button_mask bit (LEFT=1, RIGHT=2, MIDDLE=3)
const _MASK := {MOUSE_BUTTON_LEFT: MOUSE_BUTTON_MASK_LEFT,
	MOUSE_BUTTON_RIGHT: MOUSE_BUTTON_MASK_RIGHT,
	MOUSE_BUTTON_MIDDLE: MOUSE_BUTTON_MASK_MIDDLE}

func click_at(pos_px: Vector2, button: int = MOUSE_BUTTON_LEFT, clicks: int = 1, viewport: Viewport = null) -> void:
	for i in clicks:
		var dbl := i >= 1
		_button(pos_px, button, true, dbl, viewport)
		_button(pos_px, button, false, dbl, viewport)
	Input.flush_buffered_events()

func _button(pos: Vector2, button: int, pressed: bool, dbl: bool, viewport: Viewport) -> void:
	var ev := InputEventMouseButton.new()
	ev.button_index = button
	ev.pressed = pressed
	ev.double_click = dbl and pressed
	ev.position = pos
	ev.global_position = pos
	ev.button_mask = _MASK.get(button, 0) if pressed else 0
	_dispatch(ev, viewport)

func move_to(pos_px: Vector2, viewport: Viewport = null) -> void:
	var ev := InputEventMouseMotion.new()
	ev.position = pos_px
	ev.global_position = pos_px
	ev.velocity = Vector2.ZERO
	_dispatch(ev, viewport)
	Input.flush_buffered_events()

func drag(from_px: Vector2, to_px: Vector2, button: int = MOUSE_BUTTON_LEFT, steps: int = 6, viewport: Viewport = null) -> void:
	_button(from_px, button, true, false, viewport)
	var prev := from_px
	for i in range(1, steps + 1):
		var p := from_px.lerp(to_px, float(i) / float(steps))
		var ev := InputEventMouseMotion.new()
		ev.position = p
		ev.global_position = p
		ev.relative = p - prev
		ev.button_mask = _MASK.get(button, 0)
		_dispatch(ev, viewport)
		prev = p
	_button(to_px, button, false, false, viewport)
	Input.flush_buffered_events()

func press_key(keycode: int, unicode: int = 0, shift := false, ctrl := false, alt := false, meta := false) -> void:
	_key(keycode, unicode, true, shift, ctrl, alt, meta)
	_key(keycode, unicode, false, shift, ctrl, alt, meta)
	Input.flush_buffered_events()

func _key(keycode: int, unicode: int, pressed: bool, shift: bool, ctrl: bool, alt: bool, meta: bool) -> void:
	var ev := InputEventKey.new()
	ev.keycode = keycode
	ev.physical_keycode = keycode
	ev.unicode = unicode
	ev.pressed = pressed
	ev.shift_pressed = shift
	ev.ctrl_pressed = ctrl
	ev.alt_pressed = alt
	ev.meta_pressed = meta
	Input.parse_input_event(ev)

func send_action(action: StringName, pressed: bool, strength: float = 1.0) -> void:
	var ev := InputEventAction.new()
	ev.action = action
	ev.pressed = pressed
	ev.strength = strength
	Input.parse_input_event(ev)
	Input.flush_buffered_events()

func _dispatch(ev: InputEvent, viewport: Viewport) -> void:
	if viewport != null:
		viewport.push_input(ev, true)  # SubViewport: local coords
	else:
		Input.parse_input_event(ev)

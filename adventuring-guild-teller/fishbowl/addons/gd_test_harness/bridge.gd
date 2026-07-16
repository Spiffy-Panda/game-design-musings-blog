extends Node
## GTH · Bridge — the LIVE (MCP) transport. A loopback WebSocket server: an external MCP
## server connects and sends line JSON-RPC {id, method, params}; each is dispatched to the
## harness core's command API and answered {id, result} | {id, error}. Same methods the
## prescripted ScenarioRunner calls — this is only the wire.
##
## GATED: started ONLY in serve mode; binds 127.0.0.1 only; never opens in a shipped build.
## Pure-engine: NO project-specific references — reusable across any Godot project.

var _core: Node
var _tcp := TCPServer.new()
var _peers: Array = []
var _listening := false

func start(core: Node, port: int) -> void:
	_core = core
	var err := _tcp.listen(port, "127.0.0.1")
	if err != OK:
		push_error("[GTH] bridge failed to listen on 127.0.0.1:%d (err %d)" % [port, err])
		return
	_listening = true
	print("[GTH] bridge listening on ws://127.0.0.1:%d" % port)

func _process(_dt: float) -> void:
	if not _listening:
		return
	while _tcp.is_connection_available():
		var conn := _tcp.take_connection()
		var ws := WebSocketPeer.new()
		ws.accept_stream(conn)
		_peers.append(ws)
		print("[GTH] bridge client connected")
	for ws in _peers:
		ws.poll()
		while ws.get_ready_state() == WebSocketPeer.STATE_OPEN and ws.get_available_packet_count() > 0:
			var txt: String = ws.get_packet().get_string_from_utf8()
			_handle(ws, txt)
	_peers = _peers.filter(func(w): return w.get_ready_state() != WebSocketPeer.STATE_CLOSED)

func _handle(ws: WebSocketPeer, txt: String) -> void:
	var msg = JSON.parse_string(txt)
	if typeof(msg) != TYPE_DICTIONARY:
		_send(ws, {error = "bad request (expected JSON object)"})
		return
	var id = msg.get("id", null)
	var method := str(msg.get("method", ""))
	var params: Dictionary = msg.get("params", {})
	var result: Variant = await _dispatch(method, params)
	_send(ws, {id = id, result = result})

func _dispatch(method: String, p: Dictionary) -> Variant:
	match method:
		"snapshot": return _core.snapshot(p.get("filter", {}))
		"query_element": return _core.query_element(p.get("element", {}))
		"read_element": return _core.read_element(p.get("element", {}))
		"hit_test": return _core.hit_test(p.get("x", 0.0), p.get("y", 0.0), p.get("normalized", true))
		"click_at": return await _core.click_at(p.get("x", 0.0), p.get("y", 0.0), p)
		"click_element": return await _core.click_element(p.get("element", {}), p)
		"press_key": return await _core.press_key(str(p.get("keys", "")), p)
		"send_action": return await _core.send_action(str(p.get("action", "")), p)
		"move_to": return await _core.move_to(p.get("x", 0.0), p.get("y", 0.0), p)
		"drag": return await _core.drag(p.get("from", [0, 0]), p.get("to", [0, 0]), p)
		"capture": return await _core.capture(p)
		"wait_for": return await _core.wait_for(p)
		"run_scenario": return await _core.run_scenario(p.get("scenario", []))
	return {error = "unknown method '%s'" % method}

func _send(ws: WebSocketPeer, obj: Dictionary) -> void:
	ws.send_text(JSON.stringify(obj))

extends RefCounted
## GTH · ScenarioRunner — the PRESCRIPTED driver. Reads a scenario (JSON: an array of steps
## or {steps:[...]}), drives each through the harness core's command API, records a compact
## log, and evaluates `expect` assertions. The SAME step vocabulary the live MCP bridge
## submits via `run_scenario`, so prescripted and live share one format.
##
## Step ops: snapshot · query · hit_test · click · click_at · key · action · read · capture
##           · wait · window · expect
## Every step takes an optional {element:{...handle}} and passes itself through as opts.

var _core: Node

func _init(core: Node) -> void:
	_core = core

func run(spec: Variant) -> Dictionary:
	var steps: Array = spec if spec is Array else spec.get("steps", [])
	var log := []
	var failures := []
	for i in steps.size():
		var st: Dictionary = steps[i]
		var op := str(st.get("op", ""))
		var r: Variant = await _run_step(op, st)
		# `expect_error` asserts that bad input is REJECTED. Without it a negative test — the
		# only honest way to cover an error path — fails the very scenario it is proving right,
		# so the error paths just go untested. That is how GTH.B2 shipped: --selftest never
		# passed a `region` at all, good or bad.
		if bool(st.get("expect_error", false)):
			if typeof(r) == TYPE_DICTIONARY and r.has("error"):
				r = {ok = true, rejected_as_expected = r["error"]}
			else:
				r = {__fail = "expected this to be rejected, but it succeeded: %s" % _brief(r)}
		var entry := {step = i, op = op, label = st.get("label", st.get("element", null))}
		if typeof(r) == TYPE_DICTIONARY and r.has("__fail"):
			failures.append({step = i, op = op, why = r["__fail"]})
			entry["fail"] = r["__fail"]
		else:
			entry["result"] = r
		if typeof(r) == TYPE_DICTIONARY and r.has("error"):
			failures.append({step = i, op = op, why = r["error"]})
		log.append(entry)
		print("[GTH] step %d %s -> %s" % [i, op, _brief(r)])
	return {ok = failures.is_empty(), steps = steps.size(), failures = failures, log = log}

func _run_step(op: String, st: Dictionary) -> Variant:
	match op:
		"snapshot": return _core.snapshot(st.get("filter", {}))
		"query": return _core.query_element(st.get("element", {}))
		"read": return _core.read_element(st.get("element", {}))
		"hit_test": return _core.hit_test(st.get("x", 0.0), st.get("y", 0.0), st.get("normalized", true))
		"click": return await _core.click_element(st.get("element", {}), st)
		"click_at": return await _core.click_at(st.get("x", 0.0), st.get("y", 0.0), st)
		"key": return await _core.press_key(str(st.get("keys", "")), st)
		"action": return await _core.send_action(str(st.get("action", "")), st)
		"capture": return await _core.capture(st)
		"wait": return await _core.wait_for(st)
		"window": return await _core.window_state(st)
		"expect": return _expect(st)
	return {error = "unknown op '%s'" % op}

func _expect(st: Dictionary) -> Dictionary:
	var q: Dictionary = _core.query_element(st.get("element", {}))
	var fails := []
	if not q.get("exists", false):
		fails.append("element not found")
	else:
		if st.has("visible") and q.get("factors", {}).get("visible", false) != st["visible"]:
			fails.append("visible=%s (wanted %s)" % [q.get("factors", {}).get("visible", false), st["visible"]])
		if st.has("clickable") and q.get("clickable", false) != st["clickable"]:
			fails.append("clickable=%s (wanted %s)" % [q.get("clickable", false), st["clickable"]])
		# on_screen is STRICT (fully inside the viewport) — assertable precisely because a
		# partly-clipped control no longer rounds itself up to true (GTH.B1).
		if st.has("on_screen") and q.get("on_screen", false) != st["on_screen"]:
			fails.append("on_screen=%s (wanted %s; visible_fraction=%s, clipped=%s)"
				% [q.get("on_screen", false), st["on_screen"],
					q.get("visible_fraction", "?"), str(q.get("clipped", []))])
		if st.has("text_contains") and not (str(st["text_contains"]) in str(q.get("text", ""))):
			fails.append("text '%s' lacks '%s'" % [q.get("text", ""), st["text_contains"]])
	if fails.is_empty():
		return {ok = true, element = q.get("text", q.get("path", ""))}
	return {__fail = ", ".join(fails)}

func _brief(r: Variant) -> String:
	if typeof(r) != TYPE_DICTIONARY:
		return str(r)
	if r.has("__fail"): return "FAIL " + str(r["__fail"])
	if r.has("error"): return "ERROR " + str(r["error"])
	if r.has("count"): return "%d elements" % r["count"]
	if r.has("consumer"): return "consumer=%s (%d received)" % [r.get("consumer"), (r.get("received_before_consume", []) as Array).size()]
	if r.has("clickable"): return "clickable=%s type=%s" % [r["clickable"], r.get("type", "?")]
	if r.has("changed"): return ("captured %s" % r.get("path", "")) if r["changed"] else ("deduped (dist %s)" % r.get("phash_distance", "?"))
	if r.has("ok"): return "ok"
	return str(r).substr(0, 80)

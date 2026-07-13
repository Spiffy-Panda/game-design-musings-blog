class_name ShiftGenerator
extends RefCounted
## ShiftGenerator — composes one desk shift procedurally from the reference banks.
##
## Data-driven and DETERMINISTIC by day: `generate_shift(day)` seeds a
## RandomNumberGenerator with `rng.seed = day`, so the same day always yields the same
## shift and a "week" is seven reproducible days (1..7). Day 0 stays the curated tutorial
## shift in data/visitors.json (the Deck loads that directly; the generator is never asked
## for day 0). Every emitted visit matches the visitors.json schema EXACTLY — id, order,
## name, affiliation, profession, task_type, portrait, claim{summary,asserts}, truth,
## checks[], inspections{glass,scale}, player_story, notes — so the VisitorCard /
## ReferencePanel / VerdictBar / Scoreboard consume a generated visit with zero edits.
##
## It reads (never writes): Deck.references (book / postings / cipher_table / drop_table /
## season / payout / roster / archive / rankup_thresholds + the injected townee_directory
## & adventurer_directory tables), Deck.townees, Deck.adventurers, Deck.generation. See
## ../CONTENT-BANKS.md §4 (the per-task recipes this file implements verbatim) and §3 (the
## Glass generalization: every visit's glass.reading is DERIVED from an authored bank
## source per glass_subject_kind).
##
## Called statically like Loc (`ShiftGenerator.generate_shift(day)`) — no autoload, so no
## autoload-order coupling with Deck. Because it is a class_name script, the global-class
## cache must be regenerated once after adding it (`godot --headless --path . --import`, or
## open the editor) or the project won't parse — same gotcha as Loc/Palette.

# The failure axes the two new mechanics add to the curated enum, plus the ones the banks
# already carry. Kept here for reference; the generator only emits axes a task admits.
const NEW_AXES := ["dues", "amount"]


## PUBLIC API. Compose the full shift for `day` (>0). Returns Array[Dictionary] already in
## `order` (1..N). Reads the banks off the Deck singleton (populated before this is called).
static func generate_shift(day: int) -> Array:
	var refs: Dictionary = Deck.references
	var gen: Dictionary = Deck.generation
	if refs.is_empty() or gen.is_empty():
		push_warning("[ShiftGenerator] banks not loaded; returning empty shift")
		return []

	var rng := RandomNumberGenerator.new()
	rng.seed = day

	var ctx := _make_context(day, rng, refs, gen)

	var shift: Dictionary = gen.get("shift", {})
	var n_min := int(shift.get("visits_min", 12))
	var n_max := int(shift.get("visits_max", 16))
	var count := rng.randi_range(n_min, n_max)

	var invalid_rate := _invalid_rate(gen, day)

	var visits: Array = []
	for n in range(1, count + 1):
		var task := _weighted_key(rng, gen.get("task_weights", {}))
		var is_valid := rng.randf() >= invalid_rate
		var visit := _compose_visit(day, n, task, is_valid, rng, ctx)
		if visit.is_empty():
			# No material for this (task, valid) combo this shift — fall back to a clean
			# item_check so the queue length is honored and the shift stays coherent.
			visit = _compose_visit(day, n, "item_check", true, rng, ctx)
		visit["id"] = "gen-d%d-%d" % [day, n]
		visit["order"] = n
		visit["portrait"] = null
		visits.append(visit)
	return visits


# --- context ----------------------------------------------------------------

## Precompute the derived pools the recipes draw from, plus per-shift "used" sets so a
## directory actor is not drawn twice in one shift (sample-without-replacement; §4.4b).
static func _make_context(day: int, _rng: RandomNumberGenerator, refs: Dictionary, gen: Dictionary) -> Dictionary:
	var ctx := {
		"refs": refs,
		"gen": gen,
		"townees": Deck.townees,
		"adventurers": Deck.adventurers,
		"rank_order": refs.get("rank_order", ["copper", "bronze", "silver", "gold", "platinum"]),
		"season": _season_for(gen, day, refs),
		"used_townees": {},        # id -> true
		"used_adventurers": {},    # id -> true
	}
	ctx["max_reach"] = _max_reach(refs)
	ctx["gate_postings"] = _gate_postings(refs)          # [{id, rank_idx}] pure rank gates
	ctx["standing_orders"] = _standing_orders(refs)      # [id,...]
	ctx["drops"] = _drops(refs)                          # [{id, floor, season, base}]
	ctx["tokens"] = _tokens(refs)                        # [{id, posting, assigned}]
	return ctx


static func _season_for(gen: Dictionary, day: int, refs: Dictionary) -> String:
	var sched: Dictionary = gen.get("season_schedule", {})
	var by_day: Dictionary = sched.get("by_day", {})
	if by_day.has(str(day)):
		return str(by_day[str(day)])
	var wheel: Array = sched.get("wheel", refs.get("season", {}).get("wheel", ["spring", "summer", "autumn", "winter"]))
	if wheel.is_empty():
		return "summer"
	return str(wheel[day % wheel.size()])


static func _invalid_rate(gen: Dictionary, day: int) -> float:
	var by_day: Dictionary = gen.get("invalid_rate_by_day", {})
	if by_day.has(str(day)):
		return float(by_day[str(day)])
	return float(gen.get("invalid_rate", 0.45))


static func _max_reach(refs: Dictionary) -> int:
	var best := 0
	for p in refs.get("roster", {}).get("parties", []):
		if p is Dictionary:
			best = max(best, int(p.get("reach_floor", 0)))
	return best


static func _gate_postings(refs: Dictionary) -> Array:
	var order: Array = refs.get("rank_order", [])
	var out: Array = []
	var postings: Dictionary = refs.get("postings", {})
	for pid in postings.keys():
		if pid.begins_with("_"):
			continue
		var p: Variant = postings[pid]
		if not (p is Dictionary):
			continue
		if str(p.get("type", "")) == "standing_order":
			continue
		if not p.has("rank_min"):
			continue
		var ward: Variant = p.get("ward_required", [])
		if ward is Array and not (ward as Array).is_empty():
			continue   # ward gates are quest_file material; rank_gate uses pure rank gates
		out.append({ "id": pid, "rank_idx": order.find(str(p.get("rank_min", ""))) })
	return out


static func _standing_orders(refs: Dictionary) -> Array:
	var out: Array = []
	var postings: Dictionary = refs.get("postings", {})
	for pid in postings.keys():
		if pid.begins_with("_"):
			continue
		var p: Variant = postings[pid]
		if p is Dictionary and str(p.get("type", "")) == "standing_order":
			out.append(pid)
	return out


static func _drops(refs: Dictionary) -> Array:
	var out: Array = []
	var dt: Dictionary = refs.get("drop_table", {})
	for did in dt.keys():
		if did.begins_with("_"):
			continue
		var d: Variant = dt[did]
		if d is Dictionary and bool(d.get("is_drop", false)):
			out.append({ "id": did, "floor": int(d.get("floor", 1)), "season": str(d.get("season", "")), "base": int(d.get("base_bounty", 0)) })
	return out


static func _tokens(refs: Dictionary) -> Array:
	var out: Array = []
	var arc: Dictionary = refs.get("archive", {})
	for tid in arc.keys():
		if tid.begins_with("_"):
			continue
		var t: Variant = arc[tid]
		if t is Dictionary and t.has("posting") and t.has("assigned_to"):
			out.append({ "id": tid, "posting": str(t["posting"]), "assigned": str(t["assigned_to"]) })
	return out


# --- dispatch ---------------------------------------------------------------

static func _compose_visit(_day: int, _n: int, task: String, is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	match task:
		"item_check":       return _item_check(is_valid, rng, ctx)
		"rank_gate":        return _rank_gate(is_valid, rng, ctx)
		"quest_file":       return _quest_file(is_valid, rng, ctx)
		"completion_claim": return _completion_claim(is_valid, rng, ctx)
		"rank_up":          return _rank_up(is_valid, rng, ctx)
		"roster_change":    return _roster_change(is_valid, rng, ctx)
		"dungeon_drop":     return _dungeon_drop(is_valid, rng, ctx)
		_:                  return _item_check(is_valid, rng, ctx)


# --- pools & picking --------------------------------------------------------

static func _weighted_key(rng: RandomNumberGenerator, weights: Dictionary) -> String:
	var total := 0.0
	var keys: Array = []
	for k in weights.keys():
		if k is String and (k as String).begins_with("_"):
			continue
		var w := float(weights[k])
		if w <= 0.0:
			continue
		total += w
		keys.append(k)
	if keys.is_empty():
		return ""
	var roll := rng.randf() * total
	var acc := 0.0
	for k in keys:
		acc += float(weights[k])
		if roll <= acc:
			return str(k)
	return str(keys[keys.size() - 1])


## Weighted pick over a subset of failure_axis_weights, restricted to `admissible`.
static func _pick_axis(rng: RandomNumberGenerator, ctx: Dictionary, admissible: Array) -> String:
	if admissible.is_empty():
		return ""
	var weights: Dictionary = ctx["gen"].get("failure_axis_weights", {})
	var sub := {}
	for a in admissible:
		sub[a] = float(weights.get(a, 1.0))
	return _weighted_key(rng, sub)


## Pick an id from `ids`, preferring one not yet used this shift; marks it used. Falls back
## to a used id (deterministically) when the pool is exhausted — correctness over no-repeat.
static func _pick_unused(rng: RandomNumberGenerator, ids: Array, used: Dictionary) -> String:
	if ids.is_empty():
		return ""
	var fresh: Array = []
	for id in ids:
		if not used.has(id):
			fresh.append(id)
	var pool: Array = fresh if not fresh.is_empty() else ids
	var pick := str(pool[rng.randi_range(0, pool.size() - 1)])
	used[pick] = true
	return pick


static func _townee_ids(ctx: Dictionary, owners_only: bool) -> Array:
	var out: Array = []
	for tid in ctx["townees"].keys():
		var t: Dictionary = ctx["townees"][tid]
		if owners_only and (t.get("owns", []) as Array).is_empty():
			continue
		out.append(tid)
	return out


static func _adventurer_ids(ctx: Dictionary) -> Array:
	return ctx["adventurers"].keys()


static func _owing(records: Dictionary, ids: Array) -> Array:
	var out: Array = []
	for id in ids:
		if str(records[id].get("dues", "current")) == "owing":
			out.append(id)
	return out


static func _current(records: Dictionary, ids: Array) -> Array:
	var out: Array = []
	for id in ids:
		if str(records[id].get("dues", "current")) == "current":
			out.append(id)
	return out


static func _walkin_name(rng: RandomNumberGenerator, ctx: Dictionary) -> String:
	var pools: Dictionary = ctx["gen"].get("name_pools", {})
	var given: Array = pools.get("given", ["Corin"])
	var sur: Array = pools.get("surname", ["Ashdown"])
	var g := str(given[rng.randi_range(0, given.size() - 1)])
	var s := str(sur[rng.randi_range(0, sur.size() - 1)])
	return "%s %s" % [g, s]


const _WALKIN_PROFESSIONS := ["Courier", "Porter", "Runner", "Carrier", "Errand-hand", "Drayman"]

static func _walkin_profession(rng: RandomNumberGenerator) -> String:
	return _WALKIN_PROFESSIONS[rng.randi_range(0, _WALKIN_PROFESSIONS.size() - 1)]


static func _rank_idx(ctx: Dictionary, rank: String) -> int:
	return (ctx["rank_order"] as Array).find(rank)


# --- unit / prose helpers ---------------------------------------------------

static func _units(amount, unit: String) -> String:
	var a := float(amount)
	var n := str(int(a)) if a == floor(a) else str(a)
	var plural := unit if (a == 1.0 or unit == "") else unit + "s"
	return "%s %s" % [n, plural]


static func _cap(s: String) -> String:
	return "" if s == "" else s.substr(0, 1).to_upper() + s.substr(1)


# --- inspection builders ----------------------------------------------------

static func _insp(glass_reading: String, glass_rel: bool, scale_reading: String, amount, unit, scale_rel: bool) -> Dictionary:
	return {
		"glass": { "reading": glass_reading, "relevant": glass_rel },
		"scale": { "reading": scale_reading, "amount": amount, "unit": unit, "relevant": scale_rel },
	}


# A decoy Scale reading for a non-weighed subject (card / seal / token / logbook / filing).
static func _decoy_scale(kind: String) -> Array:
	match kind:
		"rank_card":        return ["A guild card in its case: 2 drams on the pan.", 2, "dram"]
		"transfer_seal":    return ["The transfer card: 3 drams of waxed board.", 3, "dram"]
		"completion_token": return ["The token disc: 4 drams of good brass.", 4, "dram"]
		"logbook":          return ["The logbook: 6 drams of vellum.", 6, "dram"]
		_:                  return ["Nothing on the pan; a petition, not a delivery.", null, null]


# --- item_check -------------------------------------------------------------

static func _item_check(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var refs: Dictionary = ctx["refs"]
	var book: Dictionary = refs["book"]
	var postings: Dictionary = refs["postings"]
	var orders: Array = ctx["standing_orders"]
	if orders.is_empty():
		return {}

	var axis := ""
	if not is_valid:
		# Admissible axes depend on the order/item; pick the axis first, then an order
		# that admits it (identity needs a confusable item; paperwork needs `requires`).
		var admissible: Array = ["amount"]
		if not _orders_with_confusable(ctx, orders).is_empty():
			admissible.append("identity")
		if not _orders_with_requires(ctx, orders).is_empty():
			admissible.append("paperwork")
		axis = _pick_axis(rng, ctx, admissible)

	var order_id := ""
	match axis:
		"identity": order_id = _pick_one(rng, _orders_with_confusable(ctx, orders))
		"paperwork": order_id = _pick_one(rng, _orders_with_requires(ctx, orders))
		_: order_id = _pick_one(rng, orders)
	var order: Dictionary = postings[order_id]
	var item := str(order.get("item", ""))
	var item_rec: Dictionary = book.get(item, {})
	var unit := str(item_rec.get("unit", "dram"))

	var name := _walkin_name(rng, ctx)
	var profession := _walkin_profession(rng)
	var summary := "A delivery of \"%s\" against the %s." % [Loc.humanize(item), Loc.humanize(order_id)]
	var asserts := { "item": item, "against": order_id }

	var checks: Array = []
	var glass_reading := str(item_rec.get("glass", ""))
	var glass_rel := false
	var scale_rel := false
	var amount = _amount_within(rng, order)
	var truth: Dictionary

	if is_valid:
		# Both tools are load-bearing on a clean item_check (identity + amount).
		glass_rel = true
		scale_rel = true
		checks.append(_chk("book", item, item_rec.get("tells", []), "the delivered item", "match"))
		checks.append(_chk("posting", order_id, [_limit_str(order)], "weighed amount", _limit_result(order, amount)))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null }
	else:
		match axis:
			"identity":
				var conf: String = str((item_rec.get("confusable_with", []) as Array)[0])
				var conf_rec: Dictionary = book.get(conf, {})
				glass_reading = str(conf_rec.get("glass", ""))
				glass_rel = true
				checks.append(_chk("book", item, item_rec.get("tells", []), "the delivered item", "mismatch: it is %s" % Loc.humanize(conf)))
				checks.append(_chk("book", conf, conf_rec.get("tells", []), "the delivered item", "match"))
				checks.append(_chk("posting", order_id, [_limit_str(order)], "weighed amount", _limit_result(order, amount) + " — but the item is wrong"))
				truth = _reject("identity", "It is %s, not %s. Same family, but the tells part them — the book calls it on the look-alike." % [Loc.humanize(conf), Loc.humanize(item)])
			"paperwork":
				var req: Array = order.get("requires", [])
				checks.append(_chk("book", item, item_rec.get("tells", []), "the delivered item", "match"))
				checks.append(_chk("posting", order_id, [_limit_str(order), "%s present" % _fmt_arr(req)], "the delivery", "amount within range; %s absent" % _fmt_arr(req)))
				truth = _reject("paperwork", "The goods are right and within the limit, but the order requires %s the deliverer never obtained — right goods, incomplete request." % _fmt_arr(req))
			_:  # amount
				scale_rel = true
				# `total` orders only fail by falling short (more than needed still meets);
				# `accept` windows can fail either side.
				var dir := "under"
				if order.has("accept"):
					dir = "over" if rng.randf() < 0.5 else "under"
				amount = _amount_fail(rng, order, dir)
				if amount < 0 and order.has("accept"):
					dir = "over"
					amount = _amount_fail(rng, order, "over")
				checks.append(_chk("book", item, item_rec.get("tells", []), "the delivered item", "match"))
				checks.append(_chk("posting", order_id, [_limit_str(order)], "weighed amount", "%s the order's limit" % dir))
				truth = _reject("amount", "The item is genuine, but the weighed amount is %s the order's limit — %s does not fill %s." % [dir, _units(amount, unit), _limit_str(order)])

	var scale_reading := "The scale settles at %s." % _units(amount, unit)
	var insp := _insp(glass_reading, glass_rel, scale_reading, amount, unit, scale_rel)
	var story := "Book to %s; %s. Weigh it: %s. %s" % [
		Loc.humanize(item), ("tells match" if is_valid or axis != "identity" else "the tells fall to the look-alike"),
		_units(amount, unit), ("Stamp APPROVED." if is_valid else "Stamp REJECTED.")]
	return _visit(name, "townee", profession, "item_check", summary, asserts, truth, checks, insp, story,
		"Generated item_check (%s)." % (axis if not is_valid else "clean"))


static func _orders_with_confusable(ctx: Dictionary, orders: Array) -> Array:
	var book: Dictionary = ctx["refs"]["book"]
	var postings: Dictionary = ctx["refs"]["postings"]
	var out: Array = []
	for oid in orders:
		var item := str(postings[oid].get("item", ""))
		if not (book.get(item, {}).get("confusable_with", []) as Array).is_empty():
			out.append(oid)
	return out


static func _orders_with_requires(ctx: Dictionary, orders: Array) -> Array:
	var postings: Dictionary = ctx["refs"]["postings"]
	var out: Array = []
	for oid in orders:
		if not (postings[oid].get("requires", []) as Array).is_empty():
			out.append(oid)
	return out


static func _pick_one(rng: RandomNumberGenerator, arr: Array) -> String:
	if arr.is_empty():
		return ""
	return str(arr[rng.randi_range(0, arr.size() - 1)])


static func _amount_within(rng: RandomNumberGenerator, order: Dictionary):
	if order.has("accept"):
		var acc: Dictionary = order["accept"]
		return rng.randi_range(int(acc.get("min", 1)), int(acc.get("max", 1)))
	if order.has("total"):
		return int(order["total"].get("needed", 1))
	return 1


static func _amount_fail(rng: RandomNumberGenerator, order: Dictionary, dir: String):
	var delta := rng.randi_range(1, 3)
	if order.has("accept"):
		var acc: Dictionary = order["accept"]
		if dir == "over":
			return int(acc.get("max", 1)) + delta
		return int(acc.get("min", 1)) - delta   # may be < 0 for a min of 1..2; caller guards
	if order.has("total"):
		return int(order["total"].get("needed", 1)) - 1   # under only
	return -1


static func _limit_str(order: Dictionary) -> String:
	if order.has("accept"):
		var a: Dictionary = order["accept"]
		return "accept %s-%s %s" % [a.get("min", 0), a.get("max", 0), a.get("unit", "")]
	if order.has("total"):
		var t: Dictionary = order["total"]
		return "total %s %s" % [t.get("needed", 0), t.get("unit", "")]
	return "no limit"


static func _limit_result(order: Dictionary, amount) -> String:
	var a := float(amount)
	if order.has("accept"):
		var acc: Dictionary = order["accept"]
		if a < float(acc.get("min", 0)):
			return "under range"
		if a > float(acc.get("max", 0)):
			return "over range"
		return "within range"
	if order.has("total"):
		if a >= float(order["total"].get("needed", 0)):
			return "meets"
		return "under"
	return "no limit to measure"


# --- rank_gate --------------------------------------------------------------

static func _rank_gate(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var refs: Dictionary = ctx["refs"]
	var advs: Dictionary = ctx["adventurers"]
	var gates: Array = ctx["gate_postings"]
	if gates.is_empty():
		return {}
	var all_adv: Array = _adventurer_ids(ctx)

	var axis := ""
	if not is_valid:
		var admissible: Array = ["rank", "unverifiable"]
		if not _owing(advs, all_adv).is_empty():
			admissible.append("dues")
		axis = _pick_axis(rng, ctx, admissible)

	if not is_valid and axis == "unverifiable":
		# A walk-in claiming a rank with no card on file — mirrors ganton-reeve.
		var claimed_gate: Dictionary = gates[rng.randi_range(0, gates.size() - 1)]
		var gate_id := str(claimed_gate["id"])
		var claimed_rank := str(refs["postings"][gate_id].get("rank_min", "bronze"))
		var name := _walkin_name(rng, ctx)
		var uchecks := [_chk("ledger", "ganton-reeve", ["any card on file"], "the ledger", "no card on file under that name")]
		var utruth := _reject("unverifiable", "No card, no dues record — word alone is not proof at the desk.")
		var uscale := _decoy_scale("filing")
		var uinsp := _insp("He lays no card down — nothing to examine but his word.", false, uscale[0], uscale[1], uscale[2], false)
		return _visit(name, "adventure", "Freelancer", "rank_gate",
			"Claims rank %s for the %s, but presents no card." % [Loc.humanize(claimed_rank), Loc.humanize(gate_id)],
			{ "rank": claimed_rank, "posting": gate_id, "proof": "none" }, utruth, uchecks, uinsp,
			"Search the directory — no card on file. Nothing to check against. Stamp WITHHELD.",
			"Generated rank_gate (unverifiable).")

	# Directory-actor branches. Pick a coherent (actor, gate) pair so a valid actor truly
	# meets its gate and a rank-fail actor truly falls short.
	var adv_id := ""
	var gate: Dictionary = {}
	if is_valid:
		adv_id = _pick_unused(rng, _advs_meeting_gate(ctx, gates, all_adv, true, true), ctx["used_adventurers"])
		if adv_id == "":
			return {}
		gate = _gate_meeting(rng, gates, _rank_idx(ctx, str(advs[adv_id]["rank"])), true)
	elif axis == "dues":
		adv_id = _pick_unused(rng, _owing(advs, all_adv), ctx["used_adventurers"])
		if adv_id == "":
			return {}
		gate = _gate_meeting(rng, gates, _rank_idx(ctx, str(advs[adv_id]["rank"])), true)
	else:  # rank
		adv_id = _pick_unused(rng, _advs_meeting_gate(ctx, gates, all_adv, false, true), ctx["used_adventurers"])
		if adv_id == "":
			return _rank_gate(true, rng, ctx)
		gate = _gate_meeting(rng, gates, _rank_idx(ctx, str(advs[adv_id]["rank"])), false)
	if adv_id == "" or gate.is_empty():
		return {}

	var adv: Dictionary = advs[adv_id]
	var gate_id2 := str(gate["id"])
	var rank_min := str(refs["postings"][gate_id2].get("rank_min", ""))
	var summary := "Rank %s, taking the %s (%s or better)." % [Loc.humanize(str(adv["rank"])), Loc.humanize(gate_id2), Loc.humanize(rank_min)]
	var asserts := { "rank": str(adv["rank"]), "posting": gate_id2 }
	var checks: Array = []
	var truth: Dictionary
	var dscale := _decoy_scale("rank_card")

	if is_valid:
		checks.append(_chk("adventurer_directory", adv_id, [str(adv["rank"]), "dues current"], "the directory", "match"))
		checks.append(_chk("posting", gate_id2, ["rank_min: %s" % rank_min], "the rank", "meets"))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null }
	elif axis == "dues":
		checks.append(_chk("adventurer_directory", adv_id, ["dues current"], "the directory", "owing — %sg outstanding" % adv.get("owed", 0)))
		checks.append(_chk("posting", gate_id2, ["rank_min: %s" % rank_min], "the rank", "meets, but membership lapsed"))
		truth = _reject("dues", "The rank clears the gate, but guild dues are %sg in arrears — membership lapsed; clear the dues before taking the posting." % adv.get("owed", 0))
	else:
		checks.append(_chk("adventurer_directory", adv_id, [str(adv["rank"])], "the directory", "match"))
		checks.append(_chk("posting", gate_id2, ["rank_min: %s" % rank_min], str(adv["rank"]), "below"))
		truth = _reject("rank", "Under-ranked: a %s card against a %s gate — a rung short. That is Floor work, not a desk exception." % [Loc.humanize(str(adv["rank"])), Loc.humanize(rank_min)])

	var insp := _insp("A %s guild card, dues column inked." % str(adv["rank"]), false, dscale[0], dscale[1], dscale[2], false)
	var story := "Pull the card from the directory — %s, %s. Posting reads %s-or-better. %s" % [
		Loc.humanize(str(adv["rank"])), ("dues current" if str(adv.get("dues")) == "current" else "dues owing"),
		Loc.humanize(rank_min), ("Stamp ASSIGNED." if is_valid else "Stamp REFUSED.")]
	return _visit(str(adv["name"]), "adventure", str(adv.get("profession", "Adventurer")), "rank_gate",
		summary, asserts, truth, checks, insp, story, "Generated rank_gate (%s)." % (axis if not is_valid else "clean"))


## Gates whose rank_min is at-or-below (meets=true) or strictly above (meets=false) a rank.
static func _gate_meeting(rng: RandomNumberGenerator, gates: Array, rank_idx: int, meets: bool) -> Dictionary:
	var pool: Array = []
	for g in gates:
		var gi := int(g["rank_idx"])
		if gi < 0:
			continue
		if (meets and gi <= rank_idx) or (not meets and gi > rank_idx):
			pool.append(g)
	if pool.is_empty():
		return {}
	return pool[rng.randi_range(0, pool.size() - 1)]


## Current-dues adventurers that have at least one gate they meet (meets=true) or at least
## one gate above them (meets=false), so a coherent pair is guaranteed to exist.
static func _advs_meeting_gate(ctx: Dictionary, gates: Array, all_adv: Array, meets: bool, current_only: bool) -> Array:
	var advs: Dictionary = ctx["adventurers"]
	var out: Array = []
	for aid in all_adv:
		if current_only and str(advs[aid].get("dues")) != "current":
			continue
		var ri := _rank_idx(ctx, str(advs[aid]["rank"]))
		var has := false
		for g in gates:
			var gi := int(g["rank_idx"])
			if gi < 0:
				continue
			if (meets and gi <= ri) or (not meets and gi > ri):
				has = true
				break
		if has:
			out.append(aid)
	return out


# --- quest_file -------------------------------------------------------------

static func _quest_file(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var refs: Dictionary = ctx["refs"]
	var townees: Dictionary = ctx["townees"]
	var owners: Array = _townee_ids(ctx, true)
	if owners.is_empty():
		return {}

	var axis := ""
	if not is_valid:
		var admissible: Array = []
		if not _owing(townees, owners).is_empty():
			admissible.append("dues")
		if not _fieldability_owners(ctx, owners).is_empty():
			admissible.append("fieldability")
		if admissible.is_empty():
			return {}
		axis = _pick_axis(rng, ctx, admissible)

	var t_id := ""
	var posting_id := ""
	if is_valid:
		var pairs := _valid_quest_pairs(ctx, owners)
		if pairs.is_empty():
			return {}
		var pair: Dictionary = pairs[rng.randi_range(0, pairs.size() - 1)]
		t_id = str(pair["townee"]); posting_id = str(pair["posting"])
		ctx["used_townees"][t_id] = true
	elif axis == "dues":
		t_id = _pick_unused(rng, _owing(townees, owners), ctx["used_townees"])
		posting_id = str((townees[t_id]["owns"] as Array)[0])
	else:  # fieldability
		var fowners := _fieldability_owners(ctx, owners)
		var fo: Dictionary = fowners[rng.randi_range(0, fowners.size() - 1)]
		t_id = str(fo["townee"]); posting_id = str(fo["posting"])
		ctx["used_townees"][t_id] = true

	var t: Dictionary = townees[t_id]
	var posting: Dictionary = refs["postings"].get(posting_id, {})
	var ward: Array = posting.get("ward_required", [])
	var summary := "Files the %s for posting." % Loc.humanize(posting_id)
	var asserts := { "posting": posting_id }
	if not ward.is_empty():
		asserts["ward_required"] = ward
	var checks: Array = []
	var truth: Dictionary
	var fscale := _decoy_scale("filing")
	var glass := "A filing slip, not a specimen — only ink under the lens."

	if is_valid:
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "dues current"))
		checks.append(_chk("posting", posting_id, ["posting open"], "the filing", "open"))
		checks.append(_chk("roster", "*", ["an eligible party is active"], "active parties", "match: a party can field it"))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null }
	elif axis == "dues":
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "owing — %sg outstanding" % t.get("owed", 0)))
		truth = _reject("dues", "Posting rights lapsed — %sg in dues outstanding; clear them before filing a new posting." % t.get("owed", 0))
	else:
		checks.append(_chk("posting", posting_id, ["ward_required: %s" % _fmt_arr(ward)], "the request", "ward required"))
		checks.append(_chk("roster", "*", ["wards contains %s" % _fmt_arr(ward)], "every active party", "no eligible party registered"))
		truth = _reject("fieldability", "No %s-warded party is registered as active — no one on the roster could take it, whoever comes or goes." % _fmt_arr(ward))

	var insp := _insp(glass, false, fscale[0], fscale[1], fscale[2], false)
	var story := "Item n/a, rank n/a. %s %s" % [
		("Dues current; the roster can field it." if is_valid else ("Dues owing — no new post." if axis == "dues" else "No warded party on the books.")),
		("Stamp FILED." if is_valid else "Stamp REJECTED.")]
	return _visit(str(t["name"]), "townee", str(t.get("profession", "Petitioner")), "quest_file",
		summary, asserts, truth, checks, insp, story, "Generated quest_file (%s)." % (axis if not is_valid else "clean"))


## Owners with a current-dues, roster-fieldable owned posting (non-ward, or ward the roster
## can satisfy). Returns [{townee, posting}].
static func _valid_quest_pairs(ctx: Dictionary, owners: Array) -> Array:
	var refs: Dictionary = ctx["refs"]
	var townees: Dictionary = ctx["townees"]
	var roster_wards := _roster_wards(refs)
	var out: Array = []
	for tid in owners:
		if str(townees[tid].get("dues")) != "current":
			continue
		for pid in townees[tid]["owns"]:
			var p: Dictionary = refs["postings"].get(pid, {})
			var ward: Array = p.get("ward_required", [])
			if ward.is_empty() or _wards_satisfied(ward, roster_wards):
				out.append({ "townee": tid, "posting": pid })
	return out


## Current-dues owners whose owned posting needs a ward the roster CANNOT satisfy.
static func _fieldability_owners(ctx: Dictionary, owners: Array) -> Array:
	var refs: Dictionary = ctx["refs"]
	var townees: Dictionary = ctx["townees"]
	var roster_wards := _roster_wards(refs)
	var out: Array = []
	for tid in owners:
		if str(townees[tid].get("dues")) != "current":
			continue
		for pid in townees[tid]["owns"]:
			var ward: Array = refs["postings"].get(pid, {}).get("ward_required", [])
			if not ward.is_empty() and not _wards_satisfied(ward, roster_wards):
				out.append({ "townee": tid, "posting": pid })
	return out


static func _roster_wards(refs: Dictionary) -> Dictionary:
	var have := {}
	for p in refs.get("roster", {}).get("parties", []):
		if p is Dictionary:
			for w in p.get("wards", []):
				have[str(w)] = true
	return have


## A ward_required is satisfied if ANY listed ward is on the roster (cleric OR water, etc.).
static func _wards_satisfied(ward_required: Array, have: Dictionary) -> bool:
	for w in ward_required:
		if have.has(str(w)):
			return true
	return false


# --- completion_claim -------------------------------------------------------

static func _completion_claim(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var tokens: Array = ctx["tokens"]
	if tokens.is_empty():
		return {}
	var tok: Dictionary = tokens[rng.randi_range(0, tokens.size() - 1)]
	var token_id := str(tok["id"])
	var posting_id := str(tok["posting"])
	var assigned := str(tok["assigned"])

	var axis := ""
	if not is_valid:
		axis = _pick_axis(rng, ctx, ["claimant", "authenticity"])

	var name := _walkin_name(rng, ctx)
	var claimant := assigned
	var glass := "A completion slip under a genuine guild seal — the wax unbroken, the impression clean."
	var glass_rel := false
	var checks: Array = []
	var truth: Dictionary

	if is_valid:
		checks.append(_chk("archive", token_id, ["seal genuine"], "the token", "match"))
		checks.append(_chk("posting", posting_id, ["assigned_to: %s" % Loc.humanize(assigned)], "the payee", "match: the assigned party claims it"))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null }
	elif axis == "claimant":
		claimant = _other_party(rng, ctx, assigned)
		checks.append(_chk("archive", token_id, ["seal genuine"], "the token", "match"))
		checks.append(_chk("posting", posting_id, ["assigned_to: %s" % Loc.humanize(assigned)], "the payee", "mismatch: payee is not the assigned party"))
		# A hold under four-verdict; a reject under STRICT_BINARY (mirrors ivy-threnody).
		truth = { "valid": false, "stamp": "hold", "binary": "reject",
			"failure": { "axis": "claimant", "reason": "The token is real, but the quest was assigned to %s — right proof, wrong claimant." % Loc.humanize(assigned) } }
	else:  # authenticity
		glass = "The completion seal is broken and re-pressed — the wax cracked, the impression doubled."
		glass_rel = true
		checks.append(_chk("archive", token_id, ["seal genuine"], "the token", "mismatch: seal broken, re-pressed"))
		truth = _reject("authenticity", "The completion seal is forged — broken and re-pressed. No genuine slip backs it in the archive.")

	var dscale := _decoy_scale("completion_token")
	var summary := "Brings a completion token for the %s and asks the bounty be paid out." % Loc.humanize(posting_id)
	var asserts := { "posting": posting_id, "token": token_id, "claimant": claimant }
	var insp := _insp(glass, glass_rel, dscale[0], dscale[1], dscale[2], false)
	var story := "Match the token's seal, then the assigned party. %s" % (
		"Both hold — stamp PAID." if is_valid else ("Right seal, wrong party — stamp HELD." if axis == "claimant" else "Seal forged — stamp REJECTED."))
	return _visit(name, "townee", "Claimant", "completion_claim", summary, asserts, truth, checks, insp, story,
		"Generated completion_claim (%s)." % (axis if not is_valid else "clean"))


static func _other_party(rng: RandomNumberGenerator, ctx: Dictionary, not_this: String) -> String:
	var ids: Array = []
	for p in ctx["refs"].get("roster", {}).get("parties", []):
		if p is Dictionary and str(p.get("id")) != not_this:
			ids.append(str(p["id"]))
	if ids.is_empty():
		return not_this
	return str(ids[rng.randi_range(0, ids.size() - 1)])


# --- rank_up ----------------------------------------------------------------

static func _rank_up(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var refs: Dictionary = ctx["refs"]
	var advs: Dictionary = ctx["adventurers"]
	var thresholds: Dictionary = refs.get("rankup_thresholds", {})
	var all_adv: Array = _adventurer_ids(ctx)

	var axis := ""
	if not is_valid:
		var admissible: Array = []
		if not _dup_adventurers(ctx).is_empty():
			admissible.append("duplicate")
		if not _underthreshold_adventurers(ctx).is_empty():
			admissible.append("rank")
		if not _owing(advs, all_adv).is_empty():
			admissible.append("dues")
		if admissible.is_empty():
			return {}
		axis = _pick_axis(rng, ctx, admissible)

	var adv_id := ""
	if is_valid:
		adv_id = _pick_unused(rng, _valid_rankup_adventurers(ctx), ctx["used_adventurers"])
	elif axis == "duplicate":
		adv_id = _pick_unused(rng, _dup_adventurers(ctx), ctx["used_adventurers"])
	elif axis == "rank":
		adv_id = _pick_unused(rng, _underthreshold_adventurers(ctx), ctx["used_adventurers"])
	else:
		# Prefer an owing adventurer whose logbook is otherwise valid (a pure dues fail).
		var owing := _owing(advs, all_adv)
		adv_id = _pick_unused(rng, owing, ctx["used_adventurers"])
	if adv_id == "":
		return {}

	var adv: Dictionary = advs[adv_id]
	var lb: Dictionary = adv["logbook"]
	var from := str(lb["from"]); var to := str(lb["to"])
	var threshold := int(thresholds.get("%s->%s" % [from, to], 3))
	var archive_id := str(lb["archive_id"])
	var entries := int(lb["entries"]); var distinct := int(lb["distinct_seals"])

	var summary := "A rank-up application, %s to %s, with a logbook of sealed completions." % [Loc.humanize(from), Loc.humanize(to)]
	var asserts := { "from": from, "to": to, "logged_completions": entries }
	var checks: Array = []
	var glass := "%d slips, %d distinct seals — each grain its own." % [entries, distinct]
	var glass_rel := false
	var truth: Dictionary
	var dscale := _decoy_scale("logbook")

	if is_valid:
		checks.append(_chk("archive", archive_id, ["%d distinct seals" % distinct, "all his"], "archived slips", "match"))
		checks.append(_chk("adventurer_directory", adv_id, ["%s->%s: %d" % [from, to, threshold]], "%d completions" % distinct, "meets"))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null }
	elif axis == "duplicate":
		glass = "%d slips — but two seals share one grain, struck from a single die; only %d are distinct." % [entries, distinct]
		glass_rel = true
		checks.append(_chk("archive", archive_id, ["%d entries" % entries, "distinct seals"], "the logbook", "mismatch: two seals identical"))
		truth = _reject("duplicate", "Padded logbook: two entries carry one seal impression — only %d distinct completions exist, short of the %d needed." % [distinct, threshold])
	elif axis == "rank":
		checks.append(_chk("archive", archive_id, ["%d distinct seals" % distinct], "the logbook", "match, but too few"))
		checks.append(_chk("adventurer_directory", adv_id, ["%s->%s: %d" % [from, to, threshold]], "%d completions" % distinct, "below threshold"))
		truth = _reject("rank", "Honest work, but %d sealed completions is short of the %d the %s->%s step requires." % [distinct, threshold, Loc.humanize(from), Loc.humanize(to)])
	else:  # dues
		checks.append(_chk("archive", archive_id, ["%d distinct seals" % distinct], "the logbook", "match"))
		checks.append(_chk("adventurer_directory", adv_id, ["dues current"], "the directory", "owing — %sg outstanding" % adv.get("owed", 0)))
		truth = _reject("dues", "The logbook meets the threshold, but guild dues are %sg in arrears — settle them before the promotion is stamped." % adv.get("owed", 0))

	var insp := _insp(glass, glass_rel, dscale[0], dscale[1], dscale[2], false)
	var story := "Count the sealed entries against the archive. %s" % (
		"All distinct, threshold met — stamp PROMOTED." if is_valid else "Stamp DENIED.")
	return _visit(str(adv["name"]), "adventure", str(adv.get("profession", "Adventurer")), "rank_up",
		summary, asserts, truth, checks, insp, story, "Generated rank_up (%s)." % (axis if not is_valid else "clean"))


static func _valid_rankup_adventurers(ctx: Dictionary) -> Array:
	var thresholds: Dictionary = ctx["refs"].get("rankup_thresholds", {})
	var out: Array = []
	for aid in ctx["adventurers"].keys():
		var adv: Dictionary = ctx["adventurers"][aid]
		if str(adv.get("dues")) != "current":
			continue
		var lb: Dictionary = adv["logbook"]
		var thr := int(thresholds.get("%s->%s" % [lb["from"], lb["to"]], 3))
		if int(lb["distinct_seals"]) >= thr and int(lb["distinct_seals"]) == int(lb["entries"]):
			out.append(aid)
	return out


static func _dup_adventurers(ctx: Dictionary) -> Array:
	var out: Array = []
	for aid in ctx["adventurers"].keys():
		var lb: Dictionary = ctx["adventurers"][aid]["logbook"]
		if int(lb["distinct_seals"]) < int(lb["entries"]):
			out.append(aid)
	return out


static func _underthreshold_adventurers(ctx: Dictionary) -> Array:
	var thresholds: Dictionary = ctx["refs"].get("rankup_thresholds", {})
	var out: Array = []
	for aid in ctx["adventurers"].keys():
		var lb: Dictionary = ctx["adventurers"][aid]["logbook"]
		var thr := int(thresholds.get("%s->%s" % [lb["from"], lb["to"]], 3))
		if int(lb["entries"]) < thr:
			out.append(aid)
	return out


# --- roster_change ----------------------------------------------------------

static func _roster_change(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var refs: Dictionary = ctx["refs"]
	var advs: Dictionary = ctx["adventurers"]
	var ciphers: Dictionary = refs.get("cipher_table", {})
	var all_adv: Array = _adventurer_ids(ctx)

	var axis := ""
	if not is_valid:
		var admissible: Array = ["authenticity"]
		if not _owing(advs, all_adv).is_empty():
			admissible.append("dues")
		axis = _pick_axis(rng, ctx, admissible)

	var adv_id := ""
	if not is_valid and axis == "dues":
		adv_id = _pick_unused(rng, _owing(advs, all_adv), ctx["used_adventurers"])
	elif is_valid:
		adv_id = _pick_unused(rng, _current(advs, all_adv), ctx["used_adventurers"])
	else:
		adv_id = _pick_unused(rng, all_adv, ctx["used_adventurers"])
	if adv_id == "":
		return {}

	var adv: Dictionary = advs[adv_id]
	var chapter := str(adv["chapter"])
	var cipher: Dictionary = ciphers.get(chapter, {})
	var summary := "Presents a transfer card from the %s chapter; asks to be entered on the roster as %s." % [Loc.humanize(chapter), Loc.humanize(str(adv["rank"]))]
	var asserts := { "action": "welcome", "chapter": chapter, "rank": str(adv["rank"]) }
	var checks: Array = []
	var glass := str(cipher.get("glass", ""))
	var glass_rel := false
	var truth: Dictionary
	var dscale := _decoy_scale("transfer_seal")

	if is_valid:
		glass_rel = true
		checks.append(_chk("cipher", chapter, ["mark: %s" % cipher.get("mark", ""), "seal: %s" % cipher.get("seal", "")], "the card", "match"))
		checks.append(_chk("adventurer_directory", adv_id, ["dues stamped current"], "the card", "match"))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null,
			"roster_write": { "party": adv_id, "rank": str(adv["rank"]), "wards": adv.get("wards", []) } }
	elif axis == "dues":
		checks.append(_chk("cipher", chapter, ["mark: %s" % cipher.get("mark", "")], "the card", "match"))
		checks.append(_chk("adventurer_directory", adv_id, ["dues stamped current"], "the card", "owing — %sg outstanding" % adv.get("owed", 0)))
		truth = _reject("dues", "The cipher matches, but the card's dues stamp is stale — %sg outstanding. No roster-write until the dues clear." % adv.get("owed", 0))
	else:  # authenticity — a composed forged reading (borrow another chapter's mark)
		var other := _other_chapter(rng, ciphers, chapter)
		var other_mark := str(ciphers.get(other, {}).get("mark", "a foreign mark"))
		glass = "The mark is a %s pressed in wax where the %s's %s belongs." % [other_mark, Loc.humanize(chapter), cipher.get("mark", "")]
		glass_rel = true
		checks.append(_chk("cipher", chapter, ["mark: %s" % cipher.get("mark", ""), "seal: %s" % cipher.get("seal", "")], "the card", "mismatch: mark is a %s, not the %s's" % [other_mark, Loc.humanize(chapter)]))
		truth = _reject("authenticity", "Forged card: the seal carries a %s, not the %s's %s — an impostor claiming a chapter that never sealed it." % [other_mark, Loc.humanize(chapter), cipher.get("mark", "")])

	var insp := _insp(glass, glass_rel, dscale[0], dscale[1], dscale[2], false)
	var story := "Cipher table to %s: %s in wax. Read the card's seal. %s" % [
		Loc.humanize(chapter), cipher.get("mark", ""),
		("Match — copy name, rank, wards into the roster; stamp ENROLLED." if is_valid else "Stamp REJECTED.")]
	return _visit(str(adv["name"]), "adventure", str(adv.get("profession", "Adventurer")), "roster_change",
		summary, asserts, truth, checks, insp, story, "Generated roster_change (%s)." % (axis if not is_valid else "clean"))


static func _other_chapter(rng: RandomNumberGenerator, ciphers: Dictionary, not_this: String) -> String:
	var ids: Array = []
	for c in ciphers.keys():
		if not c.begins_with("_") and c != not_this:
			ids.append(c)
	if ids.is_empty():
		return not_this
	return str(ids[rng.randi_range(0, ids.size() - 1)])


# --- dungeon_drop -----------------------------------------------------------

static func _dungeon_drop(is_valid: bool, rng: RandomNumberGenerator, ctx: Dictionary) -> Dictionary:
	var refs: Dictionary = ctx["refs"]
	var townees: Dictionary = ctx["townees"]
	var season := str(ctx["season"])
	var max_reach := int(ctx["max_reach"])
	var all_townees: Array = _townee_ids(ctx, false)
	var in_season := _drops_in_season(ctx, season)

	var axis := ""
	if not is_valid:
		var admissible: Array = ["identity"]
		if not _owing(townees, all_townees).is_empty():
			admissible.append("dues")
		if not _drops_out_of_season(ctx, season, max_reach).is_empty():
			admissible.append("season")
		if not _drops_unreachable(ctx, season, max_reach).is_empty():
			admissible.append("reach")
		axis = _pick_axis(rng, ctx, admissible)

	# Actor.
	var t_id := ""
	if not is_valid and axis == "dues":
		t_id = _pick_unused(rng, _owing(townees, all_townees), ctx["used_townees"])
	else:
		t_id = _pick_unused(rng, _current(townees, all_townees), ctx["used_townees"])
	if t_id == "":
		t_id = _pick_unused(rng, all_townees, ctx["used_townees"])
	var t: Dictionary = townees[t_id]

	var checks: Array = []
	var truth: Dictionary
	var fscale := _decoy_scale("filing")
	var glass := "A commission slip — the drop itself is still down in the deep."
	var item := ""

	if is_valid:
		var d: Dictionary = _reachable_in_season(ctx, season, max_reach, rng)
		if d.is_empty():
			return {}   # no valid drop this season/reach; caller retries as clean item_check
		item = str(d["id"])
		var fl := int(d["floor"]); var base := int(d["base"])
		var quote := _quote(refs, base, fl)
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "dues current"))
		checks.append(_chk("drop_table", item, ["is_drop: true", "floor: %d" % fl], "the commission", "genuine drop, Floor %d" % fl))
		checks.append(_chk("season", item, ["season: %s" % season, "current: %s" % season], "the season wheel", "in season"))
		checks.append(_chk("roster", "*", ["reach_floor >= %d" % fl], "active parties", "match: a party reaches Floor %d" % max_reach))
		truth = { "valid": true, "stamp": "approve", "binary": "approve", "failure": null, "quote": quote }
	elif axis == "dues":
		item = str(in_season[rng.randi_range(0, in_season.size() - 1)]["id"]) if not in_season.is_empty() else str(ctx["drops"][0]["id"])
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "owing — %sg outstanding" % t.get("owed", 0)))
		truth = _reject("dues", "Posting rights lapsed — %sg in dues outstanding; the commission cannot be quoted until the account is cleared." % t.get("owed", 0))
	elif axis == "season":
		var d2: Dictionary = _pick_drop(rng, _drops_out_of_season(ctx, season, max_reach))
		item = str(d2["id"])
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "dues current"))
		checks.append(_chk("drop_table", item, ["is_drop: true"], "the commission", "genuine drop"))
		checks.append(_chk("season", item, ["season: %s" % d2["season"], "current: %s" % season], "the season wheel", "out of season — short-circuits the quote"))
		truth = _reject("season", "A real drop, but out of season — %s only forms in %s, and it is %s. No quote." % [Loc.humanize(item), Loc.humanize(str(d2["season"])), Loc.humanize(season)])
	elif axis == "reach":
		var d3: Dictionary = _pick_drop(rng, _drops_unreachable(ctx, season, max_reach))
		item = str(d3["id"])
		var fl := int(d3["floor"])
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "dues current"))
		checks.append(_chk("drop_table", item, ["is_drop: true", "floor: %d" % fl], "the drop table", "Floor %d" % fl))
		checks.append(_chk("roster", "*", ["reach_floor >= %d" % fl], "active parties", "no party reaches Floor %d (deepest is %d)" % [fl, max_reach]))
		truth = _reject("reach", "In season, but on Floor %d — the deepest active party reaches Floor %d. Beyond current reach; no quote." % [fl, max_reach])
	else:  # identity — a shop-craftable item that is not a genuine drop
		var fake := _fake_drop_item(rng, ctx)
		item = fake
		checks.append(_chk("townee_directory", t_id, ["dues current"], "the directory", "dues current"))
		checks.append(_chk("book", item, ["shop-craftable"], "the commission", "a bench-craftable item — not a dungeon drop"))
		truth = _reject("identity", "%s is shop-craftable, not a genuine dungeon drop — nothing to fetch from a floor; refer it to a craftsman." % Loc.humanize(item))

	var summary := "Commissions the guild to fetch \"%s\" and asks what it will cost." % Loc.humanize(item)
	var asserts := { "item": item, "action": "commission" }
	var insp := _insp(glass, false, fscale[0], fscale[1], fscale[2], false)
	var story := "Drop pipeline: real drop? in season? reachable? %s" % (
		"All clear — stamp QUOTED." if is_valid else "A gate fails — stamp DECLINED.")
	return _visit(str(t["name"]), "townee", str(t.get("profession", "Commissioner")), "dungeon_drop",
		summary, asserts, truth, checks, insp, story, "Generated dungeon_drop (%s)." % (axis if not is_valid else "clean"))


static func _drops_in_season(ctx: Dictionary, season: String) -> Array:
	var out: Array = []
	for d in ctx["drops"]:
		if str(d["season"]) == season:
			out.append(d)
	return out


static func _drops_out_of_season(ctx: Dictionary, season: String, max_reach: int) -> Array:
	# Out-of-season, but reachable — so the season axis is isolated (not confounded by reach).
	var out: Array = []
	for d in ctx["drops"]:
		if str(d["season"]) != season and int(d["floor"]) <= max_reach:
			out.append(d)
	return out


static func _drops_unreachable(ctx: Dictionary, season: String, max_reach: int) -> Array:
	# In-season, but too deep — so the reach axis is isolated (season passes).
	var out: Array = []
	for d in ctx["drops"]:
		if str(d["season"]) == season and int(d["floor"]) > max_reach:
			out.append(d)
	return out


static func _reachable_in_season(ctx: Dictionary, season: String, max_reach: int, rng: RandomNumberGenerator) -> Dictionary:
	var pool: Array = []
	for d in ctx["drops"]:
		if str(d["season"]) == season and int(d["floor"]) <= max_reach:
			pool.append(d)
	if pool.is_empty():
		return {}
	return pool[rng.randi_range(0, pool.size() - 1)]


static func _pick_drop(rng: RandomNumberGenerator, pool: Array) -> Dictionary:
	if pool.is_empty():
		return {}
	return pool[rng.randi_range(0, pool.size() - 1)]


## A book item that is NOT a genuine drop (used for the dungeon identity fail). Prefers a
## mineral/relic — plausibly "shop-craftable" — and never one that shares a drop id.
static func _fake_drop_item(rng: RandomNumberGenerator, ctx: Dictionary) -> String:
	var book: Dictionary = ctx["refs"]["book"]
	var drop_ids := {}
	for d in ctx["drops"]:
		drop_ids[str(d["id"])] = true
	var pool: Array = []
	for bid in book.keys():
		if bid.begins_with("_") or drop_ids.has(bid):
			continue
		var cat := str(book[bid].get("category", ""))
		if cat == "mineral" or cat == "relic" or cat == "reagent":
			pool.append(bid)
	if pool.is_empty():
		return "moonstone"
	return str(pool[rng.randi_range(0, pool.size() - 1)])


static func _quote(refs: Dictionary, base: int, depth: int) -> Dictionary:
	var payout: Dictionary = refs.get("payout", {})
	var premium := int(payout.get("in_season_premium", 10))
	var mult := 1.0 + 0.25 * float(depth)
	var total := int(round(float(base) * mult)) + premium
	return { "base": base, "depth_multiplier": mult, "in_season_premium": premium, "total": total, "currency": "g" }


# --- assembly ---------------------------------------------------------------

static func _chk(consult: String, entry: String, compare: Array, against: String, result: String) -> Dictionary:
	return { "consult": consult, "entry": entry, "compare": compare, "against": against, "result": result }


static func _reject(axis: String, reason: String) -> Dictionary:
	return { "valid": false, "stamp": "reject", "binary": "reject", "failure": { "axis": axis, "reason": reason } }


static func _visit(name: String, affiliation: String, profession: String, task_type: String,
		summary: String, asserts: Dictionary, truth: Dictionary, checks: Array,
		inspections: Dictionary, story: String, notes: String) -> Dictionary:
	return {
		"name": name,
		"affiliation": affiliation,
		"profession": profession,
		"task_type": task_type,
		"claim": { "summary": summary, "asserts": asserts },
		"truth": truth,
		"checks": checks,
		"inspections": inspections,
		"player_story": story,
		"notes": notes,
	}


static func _fmt_arr(a) -> String:
	if not (a is Array) or (a as Array).is_empty():
		return "—"
	var parts: Array = []
	for e in a:
		parts.append(Loc.humanize(str(e)))
	return " / ".join(parts)

extends RefCounted
## GTH · Capturer — token-frugal viewport capture. Returns images BY REFERENCE (path +
## metadata), not inline. Levers: settle-then-shoot (wait for the frame to stop changing),
## `if_changed` dedup (sha256 + 8×8 average perceptual hash), downscale / region / element
## crop, optional annotate, and an appended `manifest.jsonl`. Writes under the configured
## artifacts dir (globalized to an absolute path so an external reader can open it).
##
## Pure-engine: NO project-specific references — reusable across any Godot project.

var _h: Node
var _cfg: Dictionary
var _dir_abs := ""
var _session := "s"
var _seq := 0
var _last := {}        # label → {sha, phash, path}
var _global_last := {} # most recent capture of any label
var _images_written := 0

func _init(harness: Node, cfg: Dictionary) -> void:
	_h = harness
	_cfg = cfg
	var res_dir: String = cfg.get("artifacts_dir", "res://.captures/gth")
	_dir_abs = ProjectSettings.globalize_path(res_dir)
	_session = str(cfg.get("session_id", "s"))

func capture(opts := {}) -> Dictionary:
	var label := str(opts.get("label", "cap"))
	var if_changed: bool = opts.get("if_changed", _cfg.get("if_changed", true))
	var settle_ms: int = int(opts.get("settle_ms", _cfg.get("settle_ms", 250)))
	if settle_ms > 0:
		await _settle(settle_ms)

	var img := _grab()
	if img == null:
		return {error = "no framebuffer (headless/dummy renderer cannot capture pixels — use a rendered session)"}

	# region / element crop (rect in px: [x,y,w,h])
	if opts.has("region"):
		var r: Array = opts["region"]
		img = img.get_region(Rect2i(int(r[0]), int(r[1]), int(r[2]), int(r[3])))

	var sha := _sha256(img)
	var phash := _phash(img)
	var has_annotate := opts.has("annotate")
	var baseline: Dictionary = _last.get(label, _global_last)
	var dist := 64
	if not baseline.is_empty():
		dist = _hamming(phash, str(baseline.get("phash", "")))
		# `changed` is authoritative on the exact frame bytes (sha256); the perceptual
		# distance is advisory only. Visually-similar dedup is OPT-IN (`similar_ok`) so a
		# small-but-real UI change (a line of text) is never silently deduped away — a false
		# "unchanged" is the dangerous direction for a test harness.
		var identical: bool = sha == baseline.get("sha", "")
		var similar: bool = bool(opts.get("similar_ok", false)) \
			and dist <= int(opts.get("phash_threshold", _cfg.get("phash_threshold", 4)))
		if if_changed and not has_annotate and (identical or similar):
			return {changed = false, phash_distance = dist, path = baseline.get("path", ""),
				w = int(img.get_width()), h = int(img.get_height()),
				note = "unchanged vs previous (deduped, no new file)"}

	# downscale (long-edge cap)
	var max_dim: int = int(opts.get("max_dim", _cfg.get("max_dim", 1280)))
	if max_dim > 0 and max(img.get_width(), img.get_height()) > max_dim:
		var s := float(max_dim) / float(max(img.get_width(), img.get_height()))
		img.resize(int(img.get_width() * s), int(img.get_height() * s), Image.INTERPOLATE_BILINEAR)

	# annotate: point [x,y] and/or rect [x,y,w,h] in (possibly downscaled) px
	if opts.has("annotate"):
		_annotate(img, opts["annotate"])

	var fmt := str(opts.get("format", _cfg.get("format", "png")))
	DirAccess.make_dir_recursive_absolute(_dir_abs.path_join(_session))
	_seq += 1
	var fname := "%03d-%s.%s" % [_seq, _sanitize(label), fmt]
	var path := _dir_abs.path_join(_session).path_join(fname)
	var bytes := 0
	if fmt == "jpg" or fmt == "jpeg":
		img.save_jpg(path, float(_cfg.get("jpeg_quality", 0.8)))
	else:
		img.save_png(path)
	var fa := FileAccess.open(path, FileAccess.READ)
	if fa != null:
		bytes = fa.get_length()
		fa.close()

	_images_written += 1
	var rec := {seq = _seq, label = label, path = path, w = img.get_width(), h = img.get_height(),
		bytes = bytes, sha256 = sha, phash = phash, changed = true, phash_distance = dist}
	_last[label] = {sha = sha, phash = phash, path = path}
	_global_last = _last[label]
	_append_manifest(rec)

	var budget: int = int(_cfg.get("image_budget", 60))
	if _images_written > budget:
		rec["budget_warning"] = "session image budget (%d) exceeded" % budget
	return rec

# --- settle -------------------------------------------------------------------------------

func _settle(timeout_ms: int) -> void:
	var interval := float(_cfg.get("settle_interval_ms", 60)) / 1000.0
	var deadline := float(timeout_ms) / 1000.0
	var elapsed := 0.0
	var prev := _phash_small()
	var stable := 0
	while elapsed < deadline:
		await _h.get_tree().create_timer(interval).timeout
		elapsed += interval
		var cur := _phash_small()
		if _hamming(prev, cur) <= 1:
			stable += 1
			if stable >= 2:
				return
		else:
			stable = 0
		prev = cur

# --- pixels -------------------------------------------------------------------------------

func _grab() -> Image:
	var tex := _h.get_tree().root.get_texture()
	if tex == null:
		return null
	return tex.get_image()

func _phash_small() -> String:
	var img := _grab()
	if img == null:
		return ""
	return _phash(img)

func _phash(src: Image) -> String:
	var im := src.duplicate() as Image
	im.resize(8, 8, Image.INTERPOLATE_BILINEAR)
	im.convert(Image.FORMAT_L8)
	var vals := []
	var sum := 0.0
	for y in 8:
		for x in 8:
			var v := im.get_pixel(x, y).r
			vals.append(v)
			sum += v
	var mean := sum / 64.0
	var b := PackedByteArray()
	b.resize(8)
	for i in 64:
		if vals[i] > mean:
			b[i / 8] |= (1 << (i % 8))
	return b.hex_encode()

func _hamming(a: String, b: String) -> int:
	if a == "" or b == "" or a.length() != b.length():
		return 64
	var ba := _hex_to_bytes(a)
	var bb := _hex_to_bytes(b)
	var d := 0
	for i in ba.size():
		var x := ba[i] ^ bb[i]
		while x != 0:
			d += x & 1
			x >>= 1
	return d

func _hex_to_bytes(h: String) -> PackedByteArray:
	var out := PackedByteArray()
	var i := 0
	while i + 1 < h.length():
		out.append(("0x" + h.substr(i, 2)).hex_to_int())
		i += 2
	return out

func _sha256(img: Image) -> String:
	var ctx := HashingContext.new()
	ctx.start(HashingContext.HASH_SHA256)
	ctx.update(img.get_data())
	return ctx.finish().hex_encode()

# --- annotate -----------------------------------------------------------------------------

func _annotate(img: Image, a: Dictionary) -> void:
	var col := Color(1, 0.25, 0.15)
	if a.has("point"):
		var p: Array = a["point"]
		_cross(img, Vector2i(int(p[0]), int(p[1])), col, 9)
	if a.has("rect"):
		var r: Array = a["rect"]
		_rect_outline(img, Rect2i(int(r[0]), int(r[1]), int(r[2]), int(r[3])), col)

func _cross(img: Image, c: Vector2i, col: Color, r: int) -> void:
	for d in range(-r, r + 1):
		_plot(img, Vector2i(c.x + d, c.y), col)
		_plot(img, Vector2i(c.x, c.y + d), col)

func _rect_outline(img: Image, rect: Rect2i, col: Color) -> void:
	for x in range(rect.position.x, rect.position.x + rect.size.x):
		_plot(img, Vector2i(x, rect.position.y), col)
		_plot(img, Vector2i(x, rect.position.y + rect.size.y - 1), col)
	for y in range(rect.position.y, rect.position.y + rect.size.y):
		_plot(img, Vector2i(rect.position.x, y), col)
		_plot(img, Vector2i(rect.position.x + rect.size.x - 1, y), col)

func _plot(img: Image, p: Vector2i, col: Color) -> void:
	if p.x >= 0 and p.y >= 0 and p.x < img.get_width() and p.y < img.get_height():
		img.set_pixelv(p, col)

# --- manifest -----------------------------------------------------------------------------

func _append_manifest(rec: Dictionary) -> void:
	var mpath := _dir_abs.path_join(_session).path_join("manifest.jsonl")
	var f: FileAccess
	if FileAccess.file_exists(mpath):
		f = FileAccess.open(mpath, FileAccess.READ_WRITE)
		f.seek_end()
	else:
		f = FileAccess.open(mpath, FileAccess.WRITE)
	if f != null:
		f.store_line(JSON.stringify(rec))
		f.close()

func _sanitize(s: String) -> String:
	var out := ""
	for c in s:
		out += c if c.is_valid_identifier() or c.is_valid_int() or c == "-" else "-"
	return out if out != "" else "cap"

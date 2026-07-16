class_name Sparkline
extends Control
## A tiny trailing-series sparkline for the inspector's pressure drives.
## Values are expected in [0,1]; it draws a baseline at 0.5 and the series polyline.

var values: PackedFloat32Array = PackedFloat32Array()
var line_color: Color = Color(0.37, 0.70, 0.83)

func set_values(v: Array) -> void:
	values = PackedFloat32Array()
	for x in v:
		values.append(float(x))
	queue_redraw()

func _draw() -> void:
	var w := size.x
	var h := size.y
	draw_rect(Rect2(Vector2.ZERO, size), Color(0.11, 0.14, 0.18))
	# Midline (0.5) — the reference the series is read AGAINST, so it has to be visible.
	# At the original alpha 0.12 it rendered RGB(55,62,71) on the box's RGB(28,35,46): a
	# contrast of 1.46:1, measured off a real capture, where WCAG asks 3:1 of a graphical
	# object you need in order to read the content. A 1px line at 1.46:1 is not subtle, it
	# is absent — and these boxes get published as PNGs that may be downscaled, which only
	# ever costs a thin low-contrast line more. 0.35 measures 3.17:1 and still sits well
	# under the series colour (0.37,0.70,0.83), so the polyline stays the dominant mark.
	var mid := h - 0.5 * h
	draw_line(Vector2(0, mid), Vector2(w, mid), Color(1, 1, 1, 0.35), 1.0)
	if values.size() < 2:
		return
	var pts := PackedVector2Array()
	var n := values.size()
	for i in range(n):
		var x := w * float(i) / float(n - 1)
		var y := h - clampf(values[i], 0.0, 1.0) * h
		pts.append(Vector2(x, y))
	draw_polyline(pts, line_color, 1.5, true)

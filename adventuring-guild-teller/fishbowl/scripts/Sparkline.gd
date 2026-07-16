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
	# midline (0.5)
	var mid := h - 0.5 * h
	draw_line(Vector2(0, mid), Vector2(w, mid), Color(1, 1, 1, 0.12), 1.0)
	if values.size() < 2:
		return
	var pts := PackedVector2Array()
	var n := values.size()
	for i in range(n):
		var x := w * float(i) / float(n - 1)
		var y := h - clampf(values[i], 0.0, 1.0) * h
		pts.append(Vector2(x, y))
	draw_polyline(pts, line_color, 1.5, true)

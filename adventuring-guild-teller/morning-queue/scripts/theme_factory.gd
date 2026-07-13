class_name ThemeFactory
extends RefCounted
## Code-built Godot Theme for The Morning Queue — makes the whole desk read as one
## parchment surface. Every color comes from the frozen Palette token set; nothing is
## hardcoded, so the components never fork the values. The integrator calls
## `ThemeFactory.build()` from Main and assigns the result as the UI theme.
##
## Everything here is authored in code (StyleBoxFlat.new(), set_stylebox / set_color /
## set_font_size) by design — there is no hand-authored .tres to drift out of sync.

const BASE_FONT_SIZE := 16   ## body text on the counter
const SMALL_FONT_SIZE := 14  ## tab labels, secondary chrome
const PANEL_RADIUS := 4      ## card corner rounding
const PANEL_MARGIN := 10     ## inner padding for panels
const BUTTON_RADIUS := 5


## Assemble the parchment desk theme.
static func build() -> Theme:
	var theme := Theme.new()
	theme.default_font_size = BASE_FONT_SIZE

	_apply_label(theme)
	_apply_panel(theme)
	_apply_button(theme)
	_apply_tabs(theme)

	return theme


# --- Label: primary ink text on parchment ---------------------------------------
static func _apply_label(theme: Theme) -> void:
	theme.set_color("font_color", "Label", Palette.INK)
	theme.set_font_size("font_size", "Label", BASE_FONT_SIZE)


# --- PanelContainer / Panel "panel": a card sitting on the desk ------------------
static func _apply_panel(theme: Theme) -> void:
	var card := _flat(Palette.SURFACE, Palette.LINE, 1, PANEL_RADIUS, PANEL_MARGIN)
	# Same surface for both container kinds so nested panels read as one sheet.
	theme.set_stylebox("panel", "PanelContainer", card)
	theme.set_stylebox("panel", "Panel", card.duplicate() as StyleBoxFlat)


# --- Button: approval-green family with a satisfying pressed inset ----------------
static func _apply_button(theme: Theme) -> void:
	# Base green button; extra horizontal padding for a stamp-like target.
	var normal := _flat(Palette.GREEN, Palette.GREEN.darkened(0.18), 1, BUTTON_RADIUS, 8)
	normal.content_margin_left = 14
	normal.content_margin_right = 14

	var hover := normal.duplicate() as StyleBoxFlat
	hover.bg_color = Palette.GREEN.lightened(0.08)
	hover.border_color = Palette.GREEN.darkened(0.10)

	# Pressed: darker face + the label nudged down/in so the key feels depressed.
	var pressed := normal.duplicate() as StyleBoxFlat
	pressed.bg_color = Palette.GREEN.darkened(0.16)
	pressed.border_color = Palette.GREEN.darkened(0.28)
	pressed.content_margin_top = 10
	pressed.content_margin_bottom = 6

	# Disabled: green faded toward parchment, muted ink label.
	var disabled := normal.duplicate() as StyleBoxFlat
	disabled.bg_color = Palette.GREEN.lerp(Palette.GROUND, 0.62)
	disabled.border_color = Palette.LINE

	theme.set_stylebox("normal", "Button", normal)
	theme.set_stylebox("hover", "Button", hover)
	theme.set_stylebox("pressed", "Button", pressed)
	theme.set_stylebox("disabled", "Button", disabled)
	theme.set_stylebox("focus", "Button", StyleBoxEmpty.new())

	# Light parchment text reads cleanly on the green face; muted ink when disabled.
	theme.set_color("font_color", "Button", Palette.SURFACE)
	theme.set_color("font_hover_color", "Button", Palette.SURFACE)
	theme.set_color("font_pressed_color", "Button", Palette.SURFACE)
	theme.set_color("font_focus_color", "Button", Palette.SURFACE)
	theme.set_color("font_disabled_color", "Button", Palette.INK3)
	theme.set_font_size("font_size", "Button", BASE_FONT_SIZE)


# --- TabContainer: the reference booklet's dividers -------------------------------
static func _apply_tabs(theme: Theme) -> void:
	# Active tab: parchment surface with a green accent stripe along the top edge.
	var selected := _flat(Palette.SURFACE, Palette.GREEN, 0, 0, 8)
	selected.border_width_top = 2
	selected.content_margin_left = 14
	selected.content_margin_right = 14
	selected.content_margin_top = 6
	selected.content_margin_bottom = 6
	_top_corners(selected, PANEL_RADIUS)

	# Inactive tab: recessed, muted parchment.
	var unselected := _flat(Palette.GROUND, Palette.LINE, 1, 0, 8)
	unselected.border_width_bottom = 0
	unselected.content_margin_left = 14
	unselected.content_margin_right = 14
	unselected.content_margin_top = 6
	unselected.content_margin_bottom = 6
	_top_corners(unselected, PANEL_RADIUS)

	# Hovered inactive tab: warms toward the active surface.
	var hovered := unselected.duplicate() as StyleBoxFlat
	hovered.bg_color = Palette.GROUND.lightened(0.03)

	var disabled := unselected.duplicate() as StyleBoxFlat
	disabled.bg_color = Palette.GROUND

	# Content pane below the tab row.
	var pane := _flat(Palette.SURFACE, Palette.LINE, 1, PANEL_RADIUS, PANEL_MARGIN)
	# Tab bar strip behind the tabs.
	var tabbar := _flat(Palette.GROUND, Palette.LINE, 0, 0, 0)

	theme.set_stylebox("tab_selected", "TabContainer", selected)
	theme.set_stylebox("tab_unselected", "TabContainer", unselected)
	theme.set_stylebox("tab_hovered", "TabContainer", hovered)
	theme.set_stylebox("tab_disabled", "TabContainer", disabled)
	theme.set_stylebox("tab_focus", "TabContainer", StyleBoxEmpty.new())
	theme.set_stylebox("panel", "TabContainer", pane)
	theme.set_stylebox("tabbar_background", "TabContainer", tabbar)

	theme.set_color("font_selected_color", "TabContainer", Palette.INK)
	theme.set_color("font_unselected_color", "TabContainer", Palette.INK3)
	theme.set_color("font_hovered_color", "TabContainer", Palette.INK2)
	theme.set_color("font_disabled_color", "TabContainer", Palette.LINE2)
	theme.set_font_size("font_size", "TabContainer", SMALL_FONT_SIZE)

	# Mirror the tab styling onto the standalone TabBar so a bare tab strip matches.
	theme.set_stylebox("tab_selected", "TabBar", selected.duplicate() as StyleBoxFlat)
	theme.set_stylebox("tab_unselected", "TabBar", unselected.duplicate() as StyleBoxFlat)
	theme.set_stylebox("tab_hovered", "TabBar", hovered.duplicate() as StyleBoxFlat)
	theme.set_stylebox("tab_disabled", "TabBar", disabled.duplicate() as StyleBoxFlat)
	theme.set_color("font_selected_color", "TabBar", Palette.INK)
	theme.set_color("font_unselected_color", "TabBar", Palette.INK3)
	theme.set_color("font_hovered_color", "TabBar", Palette.INK2)
	theme.set_font_size("font_size", "TabBar", SMALL_FONT_SIZE)


# --- helpers ---------------------------------------------------------------------

## A filled card box: solid fill, uniform border, uniform rounding + inner padding.
static func _flat(bg: Color, border: Color, border_w: int, radius: int, margin: int) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = bg
	sb.set_border_width_all(border_w)
	sb.border_color = border
	sb.set_corner_radius_all(radius)
	sb.set_content_margin_all(margin)
	sb.anti_aliasing = true
	return sb


## Round only the two top corners (tabs sit flush against the pane below).
static func _top_corners(sb: StyleBoxFlat, radius: int) -> void:
	sb.corner_radius_top_left = radius
	sb.corner_radius_top_right = radius
	sb.corner_radius_bottom_left = 0
	sb.corner_radius_bottom_right = 0

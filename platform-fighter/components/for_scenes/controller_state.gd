class_name ControllerState
extends Resource


var buttons: Dictionary[String, bool] = {
	"special" : false,
	"light" : false,
	"Y" : false,
	"jump" : false,
	"grab" : false,
	"left_up" : false,
	"left_down" : false,
	"left_left" : false,
	"left_right" : false,
}

var sticks = {
	0 : Vector2.ZERO,
	1 : Vector2.ZERO
}

func get_copy()->ControllerState:
	var new = ControllerState.new()
	new.buttons = buttons.duplicate()
	new.sticks = sticks.duplicate(true)
	return new

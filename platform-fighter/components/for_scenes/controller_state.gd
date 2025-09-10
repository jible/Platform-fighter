class_name ControllerState
extends Resource


var buttons: Dictionary[String, bool] = {
	"B" : false,
	"A" : false,
	"Y" : false,
	"X" : false,
	"L" : false,
	"R" : false,
	"d_up" : false,
	"d_down" : false,
	"d_left" : false,
	"d_right" : false,
	"ZL": false,
	"ZR" : false,
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

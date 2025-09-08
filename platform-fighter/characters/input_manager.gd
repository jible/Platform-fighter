class_name InputManager
extends Node


"""
This script parses inputs and feeds them to the player

In the future it should handle them taking account for the player's specialized binds
"""

var drift_threshold = .1
@onready var current_controller_states: Array[ControllerState] = [
	ControllerState.new(),
	ControllerState.new(),
	ControllerState.new(),
]

const button_default_controls: Dictionary[int, String] ={
	0:"B",
	1:"A",
	2:"Y",
	3:"X",
	9:"L",
	10:"R",
	11:"d_up",
	12:"d_down",
	13:"d_left",
	14:"d_right",
}

const axis_default_controls: Dictionary[int, String] = {
	0: "left_horizontal",
	1: "left_vertical",
	2: "right_horizontal",
	3: "right_vertical",
	4: "ZL",
	5: "ZR",
}



func _input(event):
	var button
	if event is InputEventJoypadButton:
		button = button_default_controls.get(event.button_index, null)
		if !button: return
		current_controller_states[event.device].buttons[button] = event.pressed
	elif event is InputEventJoypadMotion:
		var axis_value = event.axis_value
		if abs(axis_value) < drift_threshold:
			axis_value = 0
		button = axis_default_controls.get(event.axis)
		if !button: return
		if button == "ZL" or button == "ZR":
			current_controller_states[event.device].buttons[button] = true if axis_value > .9 else false
		else: 
			var stick_num = floor(event.axis/2)
			var axis = 'x' if event.axis%2 == 0 else 'y'
			current_controller_states[event.device].sticks[stick_num][axis] = axis_value

func _process(_delta):
	current_controller_states[0] = current_controller_states[0].get_copy()

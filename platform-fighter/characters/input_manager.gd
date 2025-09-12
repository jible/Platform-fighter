class_name InputManager
extends Node


"""
This script parses inputs and feeds them to the player

In the future it should handle them taking account for the player's specialized binds
"""
enum button_event_type{
	PRESSED,
	RELEASED,
	STICK_MOVE,
}
var drift_threshold = .1
signal button_event(button_name: String, device: int, event_type: button_event_type, axis:Vector2)

@onready var current_controller_states: Array[ControllerState] = [
	ControllerState.new(),
	ControllerState.new(),
	ControllerState.new(),
]

const keyboard_controls: Dictionary = {
	KEY_SHIFT: "B",
	KEY_E:"A",
	KEY_SPACE:"X",
	KEY_W: "left_up",
	KEY_S: "left_down",
	KEY_A: "left_left",
	KEY_D: "left_right",
}

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
		var event_type = button_event_type.PRESSED if event.pressed else button_event_type.RELEASED
		button_event.emit(button,event.device, event_type, Vector2.ZERO)
	elif event is InputEventJoypadMotion:
		var axis_value = event.axis_value
		if abs(axis_value) < drift_threshold:
			axis_value = 0
		button = axis_default_controls.get(event.axis)
		if !button: return
		if button == "ZL" or button == "ZR":
			var pressed = true if axis_value > .9 else false
			current_controller_states[event.device].buttons[button] = pressed
			var event_type = button_event_type.PRESSED if pressed else button_event_type.RELEASED
			button_event.emit(button,event.device, event_type, Vector2.ZERO)
		
		else: 
			var stick_num = floor(event.axis/2)
			var axis = 'x' if event.axis%2 == 0 else 'y'
			current_controller_states[event.device].sticks[stick_num][axis] = axis_value
			button_event.emit(
				button,
				event.device, 
				button_event_type.STICK_MOVE, 
				current_controller_states[event.device].sticks[stick_num]
			)
	elif event is InputEventKey:
		button = keyboard_controls.get(event.keycode, null)
		var dir_keys = {
			"left_up":Vector2.UP,
			"left_down":Vector2.DOWN,
			"left_left":Vector2.LEFT,
			"left_right":Vector2.RIGHT,
		}
		if button in dir_keys.keys():
			var dir_vector = dir_keys[button]
			var stick_num = 0
			dir_vector = dir_vector * (-1 if event.is_released() else 1)
			dir_vector = current_controller_states[0].sticks[stick_num] + dir_vector
			dir_vector.x = clamp(dir_vector.x, -1, 1)
			dir_vector.y = clamp(dir_vector.y, -1, 1)
			current_controller_states[0].sticks[stick_num] = dir_vector
			return
		var event_type = button_event_type.PRESSED if event.pressed else button_event_type.RELEASED
		button_event.emit(button,event.device, event_type, Vector2.ZERO)
	
func _physics_process(_delta):
	#current_controller_states[0] = current_controller_states[0].get_copy()
	pass
	

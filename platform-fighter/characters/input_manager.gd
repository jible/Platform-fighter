class_name InputManager
extends Node

@export var play_scene_manager: PlaySceneManager
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
signal button_event(button_name: String, player_num: int, event_type: button_event_type, axis:Vector2)

@export var max_controller_buffer_size: int = 50
@export var keyboard_char_num: int = 0

@onready var current_controller_states: Array[ControllerState] = []
# Rolling array of array of controller states
var all_controller_states: Array[Array]

const keyboard_controls: Dictionary = {
	KEY_SHIFT: "B",
	KEY_E:"A",
	KEY_SPACE:"X",
}

#TODO Change these dicionaries from consts to support customization
const keyboard_direction: Dictionary = {
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

func _ready():
	for i in max_controller_buffer_size:
		pass
	for player in PlayerManager.all_players:
		current_controller_states.append(ControllerState.new())


func _input(event):
	var button
	var controller_type: PlayerProfile.ControllerType
	if event is InputEventKey:
		controller_type = PlayerProfile.ControllerType.KEYBOARD
	elif event is InputEventJoypadButton or event is InputEventJoypadMotion:
		controller_type = PlayerProfile.ControllerType.CONTROLLER
	if controller_type == null: return
	var player_number = PlayerManager.get_player_num_from_input(event.device, controller_type)
	if player_number == null: return
	if event is InputEventJoypadButton:
		controller_type = PlayerProfile.ControllerType.CONTROLLER
		button = button_default_controls.get(event.button_index, null)
		if !button: return
		current_controller_states[event.device].buttons[button] = event.pressed
		var event_type = button_event_type.PRESSED if event.pressed else button_event_type.RELEASED
		button_event.emit(button, player_number, event_type, Vector2.ZERO)
	elif event is InputEventJoypadMotion:
		controller_type = PlayerProfile.ControllerType.CONTROLLER
		
		var axis_value = event.axis_value
		if abs(axis_value) < drift_threshold:
			axis_value = 0
		button = axis_default_controls.get(event.axis)
		if !button: return
		if button == "ZL" or button == "ZR":
			var pressed = true if axis_value > .9 else false
			current_controller_states[event.device].buttons[button] = pressed
			button_event.emit(button,player_number, Vector2.ZERO)
		
		else: 
			var stick_num = floor(event.axis/2)
			var axis = 'x' if event.axis%2 == 0 else 'y'
			current_controller_states[event.device].sticks[stick_num][axis] = axis_value
			button_event.emit(
				button,
				player_number,
				button_event_type.STICK_MOVE, 
				current_controller_states[event.device].sticks[stick_num]
			)

	elif event is InputEventKey:
		controller_type = PlayerProfile.ControllerType.KEYBOARD
		
		button = keyboard_controls.get(event.keycode, null)
		if !button: button = keyboard_direction.get(event.keycode, null)
		if !button:return
		
		var dir_keys = {
			"left_up":Vector2.UP,
			"left_down":Vector2.DOWN,
			"left_left":Vector2.LEFT,
			"left_right":Vector2.RIGHT,
		}
		
		if button in dir_keys.keys():
			var stick_num = 0
			var dir_vector = Vector2.ZERO
			
			
			for dir in keyboard_direction.keys():
				if Input.is_key_pressed(dir):
					dir_vector += dir_keys[keyboard_direction[dir]]
			
			current_controller_states[player_number].sticks[stick_num] = dir_vector
			return
		var event_type = button_event_type.PRESSED if event.pressed else button_event_type.RELEASED
		button_event.emit(button,player_number, event_type, Vector2.ZERO)

func _physics_process(_delta):
	#var current_frame = play_scene_manager.get_current_play_frame()
	for controller in current_controller_states:
		controller = controller.get_copy()

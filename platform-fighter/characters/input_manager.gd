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
signal button_event(button: ControllerState.Button_Types, player_num: int, pressed: bool)

@export var max_controller_buffer_size: int = 50
@export var keyboard_char_num: int = 0

@onready var current_controller_states: Array[ControllerState] = []
# Rolling array of array of controller states

var finalized_controller_state = []
var all_controller_states: Array[Array] = []
func _ready():
	all_controller_states= []

	for i in range(max_controller_buffer_size):
		var empty = []
		for player in PlayerManager.all_players:
			empty.append(ControllerState.new())
		all_controller_states.append(empty)
	for player in PlayerManager.all_players:
		current_controller_states.append(ControllerState.new())
		finalized_controller_state.append(ControllerState.new())

func _input(event):
	var controller_type: PlayerProfile.ControllerType 
	if event is InputEventKey:
		controller_type = PlayerProfile.ControllerType.KEYBOARD
	elif event is InputEventJoypadButton or event is InputEventJoypadMotion:
		controller_type = PlayerProfile.ControllerType.CONTROLLER
	if controller_type == -1: return
	var player_number = PlayerManager.get_player_num_from_input(event.device, controller_type)
	if player_number == -1: return
	var player_profile = PlayerManager.all_players[player_number]
	var tag = player_profile.player_tag
	if event is InputEventJoypadButton:
		var buttons = tag.reverse_map.get(event.button_index)
		if buttons == null:return
		for button in buttons:
			if button == null: continue
			current_controller_states[player_number].set_button(button,event.pressed)

	elif event is InputEventJoypadMotion:
		var axis_value = event.axis_value
		if abs(axis_value) < drift_threshold:
			axis_value = 0
		var joy_axis = event.axis
		if joy_axis == null:return
		if joy_axis == JoyAxis.JOY_AXIS_TRIGGER_LEFT or joy_axis == JoyAxis.JOY_AXIS_TRIGGER_RIGHT:
			return
			#var pressed = axis_value > .9
			#current_controller_states[player_number].buttons[button] = pressed
		else: 
			var stick_num = floor(event.axis/2)
			var axis = 'x' if event.axis%2 == 0 else 'y'
			current_controller_states[player_number].sticks[stick_num][axis] = axis_value
	elif event is InputEventKey:
		
		var buttons = tag.reverse_map.get(event.keycode)
		if buttons == null:	return
		for button in buttons:
			
			if button == null:continue
			
			var action_to_vector = {
				ControllerState.Button_Types.LEFT_UP:Vector2.UP,
				ControllerState.Button_Types.LEFT_DOWN:Vector2.DOWN,
				ControllerState.Button_Types.LEFT_LEFT:Vector2.LEFT,
				ControllerState.Button_Types.LEFT_RIGHT:Vector2.RIGHT,
			}
			
			if button in action_to_vector.keys():
				var stick_num = 0
				var dir_vector = Vector2.ZERO
				
				
				for action in action_to_vector.keys():
					for dir_key in tag.action_map[action]:
						if Input.is_key_pressed(dir_key):
							dir_vector += action_to_vector[action]
							break
				
				current_controller_states[player_number].sticks[stick_num] = dir_vector
				return
			current_controller_states[player_number].set_button(button,event.pressed)

func get_finalized_stick(player_num: int, stick_num: int):
	return finalized_controller_state[player_num].sticks[stick_num]

func _physics_process(_delta):
	# Extract all current input data to the array
	var current_frame = play_scene_manager.get_current_play_frame()
	
	finalized_controller_state = []
	for i in current_controller_states:
		finalized_controller_state.append(i.get_copy())
	
	all_controller_states[current_frame % max_controller_buffer_size] = finalized_controller_state
	var previous_frame_index = (current_frame - 1) % max_controller_buffer_size
	var previous_frame_finalized_state = all_controller_states[previous_frame_index]
	
	for player_number in range(finalized_controller_state.size()):
		var current_frame_player_controller = finalized_controller_state[player_number]
		if current_frame_player_controller == null: continue
		var previous_frame_player_controller = previous_frame_finalized_state[player_number]
		for button in ControllerState.Button_Types.values():
			var current_button_value = current_frame_player_controller.get_button(button)
			var previous_button_value = previous_frame_player_controller.get_button(button)
			if current_button_value != previous_button_value:
				button_event.emit(button, player_number, current_button_value)

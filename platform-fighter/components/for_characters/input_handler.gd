class_name InputHandler
extends Node


"""
This node takes input signals from the play scene and vets them for 
nodes on the player
"""

@export var base_character: BaseCharacter
var input_manager: InputManager
var player_number: int
signal button_pressed(button)
signal button_released(button)


func _ready():
	player_number = base_character.player_number 
	if base_character and base_character.play_scene_manager:
		input_manager = base_character.play_scene_manager.input_manager
	else: return
	input_manager.button_event.connect(pass_button_signal)
	
func pass_button_signal(button_name: String, device: int, event_type: InputManager.button_event_type, _axis:Vector2):
	print(device, player_number)
	if device != player_number: return
	match event_type:
		InputManager.button_event_type.PRESSED:
			button_pressed.emit(button_name)
		InputManager.button_event_type.RELEASED:
			button_released.emit(button_name)
		_:
			return
func get_sticks():
	return input_manager.current_controller_states[player_number].sticks

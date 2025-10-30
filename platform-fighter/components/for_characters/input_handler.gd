class_name InputHandler
extends Node


"""
This node takes input signals from the play scene and clean them up for 
nodes on the player
"""

@export var base_character: BaseCharacter
var input_manager: InputManager
var player_number: int
signal button_pressed(button: ControllerState.Button_Types)
signal button_released(button: ControllerState.Button_Types)


func configure():
	player_number = base_character.player_number 
	if base_character and base_character.play_scene_manager:
		input_manager = base_character.play_scene_manager.input_manager
	else: return
	input_manager.button_event.connect(pass_button_signal)
	
func pass_button_signal(button_name: ControllerState.Button_Types, _player_number: int, pressed: bool):
	
	if player_number != _player_number: return
	if pressed:
		button_pressed.emit(button_name)
	else:
		button_released.emit(button_name)
		
func get_left_stick() -> Vector2:
	return input_manager.get_finalized_stick(player_number, 0)

func get_right_stick() -> Vector2:
	return input_manager.get_finalized_stick(player_number, 1)

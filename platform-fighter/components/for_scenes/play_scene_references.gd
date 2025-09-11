class_name PlaySceneReferences
extends Resource

var play_scene_manager: PlaySceneManager
var input_manager: InputManager
var character_holder
var camera: Camera2D


func _init(play_scene_reference:PlaySceneManager):
	play_scene_manager = play_scene_reference
	input_manager = play_scene_manager.input_manager
	

extends Node2D

@export var play_scene_path: String
@export var character_path: String


func _process(_delta):
	PlayerManager.attempt_add_player(0, PlayerProfile.ControllerType.KEYBOARD, 0)
	get_tree().change_scene_to_file(play_scene_path)

extends Node2D

var play_scene_path = "res://scenes/helper_scenes/play_scene.tscn"


func _ready():
	print("Press A to join")



func _input(event: InputEvent):
	var controller_type: PlayerProfile.ControllerType
	if event is InputEventJoypadButton:
		controller_type = PlayerProfile.ControllerType.CONTROLLER
	elif event is InputEventKey:
		controller_type = PlayerProfile.ControllerType.KEYBOARD
	if event.is_action_pressed("debug_ready"):
		PlayerManager.attempt_add_player(event.device, controller_type, 0)
		return
	if event.is_action_pressed("cancel"):
		PlayerManager.attempt_remove_player(event.device, controller_type, 0)
		return
	if event.is_action_pressed("start") and can_start():
		print(PlayerManager.all_players)
		get_tree().change_scene_to_file(play_scene_path)
	
func can_start() ->bool:
	return PlayerManager.all_players.size() >= 2

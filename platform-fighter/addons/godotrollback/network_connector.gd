extends Node2D

var play_scene_path = "res://scenes/test_scene.tscn"

func _ready():
	NetworkManager.multiplayer.connected_to_server.connect(_on_connected_to_host)
	#NetworkManager.multiplayer.peer_connected.connect()
	NetworkManager.started_to_host.connect(on_started_to_host)
	
func on_started_to_host():
	get_tree().change_scene_to_file(play_scene_path)

func _on_connected_to_host():
	get_tree().change_scene_to_file(play_scene_path)
	pass
	

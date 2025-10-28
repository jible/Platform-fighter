extends Node2D

var play_scene_path = "res://scenes/helper_scenes/play_scene.tscn"

func _ready():
	GlobalResources.connection_mode = GlobalResources.ConnectionMode.ONLINE
	print("press h to host or j to join")
	NetworkManager.multiplayer.connected_to_server.connect(print_ready_message)
	NetworkManager.multiplayer.peer_connected.connect(print_ready_message)
	
func _physics_process(delta):
	if NetworkManager.connection_type == NetworkManager.ConnectionType.DISCONNECTED:
		if Input.is_action_just_pressed("debug_host"):
			NetworkManager.start_game(NetworkManager.PORT)
		if Input.is_action_just_pressed("debug_join"):
			NetworkManager.join_game(NetworkManager.TARGET_IP, NetworkManager.PORT)
		return
	if Input.is_action_just_pressed("debug_ready") and !NetworkManager.readied_up:
		NetworkManager.readied_up = true
		get_tree().change_scene_to_file(play_scene_path)

func print_ready_message(_peer = null):
	print("Press R to ready up")

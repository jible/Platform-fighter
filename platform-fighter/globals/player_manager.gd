extends Node

var all_players: Array[PlayerProfile]

signal added_player
signal removed_player
var max_player_count = 4

func _ready():
	for i in range(max_player_count):
		all_players.append(null)

func attempt_add_player(new_player_device: int, controller_type: PlayerProfile.ControllerType, new_player_peer_id: int):
	if get_player_num_from_input(new_player_device, controller_type) != -1: return
	
	var new_player_profile = PlayerProfile.new()
	var new_player_num
	for i in range(max_player_count):
		if all_players[i] == null:
			new_player_num = i
			break
	new_player_profile.configure(new_player_num, new_player_peer_id, new_player_device, controller_type)
	all_players[new_player_num] = new_player_profile
	print("adding player")
	
	added_player.emit(new_player_num)

func attempt_remove_player(target_device_num, controller_type: PlayerProfile.ControllerType , _peer_id):
	var player_num = get_player_num_from_input(target_device_num, controller_type)
	if player_num == -1: return
	all_players[player_num] = null
	removed_player.emit(player_num)

# Outputs -1 if no such player exists
func get_player_num_from_input(target_device_num: int, controller_type : PlayerProfile.ControllerType) -> int:
	for i in range(all_players.size()):
		var player = all_players[i]
		if player and target_device_num == player.input_device_number and player.controller_type == controller_type:
			return i
	return -1

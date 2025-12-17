class_name PlayerProfile
extends Resource
# This stores all day associated with a given player. 
# Mostly for remembering what input device to listen for

enum ControllerType{
	KEYBOARD,
	CONTROLLER
}



# Player number for the lobby/match
@export var player_number: int = 0
# Team player belongs to
@export var team_number : int = 0 
# The peer number of what machine they belong to
@export var local_peer_number: int = 0
# The input device they are listening for on their local device.
@export var input_device_number: int = 0
# godot can map keyboard and contrloler to the same input device,
# so different players can have the same input device but different device types
@export var controller_type: ControllerType
# The selected character
@export var selected_character: CharacterProfile =preload("uid://wmqfm5khqbjm")
# Player tag
@export var player_tag: PlayerTag = preload("res://player_tags/default_player_tag.tres")

func configure(_player_number, _local_peer_number, _input_device_number, _controller_type):
	player_number = _player_number
	# For now:
	team_number = _player_number
	local_peer_number = _local_peer_number
	input_device_number = _input_device_number
	controller_type = _controller_type
	player_tag.configure()

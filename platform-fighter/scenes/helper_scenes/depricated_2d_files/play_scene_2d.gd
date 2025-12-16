class_name PlaySceneManager
extends Node2D


@export var camera:Camera2D
@export var input_manager:InputManager
@export var stage_holder: StageHolder
@export var character_holder: CharacterHolder
@export var rollback_communication_manager: RollbackCommunicationManager


 
var local_start_frame: int

func _ready():
	instance_scene()
	if GlobalResources.connection_mode == GlobalResources.ConnectionMode.ONLINE:
		rollback_communication_manager.start_connection()
	else:
		start_play()

func instance_scene():
	stage_holder.instance_stage()
	character_holder.instance_players()


func start_play():
	local_start_frame = Engine.get_physics_frames()
	character_holder.start_players()

func get_current_play_frame():
	var current_frame = Engine.get_physics_frames() - local_start_frame
	if current_frame < 0: 
		current_frame += (1 << 64)
	return current_frame

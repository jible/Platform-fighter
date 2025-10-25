class_name PlaySceneManager
extends Node2D


@export var camera:Camera2D
@export var input_manager:InputManager
@export var stage_holder: StageHolder
@export var character_holder: CharacterHolder
@export var rollback_communication_manager: RollbackCommunicationManager

enum ConnectionMode {
	LOCAL,
	ONLINE
}
@export var connection_mode: ConnectionMode = ConnectionMode.LOCAL

func _ready():
	instance_scene()
	if connection_mode == ConnectionMode.ONLINE:
		rollback_communication_manager.start_connection()
	else:
		start_play()
func instance_scene():
	stage_holder.instance_stage()
	character_holder.instance_players()


func start_play():
	character_holder.start_players()

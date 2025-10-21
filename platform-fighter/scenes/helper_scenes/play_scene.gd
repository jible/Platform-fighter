class_name PlaySceneManager
extends Node2D


@export var camera:Camera2D
@export var input_manager:InputManager
@export var stage_holder: StageHolder
@export var character_holder: CharacterHolder

func _ready():
	start_scene()
	
func start_scene():
	stage_holder.instance_stage()
	character_holder.instance_players()

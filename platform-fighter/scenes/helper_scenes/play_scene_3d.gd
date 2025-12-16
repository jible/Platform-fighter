extends Node3D


@export var input_manager: InputManager
@export var tick_manager: TickManager
@export var camera: Camera3D
@export var stage_holder: StageHolder
@export var character_holder: CharacterHolder
@export var physics_manager: dp_physics_server
@export var shape_renderer: dp_shape_renderer_3d

func _ready():
	pass
	
func _physics_process(delta):
	pass

func advance_tick():
	input_manager

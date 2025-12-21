extends Node3D
class_name PlaySceneManager3D

@export var input_manager: InputManager
@export var tick_manager: TickManager
@export var camera: Camera3D
@export var stage_holder: StageHolder
@export var character_holder: CharacterHolder
@export var test_objects_holder: Node

func _ready():
	character_holder.config()
	stage_holder.config()
	
func _physics_process(_delta):
	pass

func tick(prev_tick_inputs, current_tick_inputs):
	# Get previous and current inputs states and pass them to input manager to disbatch
	# Not sure if this should go first or the character process. I'm thinking aboutswapping them
	# cause this may result in frame 0 of anims playing and then frame 1 instatly playing too
	input_manager.dispatch_controller_states(current_tick_inputs, prev_tick_inputs)
	
	# Progress stage tick
	stage_holder.tick()
	# Progress character ticks
	character_holder.tick()
	
	TickManager.propogate_tick(test_objects_holder)
	
	# Physics and rendering
	DpPhysicsServer.PhysicsTick();
	DpShapeRenderer3d.update_shape_render()

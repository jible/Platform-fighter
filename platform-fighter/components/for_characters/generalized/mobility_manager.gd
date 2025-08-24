extends Node
class_name MobilityManager


@onready var base_character: BasePlayer = owner as BasePlayer
@onready var character_body = base_character.character_body

enum state_types {
	STANDARD,
	NO_PROCESS,
	PHYSICS_ONLY
}

var state_processes: Dictionary = {
	state_types.STANDARD: [
		Callable(standard_movement_process),
		Callable(standard_gravity_process),
		Callable(standard_drag_process),
	],
	state_types.NO_PROCESS: [],
	state_types.PHYSICS_ONLY: [
		Callable(standard_gravity_process),
		Callable(standard_drag_process),
	]
}

var current_state_type: state_types = state_types.NO_PROCESS

@export var jump_vel = 800
@export var grounded_acceleration = 1500
@export var aeriel_acceleration = 900
@export var gravity_force = 4500

# fraction of velocity removed each second
@export var grounded_drag = 20
@export var aeriel_drag = 2
@export var max_horizontal_velocity = 500
@export var max_fall_velocity = 800
@export var velocity_threshold = .5

var max_ariel_jumps = 2
var used_ariel_jumps = 0

func _physics_process(delta):
	# State Decides the movement processes that occur each frame
	# The default movement process is:
	# Normal movmenet process
	var input_dir = Input.get_action_strength("move_right") - Input.get_action_strength("move_left")
	for process in state_processes[current_state_type]:
		process.call(delta, input_dir)

func standard_movement_process(delta, input_dir):
	var acceleration = grounded_acceleration if character_body.is_on_floor() else aeriel_acceleration
	# If the player attempts to accelerate in the direction they are already traveling 
	if sign(input_dir) == sign(character_body.velocity.x):
		# If they are below the max velocity, increase the velocity and cap it.
		if abs(character_body.velocity.x) < max_horizontal_velocity:
			character_body.velocity.x += acceleration * input_dir * delta
			character_body.velocity.x = clamp(character_body.velocity.x, -max_horizontal_velocity, max_horizontal_velocity)
	else:
		# If they are trying to turn around, give them a kick
		character_body.velocity.x += acceleration * input_dir * delta * 5
	
func standard_drag_process(delta, input_dir):
	# If there is no input, apply drag
	if input_dir == 0 or abs(character_body.velocity.x) > max_horizontal_velocity:
		var drag = grounded_drag if character_body.is_on_floor() else aeriel_drag
		character_body.velocity.x -= character_body.velocity.x * delta * drag
		if abs(character_body.velocity.x) < velocity_threshold:
			character_body.velocity.x = 0

func standard_gravity_process(delta, _input_dir):
	if character_body.grounded:
		character_body.velocity.y = 0
		return
	character_body.velocity.y += gravity_force * delta
	if character_body.velocity.y > max_fall_velocity:
		character_body.velocity.y = max_fall_velocity

func can_jump() ->bool:
	return character_body.is_on_floor() or used_ariel_jumps < max_ariel_jumps

func jump():
	if character_body.is_on_floor():
		character_body.velocity.y = -jump_vel
	elif used_ariel_jumps < max_ariel_jumps:
		character_body.velocity.y = -jump_vel
		used_ariel_jumps += 1

func _on_base_player_landed():
	used_ariel_jumps = 0

func _on_state_machine_state_changed(new_state_node):
	current_state_type = new_state_node.state_type

class_name SpecializedCharacterBody
extends Node

var prev_grounded: bool = false
var lock_level: CharacterState.LockLevel


@export var base_character : BaseCharacter
@export var state_machine: CharacterStateMachine
@export var sprite_manager: SpriteManager
@export var input_handler: InputHandler
@export var health: Health
@export var node_state_serializers: Array[NodeStateSerializer]
var grounded:bool = false

@export var player_body:dp_char_physics_body

signal landed
signal lock_level_changed

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
		Callable(no_control_drag_process),
	]
}

var current_state_type: state_types = state_types.NO_PROCESS
var input_dir: float = 0
@export var jump_vel: dm64
@export var jump_horizontal_impulse = 5000
@export var grounded_acceleration = 25
@export var aeriel_acceleration = 15
@export var gravity_force = 75

# fraction of velocity removed each second
@export var grounded_drag = .3
@export var aeriel_drag = .03
@export var max_horizontal_velocity = 500
@export var max_vertical_velocity = 800
@export var velocity_threshold = .5

@export var max_ariel_jumps = 2
var used_ariel_jumps = 0
@onready var max_velocity_vector = Vector2(max_horizontal_velocity, max_vertical_velocity)

func configure():
	player_body.layer_collision = 0
	player_body.mask_collision = 1 << 0

func process_tick():# State Decides the movement processes that occur each frame
	# The default movement process is:
	# Normal movmenet process
	input_dir = input_handler.get_left_stick().x
	grounded = player_body.IsGrounded()
	
	for process in state_processes[current_state_type]:
		process.call()
	
	
	if !prev_grounded and grounded:
		emit_signal("landed")
		grounded = true
	prev_grounded = grounded


func standard_movement_process():
	var acceleration = grounded_acceleration if grounded else aeriel_acceleration
	# If the player attempts to accelerate in the direction they are already traveling 
	if sign(input_dir) == player_body.player_body.velocity.x.Sign():
		# If they are below the max velocity, increase the velocity and cap it.
		if player_body.velocity.x.Abs() < max_horizontal_velocity:
			player_body.velocity.x += acceleration * input_dir
			player_body.velocity.x = clamp(player_body.velocity.x, -max_horizontal_velocity, max_horizontal_velocity)
	else:
		# If they are trying to turn around, give them a kick
		player_body.velocity.x += acceleration * input_dir * 5
	
func standard_drag_process():
	# If there is no input, apply drag
	if input_dir == 0 or abs(player_body.velocity.x) > max_horizontal_velocity:
		var drag = grounded_drag if grounded else aeriel_drag
		player_body.velocity.x -= player_body.velocity.x * drag
		if abs(player_body.velocity.x) < velocity_threshold:
			player_body.velocity.x = 0

func no_control_drag_process():
	# If there is no input, apply drag
	var drag = grounded_drag if grounded else aeriel_drag
	player_body.velocity.x -= player_body.velocity.x * drag
	if abs(player_body.velocity.x) < velocity_threshold:
		player_body.velocity.x = 0

func standard_gravity_process():
	if grounded:
		return
	player_body.velocity.y += gravity_force
	if player_body.velocity.y > max_vertical_velocity:
		player_body.velocity.y = max_vertical_velocity

func can_jump() ->bool:
	return grounded or used_ariel_jumps < max_ariel_jumps

# This is pretty ugly! Tweek this when free!
func jump():
	if grounded:
		player_body.velocity.y = player_body.velocity.y -jump_vel
	elif used_ariel_jumps < max_ariel_jumps:
		player_body.velocity.y = player_body.velocity.y -jump_vel
		used_ariel_jumps += 1
		add_capped_velocity_impulse(Vector2(jump_horizontal_impulse * sign(input_dir), 0) )
	else:
		return


func _on_landed():
	used_ariel_jumps = 0


func add_capped_velocity_impulse(impulse_vector: Vector2):
	# For each axis
	for axis in ["x", "y"]:
		if impulse_vector[axis] == 0: continue
		var added_x_velocities = player_body.velocity[axis] + impulse_vector[axis]
		# If velociy is added in the same direction:
		if sign(impulse_vector[axis]) == sign(player_body.velocity[axis]):
			# If starting velocity is within max velocity range:
			if abs(player_body.velocity[axis]) < max_velocity_vector[axis]:
				# Add velocity and cap it
				player_body.velocity[axis] = clamp(added_x_velocities, -max_velocity_vector[axis], max_velocity_vector[axis])
			# Don't add velocity if it already exceeds max
		else:
			# If adding the velocity results in same velocity direction 
			# (despite adding magintude in another direction), add it.
			if sign(added_x_velocities) == sign(player_body.velocity[axis]):
				player_body.velocity[axis] = added_x_velocities
			# If adding the velocity changes the direction, add it and clamp it.
			else:
				player_body.velocity[axis] = clamp(added_x_velocities, -max_horizontal_velocity, max_horizontal_velocity)

func add_uncapped_velocity(velocity_impulse):
	player_body.velocity += velocity_impulse

func set_capped_velocity_on_axis(new_velocity, axis):
	player_body.velocity[axis] = clamp(new_velocity,-max_velocity_vector[axis],max_velocity_vector[axis])

func set_uncapped_velocity(new_velocity):
	player_body.velocity = new_velocity

func set_lock_level(new_lock_level: CharacterState.LockLevel):
	if lock_level != new_lock_level:
		lock_level = new_lock_level
		emit_signal("lock_level_changed", lock_level)


func _on_state_machine_state_changed(new_state_node):
	current_state_type = new_state_node.state_type

func _on_health_knockback(kb_vector):
	set_uncapped_velocity(kb_vector)

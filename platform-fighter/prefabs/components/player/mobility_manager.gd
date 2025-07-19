extends Node
class_name MobilityManager


@export var character_body: CharacterBody2D

var natural_move_velocity: Vector2 = Vector2.ZERO
var impulse_move_velocity: Vector2 = Vector2.ZERO

var jump_vel = 800
var acceleration = 1500
var gravity_force = 25

# fraction of velocity removed each second
var drag = 20
var max_horizontal_velocity = 500
var max_fall_velocity = 800
var velocity_threshold = .5

var max_ariel_jumps = 2
var used_ariel_jumps = 0

func _physics_process(delta):
	# Normal movmenet process
	#TODO: Later, make other movement processes like when in knockback
	var input_dir = Input.get_action_strength("move_right") - Input.get_action_strength("move_left")
	standard_movement_process(delta, input_dir)
	standard_gravity_process()
	standard_drag_process(delta, input_dir)
	standard_impulse_decay_process()
	# All impulse velocities should be applied by other nodes like states or called through funcs on this object
	
	# Once all velocity calculations are done, add both velocities
	character_body.velocity = natural_move_velocity + impulse_move_velocity
	
	
func standard_movement_process(delta, input_dir):
	#TODO Allow lock out for this process ( like knock back or in the middle of mobility)
	
	natural_move_velocity.x += acceleration * input_dir * delta
	if abs(natural_move_velocity.x) > max_horizontal_velocity:
		natural_move_velocity.x = sign (natural_move_velocity.x) * max_horizontal_velocity

func standard_gravity_process():
	# Apply Gravity
	natural_move_velocity.y += gravity_force
	if natural_move_velocity.y > max_fall_velocity:
		natural_move_velocity.y = max_fall_velocity
		
func standard_drag_process(delta, input_dir):
	# If there is no input, apply drag
	if input_dir == 0:
		natural_move_velocity.x -= natural_move_velocity.x * delta * drag
		if abs(natural_move_velocity.x) < velocity_threshold:
			natural_move_velocity.x = 0

func standard_impulse_decay_process():
	impulse_move_velocity *= .99
	if impulse_move_velocity.length() < .5:
		impulse_move_velocity = Vector2.ZERO


func can_jump() ->bool:
	return character_body.is_on_floor() or used_ariel_jumps < max_ariel_jumps

func jump():
	if character_body.is_on_floor():
		natural_move_velocity.y = -jump_vel
	elif used_ariel_jumps < max_ariel_jumps:
		natural_move_velocity.y = -jump_vel
		used_ariel_jumps += 1

func _on_base_player_landed():
	used_ariel_jumps = 0

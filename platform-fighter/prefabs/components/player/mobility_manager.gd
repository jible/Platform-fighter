extends Node
class_name MobilityManager


@export var character_body: CharacterBody2D



var jump_vel = 800
var acceleration = 1500
var gravity_force = 50

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
	#standard_impulse_decay_process()
	# All impulse velocities should be applied by other nodes like states or called through funcs on this object

	
	
func standard_movement_process(delta, input_dir):
	#TODO Allow lock out for this process ( like knock back or in the middle of mobility)
	
	character_body.velocity.x += acceleration * input_dir * delta
	if abs(character_body.velocity.x) > max_horizontal_velocity:
		character_body.velocity.x = sign (character_body.velocity.x) * max_horizontal_velocity

func standard_gravity_process():
	# Apply Gravity
	character_body.velocity.y += gravity_force
	if character_body.velocity.y > max_fall_velocity:
		character_body.velocity.y = max_fall_velocity
		
func standard_drag_process(delta, input_dir):
	# If there is no input, apply drag
	if input_dir == 0:
		character_body.velocity.x -= character_body.velocity.x * delta * drag
		if abs(character_body.velocity.x) < velocity_threshold:
			character_body.velocity.x = 0

func standard_impulse_decay_process():
	character_body.velocity *= .99
	if character_body.velocity.length() < .5:
		character_body.velocity = Vector2.ZERO


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

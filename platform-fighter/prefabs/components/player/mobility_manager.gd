extends Node
class_name MobilityManager

@export var character_body: CharacterBody2D

var jump_vel = 800
var acceleration = 1500
var gravity_force = 25

# fraction of velocity removed each second
var drag = 20
var max_horizontal_velocity = 500
var velocity_threshold = .5

var max_ariel_jumps = 2
var used_ariel_jumps = 0

func _physics_process(delta):
	var input_dir = Input.get_action_strength("move_right") - Input.get_action_strength("move_left")
	character_body.velocity.x += acceleration * input_dir * delta
	
	# If there is no input, apply drag
	if input_dir == 0:
		character_body.velocity.x -= character_body.velocity.x * delta * drag
		if abs(character_body.velocity.x) < velocity_threshold:
			character_body.velocity.x = 0
	
	# Apply Gravity
	character_body.velocity.y += gravity_force
	if abs(character_body.velocity.x) > max_horizontal_velocity:
		character_body.velocity.x = max_horizontal_velocity * sign(character_body.velocity.x)

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

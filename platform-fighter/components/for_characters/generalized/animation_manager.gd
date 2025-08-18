class_name AnimationManager
extends AnimatedSprite2D

# Based on https://github.com/jible/capstone/blob/main/scripts/characters/animation_manager.gd
@export var player_body: CharacterBody2D
@export var snap: bool = true
var current_state: String = ""

func _ready():
	player_body.position = (player_body.position/scale).round() * scale

func _on_state_machine_state_changed(new_state_node):
	current_state = new_state_node.name
	play(current_state)

func _process(_delta):
	if snap:
		if player_body.velocity.x != 0 and player_body.velocity.y != 0:
			var projected_pos = (player_body.position/scale).round() * scale
			if (projected_pos - position).length() > 1:
				position = snapped(player_body.position, scale)
		else:
			position = snapped(player_body.position, scale)

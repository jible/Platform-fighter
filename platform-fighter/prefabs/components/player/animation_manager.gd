class_name AnimationManager
extends AnimatedSprite2D

# Based on https://github.com/jible/capstone/blob/main/scripts/characters/animation_manager.gd
@export var player_body: CharacterBody2D
var current_state: String = ""

func update_anim():
	play(current_state)
	return


func _on_state_machine_state_changed(new_state_node):
	current_state = new_state_node.name
	update_anim()



func _process(_delta):
	position.x = snapped(player_body.position.x, scale.x)
	position.y = snapped(player_body.position.y, scale.y)

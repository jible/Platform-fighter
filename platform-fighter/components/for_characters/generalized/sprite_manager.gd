class_name SpriteManager
extends AnimatedSprite2D

# Based on https://github.com/jible/capstone/blob/main/scripts/characters/animation_manager.gd
@onready var base_character: BasePlayer = owner as BasePlayer
@onready var character_body: CharacterBody2D = base_character.character_body
@export var snap: bool = true
var current_state: String = ""

func _ready():
	character_body.position = (character_body.position/scale).round() * scale

func _on_state_machine_state_changed(new_state_node):
	current_state = new_state_node.name
	play(current_state)

func _process(_delta):
	if snap:
		if character_body.velocity.x != 0 and character_body.velocity.y != 0:
			var projected_pos = (character_body.position/scale).round() * scale
			if (projected_pos - position).length() > 1:
				position = snapped(character_body.position, scale)
		else:
			position = snapped(character_body.position, scale)

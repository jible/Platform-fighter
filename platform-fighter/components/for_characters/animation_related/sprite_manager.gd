class_name SpriteManager
extends AnimatedSprite2D

# Based on https://github.com/jible/capstone/blob/main/scripts/characters/animation_manager.gd
@export var base_character: BaseCharacter
@export var character_body: SpecializedCharacterBody
@export var snap: bool = true
var current_state: String = ""

func configure():
	character_body.position = (character_body.position/scale).round() * scale

func process_tick():
	if snap:
		if character_body.velocity.x != 0 and character_body.velocity.y != 0:
			var projected_pos = (character_body.position/scale).round() * scale
			if (projected_pos - position).length() > 1:
				position = snapped(character_body.position, scale)
		else:
			position = snapped(character_body.position, scale)
			
	else: position = character_body.position

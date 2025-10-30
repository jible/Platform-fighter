class_name Hurtbox
extends Area2D

@export var base_character: BaseCharacter
@export var character_body: SpecializedCharacterBody
var health = null



func configure():
	if !base_character: 
		return
	collision_layer = 0
	collision_layer += 1 << (base_character.team_number + GlobalResources.team_one_collision_bit)
	collision_mask = 0
	health = base_character.health


signal received_hit (hitbox, hit_data)

func hit_by(hitbox, hit_data):
	received_hit.emit(hitbox, hit_data)

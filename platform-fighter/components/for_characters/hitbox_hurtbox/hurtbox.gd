class_name Hurtbox
extends Area2D

@export var base_character: BaseCharacter
@export var character_body: SpecializedCharacterBody
var health = null

func _ready():
	if !base_character: 
		return
	collision_layer = 0
	collision_layer += 1 << base_character.team_number
	print(collision_layer)
	
	health = base_character.health


signal received_hit (hitbox, hit_data)

func hit_by(hitbox, hit_data):
	print("ouch")
	emit_signal("received_hit", hitbox, hit_data)

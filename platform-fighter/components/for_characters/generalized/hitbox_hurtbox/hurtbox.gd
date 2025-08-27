class_name Hurtbox
extends Area2D


@onready var base_character = owner as BaseCharacter
var health = null

func _ready():
	if !base_character: 
		base_character = self
		return
	collision_layer = 0
	collision_layer += 1 << base_character.team_number
	
	
	health = base_character.health


signal received_hit (hitbox, hit_data)

func hit_by(hitbox, hit_data):
	print("ouch" + str(self))
	emit_signal("received_hit", hitbox, hit_data)

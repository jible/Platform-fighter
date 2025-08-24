class_name Hurtbox
extends Area2D

@onready var base_character: BasePlayer = owner as BasePlayer
var health = null

func _ready():
	if !base_character: return
	health = base_character.health


signal received_hit (hitbox, hit_data)

func hit_by(hitbox, hit_data):
	print("ouch" + str(self))
	emit_signal("received_hit", hitbox, hit_data)

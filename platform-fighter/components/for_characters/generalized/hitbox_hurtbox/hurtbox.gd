class_name Hurtbox
extends Area2D

var player

func _ready():
	player = get_player()


func get_player():
	# Returns reference to owner. 
	# This is for hitboxes to group hurtboxes and not hit the same player twice.
	pass

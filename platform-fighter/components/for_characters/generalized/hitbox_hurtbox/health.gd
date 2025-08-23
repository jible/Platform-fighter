class_name Health
extends Node

"""
This node stores health
Identifies characters
Decides knock back
"""

var health: float = 0


func hit_by(hitbox):
	health -= hitbox.health
	# Extract knock back and direction from hitbox and decide apply that via mobility manager
	# and handle state change
	# And consider super armor if that is a thing

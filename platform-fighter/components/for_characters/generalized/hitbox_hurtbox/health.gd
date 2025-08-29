class_name Health
extends Node

"""
This node stores health
Decides knock back
"""

@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine


var health: float = 0

func _ready():
	connect_hurtboxes(state_machine)
	pass


func connect_hurtboxes(base):
	for child in base.get_children():
		if child is Hurtbox:
			child.hit_by.connect(hit_by)
		connect_hurtboxes(child)
	

func hit_by(hitbox, _hurtbox):
	health -= hitbox.damage
	print("health: " + str(health) )
	# Extract knock back and direction from hitbox and decide apply that via mobility manager
	# and handle state change
	# And consider super armor if that is a thing

class_name Health
extends Node

"""
This node stores health
Decides knock back
"""

signal knockback( kb_vector :Vector2)

@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine

@export var starting_health: float = 10

var health: float = 0

func _ready():
	health = starting_health
	connect_hurtboxes(state_machine)
	pass

func connect_hurtboxes(base):
	for child in base.get_children():
		if child is Hurtbox:
			child.hit_by.connect(hit_by)
		connect_hurtboxes(child)

func hit_by(hitbox:Hitbox, _hurtbox):
	health -= hitbox.damage
	handle_kb(hitbox)
	
	# if there is super armor, it should be accounted for here.
func handle_kb(hitbox:Hitbox):
	# Extract knock back and direction from hitbox and decide apply that via mobility manager
	# and handle state change
	# And consider super armor if that is a thing
	var kb_vector = hitbox.knockback_vector
	var kb_base_magnitude = hitbox.knockback_magnitude
	
	#Don't do knopckback or stun if no knockback
	if kb_base_magnitude == 0:return 
	
	var impulse_velocity_vector = kb_vector * kb_base_magnitude * 1/health
	
	# Originally directly caleld character body, but this is better if you make non player obj/char
	# that reacts uniquely
	knockback.emit(impulse_velocity_vector)
	
	# Maybe change this to a behavior
	state_machine.change_state("knocback")

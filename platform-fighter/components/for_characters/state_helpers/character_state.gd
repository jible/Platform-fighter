class_name CharacterState
extends Node2D

enum LockLevel {
	FREE,
	ACTION_CANCELABLE,
	HURT_CANCELABLE,
	FULL_LOCK,
}
''' Toggle this to prevent this state from being active
For instance, you may inherit a base state but not want to use it.'''
@export var is_active:bool = true
@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine
@export var character_body:SpecializedCharacterBody

@export var state_type:SpecializedCharacterBody.state_types = SpecializedCharacterBody.state_types.STANDARD
@export var can_turn_around: bool = true




# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	
func on_anim_end():
	for hitbox in get_children(true):
		if hitbox is Hitbox or hitbox is HitboxCluster:
			hitbox.turn_off()
	pass

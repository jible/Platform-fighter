class_name CharacterState
extends Node2D

enum LockLevel {
	FREE,
	ACTION_CANCELABLE,
	HURT_CANCELABLE,
	FULL_LOCK,
}

var is_active:bool = false
var state_machine: CharacterStateMachine
var character_body:CharacterBody2D
var mobility_manager: MobilityManager

@export var state_type:MobilityManager.state_types = MobilityManager.state_types.STANDARD

var base_character: BaseCharacter

func configure(state_machine_ref, base_character_ref):
	state_machine = state_machine_ref
	base_character = base_character_ref
	character_body = base_character.character_body
	mobility_manager = base_character.mobility_manager

# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	
func on_anim_end():
	pass

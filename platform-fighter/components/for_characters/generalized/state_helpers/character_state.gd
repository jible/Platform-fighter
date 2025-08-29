class_name CharacterState
extends Node2D

enum LockLevel {
	FREE,
	ACTION_CANCELABLE,
	HURT_CANCELABLE,
	FULL_LOCK,
}

var is_active:bool = false
@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine
@export var character_body:CharacterBody2D
@export var mobility_manager: MobilityManager

@export var state_type:MobilityManager.state_types = MobilityManager.state_types.STANDARD





# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	
func on_anim_end():
	pass

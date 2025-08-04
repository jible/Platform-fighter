class_name CharacterState
extends Node

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


func _ready():
	state_machine = get_parent()
	character_body = state_machine.get_parent()
	if character_body.mobility_manager:
		mobility_manager = character_body.mobility_manager

# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	
func on_anim_end():
	pass

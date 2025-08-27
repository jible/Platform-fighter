class_name CharacterBehavior
extends Node

## NOTE: I spent ~1 hour trying to find a way to not make these exports, but still make them easy to change in the script
# Unfortunately, it seems like exporting is the best way to make these easy to set for now.
# This behavior takes effect when the character's lock level is less than or equal to this priority.
@export var priority: CharacterState.LockLevel
# This behavior only takes effect when the character is in any of the given states
@export var valid_states: Array[String]
@onready var base_character: BaseCharacter = owner as BaseCharacter
@onready var state_machine: CharacterStateMachine = base_character.state_machine
var is_active:bool = false



# WHEN MAKING A CLASS BEHAVIOR, COPY AND PASTE THESE FUNCTIONS INTO IT
# If the priority is correct and the state is correct, check this condition every frame. If it is true, do trigger the behavior  
func condition()->bool:
	return false
	
# When the condition is true, perform this action
func trigger()->void:
	pass

class_name PlayerBehavior
extends Node


# This behavior takes effect when the character's lock level is less than or equal to this priority.
@export var priority: CharacterState.LockLevel
# This behavior only takes effect when the character is in any of the given states
@export var valid_tags: Array[CharacterState.TAGS]

# If the priority is correct and the state is correct, check this condition every frame. If it is true, do trigger the behavior  
func condition()->bool:
	return false
	
# When the condition is true, perform this action
func trigger()->void:
	pass

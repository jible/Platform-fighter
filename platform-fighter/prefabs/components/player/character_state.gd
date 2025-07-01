class_name CharacterState
extends State


enum LockLevel {
	FREE,
	ACTION_CANCELABLE,
	HURT_CANCELABLE,
	FULL_LOCK,
}


# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	

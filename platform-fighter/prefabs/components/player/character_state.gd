class_name CharacterState
extends State

# All types of tags a player state can have
enum TAGS {
	IDLE,
	RUN,
	FALL,
	JUMP,
	ATTACK,
	BLOCK,
	HURT
}

enum LockLevel {
	FREE,
	ACTION_CANCELABLE,
	HURT_CANCELABLE,
	FULL_LOCK,
}

@export var tag: TAGS
# TO be overwritten
func condition() -> bool:
	return false

# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	

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


# The map that tells what type of state each state type can enter
static var tag_map = {
	TAGS.IDLE: [
		TAGS.RUN,
		TAGS.JUMP,
		TAGS.FALL,
		TAGS.ATTACK,
		TAGS.BLOCK,
	],
	TAGS.RUN: [
		TAGS.IDLE,
		TAGS.FALL,
		TAGS.JUMP,
		TAGS.ATTACK,
		TAGS.BLOCK
	]
}

@export var tag: TAGS
# Signals are converted into keys. For example:
# The input manager emits a signal that the side-attack input was pressed. It will the key "INPUT_SIDE_ATTACK".
# Then, the state machine will know this state requires that key, so it will change to this state if it is the correct state type
# See state machine for translation from signal to key for all cases.
@export var condition_keys: Array[String] = []
# TODO Perhaps, change the condition key to an array of keys if any of the state come to have multiple signal based conditions.

# The follwing functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass
	

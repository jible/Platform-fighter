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
	

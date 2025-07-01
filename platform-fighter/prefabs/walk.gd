extends CharacterState

func condition():
	return Input.is_action_pressed("move_left") or Input.is_action_pressed("move_right")

func enter_state():
	pass

func update_state(_delta):
	if not (Input.is_action_pressed("move_left") or Input.is_action_pressed("move_right") ):
		state_machine.change_state("Idle")

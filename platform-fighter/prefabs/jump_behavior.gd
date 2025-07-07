extends CharacterBehavior



func condition():
	return Input.is_action_just_pressed("jump")

func trigger():
	state_machine.change_state("Jump")
	pass

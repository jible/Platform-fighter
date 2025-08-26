extends CharacterBehavior


func condition():
	return Input.is_action_just_pressed("dash")

func trigger():
	state_machine.change_state("Dash")
	pass

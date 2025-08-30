extends CharacterBehavior


func condition():
	return Input.is_action_just_pressed("attack")

func trigger():
	state_machine.change_state("Punch")
	pass

extends CharacterState


func enter_state():
	pass

func update_state(delta):
	if not (Input.is_action_pressed("move_left") or Input.is_action_pressed("move_right") ):
		state_machine.change_state("Idle")

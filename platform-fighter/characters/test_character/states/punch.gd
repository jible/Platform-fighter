extends CharacterState


func on_anim_end():
	if character_body.is_on_floor():
		state_machine.change_state("Idle")
	else:
		state_machine.change_state("Aerial")
	pass

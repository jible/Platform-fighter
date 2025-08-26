extends CharacterState



func enter_state():
	mobility_manager.jump()
	#TODO Change this to happen at the end of anim
	state_machine.change_state("Aerial")

extends CharacterState

@export var input_handler: InputHandler

func enter_state():
	pass

func update_state(_delta):
	if abs(input_handler.get_left_stick().x) < .1:
		state_machine.change_state("Idle")

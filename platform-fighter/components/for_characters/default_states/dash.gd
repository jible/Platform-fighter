extends CharacterState


@export var dash_velocity = 2000
@export var input_handler: InputHandler

func update_state(_delta):
	pass

func enter_state():
	character_body.velocity = input_handler.get_left_stick().normalized() * dash_velocity
	
func exit_state():
	pass
	
func on_anim_end():
	super()
	if character_body.is_on_floor():
		state_machine.change_state("Idle")
	else:
		state_machine.change_state("Aerial")
	pass

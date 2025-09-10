extends CharacterState


@export var dash_velocity = 2000


func update_state(_delta):
	pass

func enter_state():
	character_body.velocity = Input.get_vector("move_left","move_right","move_up", "move_down") * dash_velocity
	
func exit_state():
	pass
	
func on_anim_end():
	super()
	if character_body.is_on_floor():
		state_machine.change_state("Idle")
	else:
		state_machine.change_state("Aerial")
	pass

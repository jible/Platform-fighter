extends CharacterState


@export var dash_velocity = 800


func update_state(_delta):
	pass

func enter_state():
	mobility_manager.impulse_move_velocity = Input.get_vector("move_left","move_right","move_up", "move_down") * dash_velocity
	state_machine.change_state("Idle")
	pass

func exit_state():
	pass
	
func on_anim_end():
	pass

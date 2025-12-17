extends CharacterBehavior

func trigger():
	state_machine.change_state("Dash")
	pass


func _on_input_handler_button_pressed(button):
	if button == ControllerState.Button_Types.SPECIAL and is_active:
		trigger()

extends CharacterBehavior


func _on_input_handler_button_pressed(button):
	if button == ControllerState.Button_Types.LIGHT and is_active:
		trigger()

func trigger():
	state_machine.change_state("Punch")
	pass

extends CharacterBehavior


func _on_input_handler_button_pressed(button):
	if button == "A" and is_active:
		trigger()

func trigger():
	state_machine.change_state("Punch")
	pass

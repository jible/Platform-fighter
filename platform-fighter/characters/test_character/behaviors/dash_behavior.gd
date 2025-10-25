extends CharacterBehavior

func trigger():
	state_machine.change_state("Dash")
	pass


func _on_input_handler_button_pressed(button):
	if button == "B" and is_active:
		print("triggered")
		trigger()

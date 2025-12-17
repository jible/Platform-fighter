extends CharacterBehavior


@export var character_body: SpecializedCharacterBody

func trigger():
	state_machine.change_state("Jump")
	pass


func _on_input_handler_button_pressed(button):
	if button == ControllerState.Button_Types.JUMP and character_body.can_jump() and is_active:
		trigger()

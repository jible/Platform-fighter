extends CharacterBehavior


# TODO Maybe this can be removed, but I figured it would be nice to generalize auto canceling
# If no future states auto cancel similair to the ariel state, just get rid of this behavior and put the signal on the ariel state
func _on_character_body_landed():
	if is_active:
		state_machine.change_state("Idle")

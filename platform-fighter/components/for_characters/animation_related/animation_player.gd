extends AnimationPlayer


func _on_state_machine_state_changed(new_state_node):
	play(new_state_node.name)

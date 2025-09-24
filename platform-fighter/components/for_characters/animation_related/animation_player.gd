@tool
extends AnimationPlayer

func _ready():
	if Engine.is_editor_hint():
		set_block_signals(true)
	else: set_block_signals(false)

func _on_state_machine_state_changed(new_state_node):
	if Engine.is_editor_hint():return
	play(new_state_node.name)

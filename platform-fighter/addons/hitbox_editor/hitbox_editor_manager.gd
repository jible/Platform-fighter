@tool
extends VBoxContainer

@export var character_name_label: Label
@export var state_drop_down: OptionButton
'''
On start, this loads the characters from the character folder
'''

func _ready():
	pass

func configure(character_root: BaseCharacter):
	character_name_label.text = character_root.name
	
	var states = []
	character_root.state_machine.states.keys()
	state_drop_down.clear()
	for state in states:
		state_drop_down.add_item(state.name)
	pass

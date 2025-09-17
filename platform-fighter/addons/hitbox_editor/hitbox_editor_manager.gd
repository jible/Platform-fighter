@tool
extends VBoxContainer

@export var character_name_label: Label
@export var state_drop_down: OptionButton
'''
On start, this loads the characters from the character folder
'''
var states = []
var character_root
var state_machine
var current_state_hitboxes = []
func _ready():
	pass

func configure(_character_root: BaseCharacter):
	character_root = _character_root
	character_name_label.text = character_root.name
	state_machine = character_root.state_machine
	states = character_root.state_machine.get_children(true)
	
	state_drop_down.clear()
	for state in states:
		state_drop_down.add_item(state.name)
	_on_option_button_item_selected(state_drop_down.selected)



func _on_option_button_item_selected(index):
	var current_state_name = state_drop_down.get_item_text(index)
	var state = state_machine.find_child(current_state_name)
	if !state:
		print("state does not exist")
		return
	current_state_hitboxes = state.get_children()
	print(current_state_hitboxes)

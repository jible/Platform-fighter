@tool
extends VBoxContainer

@export var hitbox_script_path:String
@export var default_hitbox_radius = 20

@export var character_name_label: Label
@export var state_drop_down: OptionButton
@export var hitbox_drop_down: OptionButton
@export var hitbox_selection: VBoxContainer
'''
On start, this loads the characters from the character folder
'''
var states = []
var state 
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
	_on_state_drop_down_item_selected(state_drop_down.selected)

func _on_state_drop_down_item_selected(index):
	var current_state_name = state_drop_down.get_item_text(index)
	state = state_machine.find_child(current_state_name)
	if !state:
		print("state does not exist")
		return
	update_hitboxes(state.get_children())

func update_hitboxes(hitboxes):
	hitbox_drop_down.clear()
	current_state_hitboxes = hitboxes
	for hitbox in current_state_hitboxes:
		hitbox_drop_down.add_item(hitbox.name)
	if current_state_hitboxes.size() > 0: # dont select if there is no hitbox
		_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)
		hitbox_selection.show()
	else:
		hitbox_selection.hide()
func _on_hitbox_drop_down_item_selected(index):
	var new_hitbox = get_hitbox_from_name(hitbox_drop_down.get_item_text(index))
	if new_hitbox:
		hitbox_selection.update_hitbox(new_hitbox, character_root, state)
	else:
		push_error("Could not find hitbox")

func get_hitbox_from_name(hitbox_name):
	for hitbox in current_state_hitboxes:
		if hitbox.name == hitbox_name:
			return hitbox
	return null



func add_hitbox(state_node):
	var new_hitbox = Area2D.new()
	new_hitbox.set_script(load(hitbox_script_path))
	
	var collision = CollisionShape2D.new()
	collision.shape = CircleShape2D.new()
	collision.shape.radius = default_hitbox_radius
	
	state_node.add_child(collision)
	
	return new_hitbox

func remove_hitbox(state_node:Node, hitbox):
	state_node.remove_child(hitbox)
	hitbox.queue_free()

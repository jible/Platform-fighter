@tool
extends ScrollContainer

# Character
@export var character_name_label: Label
var base_character: BaseCharacter
var animation_player: AnimationPlayer
var state_machine: CharacterStateMachine
var states: Array[Node]
# State
@export var state_drop_down: OptionButton
var state: CharacterState

# Cluster
@export var cluster_drop_down: OptionButton
@export var add_cluster: Button
@export var remove_cluster: Button
var cluster: HitboxCluster
@export var turn_on_frame_field: SpinBox
@export var turn_off_frame_field: SpinBox


# Hitboxes
@export var hitbox_drop_down: OptionButton
@export var add_hitbox_button: Button
@export var remove_hitbox_button: Button
var hitboxes = []



func configure(_character_root: BaseCharacter):
	base_character = _character_root
	character_name_label.text = base_character.name
	animation_player = base_character.animation_player
	state_machine = base_character.state_machine
	states = state_machine.get_children(true)
	state_drop_down.clear()
	if states.size() <= 0:return
	for state in states:
		state_drop_down.add_item(state.name)
	_on_state_drop_down_item_selected(state_drop_down.selected)

func reconfigure():configure(base_character)

# Populates hitbox ui. Call when new state is selected
# Automatically Selects the first hitbox
func update_hitboxes(_hitboxes):
	hitbox_drop_down.clear()
	hitboxes = _hitboxes
	for hitbox in hitboxes:
		hitbox_drop_down.add_item(hitbox.name)
	if hitboxes.size() > 0: # dont select if there is no hitbox
		_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)

# Signal Handlers
func _on_state_drop_down_item_selected(index):
	pass # Replace with function body.


func _on_refresh_button_pressed():
	reconfigure()

func _on_hitbox_drop_down_item_selected(index):
	pass # Replace with function body.

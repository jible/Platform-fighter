class_name CharacterStateMachine
extends Node

# Inspired by https://github.com/jible/capstone/blob/main/scripts/characters/state_machine.gd
@export var starting_state: String = ""


var states: Dictionary[String, CharacterState] = {}
var current_state_name: String
var current_state_node = null
var locked:bool = false


signal state_changed(new_state_node: CharacterState)

@export var DEBUG_PRINT_STATE_CHANGE: bool = false

func _ready():
	for child in get_children():
		states[child.name] = child
	change_state(starting_state)

func _physics_process(delta):
	#print(current_state_name)
	update_state(delta)

func change_state(state_name: String):
	# Throw error if entering invalid state
	if not states.has(state_name):
		assert(false, "attempting to enter invalid state:" + state_name)
	# Do nothing if it tries to enter the current state
	if state_name == current_state_name: return
	
	if current_state_node:
		exit_state(current_state_node)
	current_state_name = state_name
	current_state_node = states[current_state_name]
	emit_signal("state_changed", current_state_node)
	enter_state(current_state_node)

func update_state(delta):
	current_state_node.update_state(delta)
	
func enter_state(new_state):
	# Trigger the newly entered state
	if DEBUG_PRINT_STATE_CHANGE:
		print(new_state.name)
	new_state.enter_state()
	new_state.is_active = true

func exit_state(old_state):
	old_state.exit_state()
	old_state.is_active = false


func _on_animation_player_animation_finished(_anim_name):
	current_state_node.on_anim_end()

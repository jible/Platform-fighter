class_name CharacterStateMachine
extends Node

# Inspired by https://github.com/jible/capstone/blob/main/scripts/characters/state_machine.gd
@export var starting_state: String = ""

var states: Dictionary[String, CharacterState] = {}
var current_state_name: String
var current_state_node = null

var condition_keys: Dictionary[String,String] = {}

signal state_changed

func _ready():
	for child in get_children():
		states[child.name] = child
	change_state(starting_state)

func _physics_process(delta):
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

# When a signal is received, call construct a key and call this function with that key
func call_key(key):
	var reaction_state = condition_keys.get(key)
	if reaction_state:
		change_state(reaction_state)

func update_state(delta):
	current_state_node.update_state(delta)
	pass
	
func enter_state(new_state):
	# Create dictionary of signal keys that correspond to the state to swap to
	condition_keys = {}
	for state_name in states:
		var state = states[state_name]
		for enterable_state_tag in CharacterState.tag_map[new_state.tag]:
			if state.tag == enterable_state_tag:
				for key in state.condition_keys:
					condition_keys[key] = state_name
	# Trigger the newly entered state
	new_state.enter_state()
	new_state.is_active = true

func exit_state(old_state):
	condition_keys = {}
	
	old_state.exit_state()
	old_state.is_active = false

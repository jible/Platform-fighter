class_name PlayerStateSerializer
extends Node

''' 
Serializes the state of the player currently
Itterate through all children of the root

If any children own a StateExtractor, name: "node_state_serializers", they will be added to this list

Every frame, this will call to extract the state from each node's collection of serializers 

all_states is a rolling array of all character states. 
Each character state is another array of all node states on that character
each node state is an array of the values of the properties that belong to the given node


'''
@export var base_character: BaseCharacter
@export var max_rollback:int = 50
var all_states: Array = []

var nodes_with_state = []

func configure():
	get_serializable_nodes(base_character)
	all_states.resize(max_rollback)
	for i in range(max_rollback):
		all_states[i] = []


func _physics_process(_delta):
	if Input.is_action_just_pressed("debug_print"):
		print('e', all_states)
	if Input.is_action_just_pressed("debug_action"):
		rollback_to_tick(Engine.get_physics_frames() - 30)
	var current_character_state = []
	var current_tick = Engine.get_physics_frames()
	var state_index = get_tick_index(current_tick)
	for node in nodes_with_state:
		var current_node_states = []
		for node_state_extractor in node.node_state_serializers:
			current_node_states.append(node_state_extractor.extract_state(node))
		current_character_state.append(current_node_states)
	all_states[state_index] = current_character_state
	

func get_tick_index(tick):
	return tick % max_rollback


func get_serializable_nodes(parent:Node):
	if !parent: return
	if "node_state_serializers" in parent:
		parent.node_state_serializers = parent.node_state_serializers.duplicate(true)
		nodes_with_state.append(parent)
	for child in parent.get_children():
		get_serializable_nodes(child)

func rollback_to_tick(tick):
	var target_character_state = get_state_at_tick(tick)
	if !target_character_state:
		push_error("Rollingback to null state") 
		return
	
	for node_index in range(nodes_with_state.size()):
		var node = nodes_with_state[node_index]
		var target_node_properties = target_character_state[node_index]
		for property_index in range(node.node_state_serializers.size()):
			node.node_state_serializers[property_index].imbue_state(node, target_node_properties[property_index])

func get_state_at_tick(tick):
	var current_tick = Engine.get_physics_frames()
	if current_tick - tick >max_rollback:
		push_error("Requested tick out of reach")
	
	var tick_index = get_tick_index(tick)
	return all_states[tick_index]

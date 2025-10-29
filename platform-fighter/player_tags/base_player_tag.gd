class_name PlayerTag
extends Resource


@export var name: String

var play_prefix = "play"
var default_tag_name = "default"
var action_map = {}
var reverse_map = {}

func configure():
	if action_map.is_empty():
		action_map = GlobalResources.default_input_action_events.duplicate(true)
	update_reverse_map()

func add_input_event(action, key_code):
	action_map[action].append(key_code)
	update_reverse_map()

func remove_input_event(action,key_code):
	action_map[action].erase(key_code)
	update_reverse_map()

func update_reverse_map():
	reverse_map = {}
	for action in action_map.keys():
		for event in action_map[action]:
			if reverse_map.get(event) == null: 
				reverse_map[event] = []
			reverse_map[event].append(action)

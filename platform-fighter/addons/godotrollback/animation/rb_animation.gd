@tool
class_name RbAnimation
extends Resource


var keys : Dictionary[int, Variant] = {}
var length: int = 0

var animation_manager: RbAnimationManager

func play_tick(tick: int):
	for key in keys[tick]:
		call_key(key)

func call_key(key: RbAnimationKey):
	var action_object = animation_manager.get_node(key.path)
	if !action_object:
		push_error("Couldn't find key object")
		return
	if key.type == RbAnimationKey.KeyType.METHOD:
		action_object.call(key.attribute, key.value)
		return
	action_object[key.attribute] = key.value
	return
	



func add_key(tick: int, key_member: Node, key_type: RbAnimationKey.KeyType, attribute: String, value: Variant)-> RbAnimationKey:
	var key = RbAnimationKey.new()
	var path = get_anim_path_to(key_member)
	key.configure(path, key_type, attribute, value)
	if !keys[tick]:
		keys[tick] = []
	keys[tick].append(key)
	
	return key

func get_anim_path_to(node)-> NodePath:
	return animation_manager.owner.get_path_to(node)

func remove_key(tick, key_member, key_attribute):
	var tick_keys = keys[tick]
	if !keys.has(tick):
		print("couldn't find key")
		return
	var path = get_anim_path_to(key_member)
	for key_index in range(tick_keys.size()):
		var tick_key = tick_keys[key_index]
		if tick_key.path == path and tick_key.attribute == key_attribute:
			tick_keys.remove_at(key_index)
			tick_key.queue_free()
			if tick_keys.size() == 0:
				keys.erase(tick)
			break

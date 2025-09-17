@tool 
extends BoxContainer

@export_file("*.tscn") var field_path: String
@export var minimum_fields: int = 1
var fields = 0
var field_scene:PackedScene

func _ready():
	if field_path:
		field_scene = load(field_path)
	else:
		push_error("No file path set")
	reset()

func reset():
	if !field_scene:return
	for child in get_children():
		if child.scene_file_path == field_scene.resource_path:
			remove_child(child)
			child.queue_free()
	for i in minimum_fields:
		_on_add_pressed()

func _on_add_pressed():
	if !field_scene:return
	var new_field = field_scene.instantiate()
	add_child(new_field)
	fields += 1

func _on_remove_pressed():
	if !field_scene:return
	var to_go = get_child(-1)
	if !to_go or to_go.scene_file_path != field_scene.resource_path or fields <=minimum_fields:
		return
	remove_child(to_go)
	to_go.queue_free()
	fields -= 1

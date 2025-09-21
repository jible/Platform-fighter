@tool 
extends BoxContainer

@export_file("*.tscn") var field_path: String
@export var minimum_fields: int = 0
var fields = 0
var field_scene:PackedScene
signal field_added(field)
signal field_removed(field)

func _ready():
	if field_path:
		field_scene = load(field_path)
	else:
		push_error("No file path set")
	reset()

# Removes all children
func reset():
	if !field_scene:return
	for child in get_children():
		if child.scene_file_path == field_scene.resource_path:
			remove_child(child)
			child.queue_free()
	for i in range(minimum_fields):
		add_field()

func _on_add_pressed():
	add_field()

func add_field():
	if !field_scene:return
	var new_field = field_scene.instantiate()
	add_child(new_field)
	
	var remove_button = Button.new()
	remove_button.text = "X"
	new_field.add_child(remove_button)
	
	remove_button.pressed.connect(_on_remove_pressed.bind(new_field))
	fields += 1
	field_added.emit(new_field)
	return new_field

func _on_remove_pressed(field_to_remove):
	field_removed.emit(field_to_remove)
	remove_child(field_to_remove)
	field_to_remove.queue_free()

func sort_field(field):
	var index = 0
	for child in get_children():
		if child.scene_file_path != field_scene.resource_path:continue
		if field.get_sort_value() < child.get_sort_value():
			move_child(field, index)
			return
		index += 1

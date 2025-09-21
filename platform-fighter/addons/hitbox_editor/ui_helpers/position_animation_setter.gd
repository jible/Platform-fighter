@tool
class_name PositionAnimationSetter
extends HBoxContainer


@export var frame_field: SpinBox
@export var x_field: SpinBox
@export var y_field: SpinBox


signal frame_changed(_former_frame, _frame_value)
signal pos_changed(frame, pos)

func _on_frame_value_changed(value):
	frame_changed.emit(value)

func _on_x_value_changed(value):
	pos_changed.emit(Vector2(x_field.value, y_field.value))

func _on_y_value_changed(value):
	pos_changed.emit(Vector2(x_field.value, y_field.value))

func get_sort_value():
	return frame_field.value

func set_pos(pos_arg):
	x_field.value = pos_arg.x
	y_field.value = pos_arg.y
	

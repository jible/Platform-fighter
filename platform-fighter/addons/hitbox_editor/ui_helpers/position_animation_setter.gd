@tool
class_name PositionAnimationSetter
extends HBoxContainer


@export var frame_field: SpinBox
@export var x_field: SpinBox
@export var y_field: SpinBox

var old_frame = 0

signal frame_changed
signal pos_changed

func silent_populate(_frame, _x, _y):
	old_frame = _frame
	set_block_signals(true)
	frame_field.value = _frame
	x_field.value = _x
	y_field.value = _y
	set_block_signals(false)
	
func silent_set_frame(_frame):
	old_frame = _frame
	set_block_signals(true)
	frame_field.value = _frame
	set_block_signals(false)

func _on_frame_value_changed(value):
	if old_frame == value:
		return
	frame_changed.emit()

func _on_x_value_changed(value):
	pos_changed.emit()

func _on_y_value_changed(value):
	pos_changed.emit()

func get_sort_value():
	return frame_field.value

func set_pos(pos_arg):
	x_field.value = pos_arg.x
	y_field.value = pos_arg.y
	

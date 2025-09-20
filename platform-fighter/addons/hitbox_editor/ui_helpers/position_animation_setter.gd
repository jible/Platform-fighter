class_name PositionAnimationSetter
extends HBoxContainer

@export var frame: SpinBox
@export var x: SpinBox
@export var y: SpinBox

func set_value(component, value):
	if self[component]:
		self[component].value = value


func _on_frame_value_changed(value):
	pass # Replace with function body.


func _on_x_value_changed(value):
	pass # Replace with function body.


func _on_y_value_changed(value):
	pass # Replace with function body.

extends Node3D

@export var moveable_object: dp_object

func _process(_delta):
	Input.get_vector("debug_left", "debug_right", "debug_up", "debug_down")

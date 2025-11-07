extends Node2D
@export var rect: DP_Rectangle
var speed = 10

func _physics_process(_delta):
	if Input.is_action_pressed("debug_right"):
		rect.position = rect.position.add(DM_Vector2.from_ints(speed, 0))
	if Input.is_action_pressed("debug_left"):
		rect.position = rect.position.add(DM_Vector2.from_ints(-speed, 0))
	if Input.is_action_pressed("debug_up"):
		rect.position = rect.position.add(DM_Vector2.from_ints(0, -speed))
	if Input.is_action_pressed("debug_down"):
		rect.position = rect.position.add(DM_Vector2.from_ints(0, speed))
	

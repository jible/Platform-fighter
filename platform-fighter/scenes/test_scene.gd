extends Node2D

@export var slow_speed: float = .05

func _process(_delta):
	if Input.is_action_just_pressed("debug_slow_game"):
		Engine.time_scale = slow_speed   # half speed (0.1 = very slow, 2.0 = double speed)
	elif Input.is_action_just_released("debug_slow_game"):
		Engine.time_scale = 1

extends CharacterBody2D

var prev_grounded: bool = false
@export var state_machine: CharacterStateMachine
signal landed

func _physics_process(_delta):
	move_and_slide()
	
	if !prev_grounded and is_on_floor():
		emit_signal("landed")

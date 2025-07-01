extends CharacterBody2D

var prev_grounded: bool = false
var lock_level: CharacterState.LockLevel
@export var state_machine: CharacterStateMachine
@export var mobility_manager: MobilityManager 

signal landed
signal lock_level_changed

func _physics_process(_delta):
	move_and_slide()
	
	if !prev_grounded and is_on_floor():
		emit_signal("landed")


func set_lock_level(new_lock_level: CharacterState.LockLevel):
	if lock_level != new_lock_level:
		lock_level = new_lock_level
		emit_signal("lock_level_changed", lock_level)

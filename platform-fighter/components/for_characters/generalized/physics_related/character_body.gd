extends CharacterBody2D

var prev_grounded: bool = false
var lock_level: CharacterState.LockLevel

@export var base_character : BaseCharacter
@export var state_machine: CharacterStateMachine
@export var mobility_manager: MobilityManager 
@export var sprite_manager: SpriteManager
@export var health: Health

var grounded:bool = false

signal landed
signal lock_level_changed

func _physics_process(_delta):
	move_and_slide()
	grounded = is_on_floor()
	if !prev_grounded and grounded:
		emit_signal("landed")
		grounded = true


func set_lock_level(new_lock_level: CharacterState.LockLevel):
	if lock_level != new_lock_level:
		lock_level = new_lock_level
		emit_signal("lock_level_changed", lock_level)

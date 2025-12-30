class_name CharacterState
extends Node

enum LockLevel {
	FREE,
	ACTION_CANCELABLE,
	HURT_CANCELABLE,
	FULL_LOCK,
}
''' Toggle this to prevent this state from being active
For instance, you may inherit a base state but not want to use it.'''
@export var is_active:bool = true
@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine
@export var character_body:SpecializedCharacterBody
@export var input_handler: InputHandler

@export var state_type:SpecializedCharacterBody.state_types = SpecializedCharacterBody.state_types.STANDARD
@export var can_turn_around: bool = true
@export var can_turn_around_before: bool = true

@export var loop: bool = false
var ticks_per_frame: int = 60
'''
Ideally make this a common denominator of 60 for consistent division:
	1,2,4,5,6,10,12,15,20,30
	Other values will be rounded/truncated
	'''

@export var anim_target_frame_rate: float = 1:
	set(value):
		anim_target_frame_rate = value
		ticks_per_frame = round(60 / anim_target_frame_rate)
# The following functions should be overwritten in states that extend this class. 
func update_state(_delta):
	pass

func enter_state():
	pass

func exit_state():
	pass

func turn_off_hitboxes():
	for hitbox in get_children(true):
		if hitbox is Hitbox:
			hitbox.turn_off()
		if hitbox is HitboxCluster:
			hitbox.clear_hit_list()
			
func on_anim_end():
	
	pass

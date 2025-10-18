@tool
extends AnimationPlayer

@export var state_machine: CharacterStateMachine
var rb_playing = false
var current_tick = 0
var current_anim_frame = 0
var current_anim_frame_count = 0


	

func _on_state_machine_state_changed(new_state_node):
	if Engine.is_editor_hint():return

	rb_play_from_start(new_state_node.name)

'''
RB ANIM PLAYER BREAK DOWN

This is a custom animation player made to support rollback netcode. The godot animator is time based,
not tick based.

Using the animation updater, you extract sprite frames from the sprite manager. 
It will set each frame of the anim 1 sec apart. The animator will parse the state to find how many ticks pass
before changing to the next frame.
'''

func rb_safe_pause():
	rb_playing = false

func rb_safe_play():
	rb_playing = true
	
func rb_play_from_start(anim_name: String):
	current_tick = 0
	current_anim_frame = 0
	play(anim_name)
	pause()
	current_anim_frame_count = int( round( current_animation_length) ) + 1
	rb_set_frame(0)
	rb_playing = true
	
func _physics_process(_delta):
	if rb_playing:
		current_tick += 1
		var next_anim_frame = floor(current_tick/ state_machine.current_state_node.ticks_per_frame)
		if  next_anim_frame > current_anim_frame:
			current_anim_frame = next_anim_frame
			if current_anim_frame >= current_anim_frame_count:
				if state_machine.current_state_node.loop:
					rb_play_from_start(current_animation)
				emit_signal("animation_finished", current_animation)
				return
			else:
				rb_set_frame(current_anim_frame)
			



func rb_set_frame(frame):
	seek(float(frame), true)
	pause()

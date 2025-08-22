@tool
extends Node

@export_tool_button("Transfer animations to player") var button = update_player

# This script goes into the sprite manager
# and extracts the sprite animation to the animation player 
# and creates animations/ updates current animations in the animation player


# References
@export var sprite_manager: SpriteManager
@export var animation_player: AnimationPlayer


func update_player(): 
	var anim_names = Array(sprite_manager.sprite_frames.get_animation_names())
	for anim_name in anim_names:
		# Extract anim info from sprite manager
		var speed = sprite_manager.get_animation_speed(anim_name)
		var frame_count =  sprite_manager.get_frame_count(anim_name)
		var loop = sprite_manager.sprite_frames.get_animation_loop(anim_name)
		# Add animation or update animation
		var anim = animation_player.get_animation(anim_name)
		if !anim:
			anim = Animation.new()
			animation_player.add_animation(anim_name, anim)
		if loop:
			anim.loop_mode = Animation.LOOP_LINEAR
		else:
			anim.loop_mode = Animation.LOOP_LINEAR
		anim.length =  frame_count/speed
		
		# TODO: Maybe remove all other anim tracks!
		
		var track = anim.add_track(Animation.TYPE_VALUE)
		anim.track_set_path(track, "%s:frame" %sprite_manager.name)
		
		for key in range(frame_count):
			anim.track_insert_key(track,key * speed, key)
			pass
		continue
		
		
		anim.resource_changed()
		
	animation_player.property_list_changed_notify()

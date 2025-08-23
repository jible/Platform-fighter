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
	# Leave "" blank! This is the name of the default library which is unqiue to eacch animation player
	var anim_library = animation_player.get_animation_library("")
	var anim_names = Array(sprite_manager.sprite_frames.get_animation_names())
	for anim_name in anim_names:
		# Extract anim info from sprite manager
		var speed = sprite_manager.sprite_frames.get_animation_speed(anim_name)
		var frame_count =  sprite_manager.sprite_frames.get_frame_count(anim_name)
		var loop = sprite_manager.sprite_frames.get_animation_loop(anim_name)
		# Add animation or update animation
		var anim = animation_player.get_animation(anim_name)
		if !anim:
			anim = Animation.new()
			anim_library.add_animation(anim_name, anim)
		if loop:
			anim.loop_mode = Animation.LOOP_LINEAR
		else:
			anim.loop_mode = Animation.LOOP_NONE
		anim.length = frame_count/speed
		
		# TODO: Maybe remove all other anim tracks!
		for i in range(anim.get_track_count() - 1, -1, -1):
			var path = anim.track_get_path(i)
			if String(path) == "%s:animation" % sprite_manager.name \
			or String(path) =="%s:frame" %sprite_manager.name:
				anim.remove_track(i)
		
		# Add track that sets animated sprite to the correct animation
		var anim_name_track = anim.add_track(Animation.TYPE_VALUE)
		anim.track_set_path(anim_name_track, "%s:animation" % sprite_manager.name)
		anim.track_insert_key(anim_name_track,0.0, anim_name)
		
		# Make track that sets the correct frame at each key
		var track = anim.add_track(Animation.TYPE_VALUE)
		anim.track_set_path(track, "%s:frame" %sprite_manager.name)
		
		for key in range(frame_count):
			anim.track_insert_key(track,float(key) / float(speed), key)
			pass
		continue
		
		anim.resource_changed()

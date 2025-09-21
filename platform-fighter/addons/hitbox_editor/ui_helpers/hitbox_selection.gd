@tool
extends VBoxContainer


# UI Reference
@export var interpolate: CheckBox
@export var starting_pos_x: SpinBox
@export var starting_pos_y: SpinBox
@export var offset_info: VBoxContainer
@export var turn_on_frames: SpinBox
@export var turn_off_frames: SpinBox
@export var damage_value: SpinBox
@export var damage_slider: Slider
@export var knockback_x: SpinBox
@export var knockback_y: SpinBox

# Nodes
var hitbox:Hitbox
var base_character
var animation_player: AnimationPlayer
var state_node: CharacterState

# Animation Related
var animation_library
var animation: Animation
var hitbox_position_track = null
var sprite_frame_track = null
var hitbox_method_track = null

# Paths
var hitbox_path 
var sprite_manager_path

# Other
var frame_time: float

func update_hitbox(_hitbox, _base_character, _state_node):
	
	# Update node reference
	hitbox = _hitbox
	base_character = _base_character
	animation_player = base_character.animation_player
	state_node= _state_node
	
	# update anim references
	animation_library = animation_player.get_animation_library("")
	animation = animation_library.get_animation(state_node.name)
	
	# update paths
	var sprite_manager = base_character.sprite_manager
	sprite_manager_path = animation_player.owner.get_path_to(sprite_manager)
	
	hitbox_path = animation_player.owner.get_path_to(hitbox)
	# Update track references
	get_track_references()
	# If there is a null track reference, create it 
	auto_fill_tracks()
	
	frame_time = animation.track_get_key_time(sprite_frame_track, 1)
	
	# Extracts keys from anims and populates ui with their info
	set_hitbox_position_keys()
	get_hitbox_method_keys()

func set_hitbox_position_keys():
	offset_info.reset()
	for key in range(animation.track_get_key_count(hitbox_position_track)):
		var time = animation.track_get_key_time(hitbox_position_track, key)
		var hitbox_position = animation.track_get_key_value(hitbox_position_track, key)
		var frame = time_to_frame(time)
		
		# add keys to ui
		var new_field: PositionAnimationSetter = offset_info.add_field()
		new_field.frame_field.value = int(frame)
		new_field.x_field.value = hitbox_position.x
		new_field.y_field.value = hitbox_position.y

func get_hitbox_method_keys():
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		if animation.method_track_get_name(hitbox_method_track,key) == "turn_on":
			var time = animation.track_get_key_time(hitbox_method_track, key)
			var frame = time_to_frame(time)
			turn_on_frames.value = frame
		elif animation.method_track_get_name(hitbox_method_track,key) == "turn_off":
			var time = animation.track_get_key_time(hitbox_method_track, key)
			var frame = time_to_frame(time)
			turn_off_frames.value = frame

func add_position_anim_key(frame, position):
	#TODO Change this to properly extract framerate from anim
	var frame_rate = 1
	var time = frame_rate * frame
	if !hitbox_position_track:
		hitbox_position_track = animation.add_track(Animation.TYPE_VALUE)
		animation.value_track_set_update_mode(hitbox_position_track, Animation.UpdateMode.UPDATE_DISCRETE)
		animation.track_set_interpolation_type(hitbox_position_track, Animation.InterpolationType.INTERPOLATION_NEAREST)
	animation.track_insert_key(hitbox_position_track,time,position)

func get_track_references():
	hitbox_position_track = null
	sprite_frame_track = null
	hitbox_method_track = null
	
	for track in range(animation.get_track_count()):
		var track_type = animation.track_get_type(track)
		var path = animation.track_get_path(track)

		if track_type == Animation.TrackType.TYPE_VALUE:
			if path.get_concatenated_names() == hitbox_path.get_concatenated_names() \
			and path.get_concatenated_subnames() == "position": 
				hitbox_position_track = track
			elif  path.get_concatenated_names() == sprite_manager_path.get_concatenated_names() \
			and path.get_concatenated_subnames() == "frame": 
				sprite_frame_track = track
		elif track_type == Animation.TrackType.TYPE_METHOD:
			if path.get_concatenated_names() == hitbox_path.get_concatenated_names(): 
				hitbox_method_track = track

func _on_interpolate_toggled(value):
	var interpolation_type = Animation.INTERPOLATION_LINEAR if value else Animation.INTERPOLATION_NEAREST
	animation.track_set_interpolation_type(hitbox_position_track, interpolation_type)

# fires when add remove has field added
func _on_position_added(position_animation:PositionAnimationSetter):
	#if position_animation.frame_value
	pass

# Fires when add remove has field removed
func _on_position_removed(position_animation:PositionAnimationSetter):
	pass

func on_position_changed(position_animation:PositionAnimationSetter):
	pass

func _on_pos_anim_value_changed(former_frame, frame, pos):
	var key = animation.track_find_key(hitbox_position_track,frame_to_time(former_frame),Animation.FIND_MODE_NEAREST)
	animation.track_set_key_time(hitbox_position_track , key, frame_to_time(frame))

func auto_fill_tracks():
	if hitbox_position_track == null:
		hitbox_position_track = animation.add_track(Animation.TYPE_VALUE)
		animation.track_set_path(hitbox_position_track, NodePath(String(hitbox_path) + ":position") )
		var default_key = animation.track_insert_key(hitbox_position_track,0,hitbox.position)
		_set_track_defaults(hitbox_position_track)
	if sprite_frame_track == null:
		# TODO Add default key
		sprite_frame_track =animation.add_track(Animation.TYPE_ANIMATION)
		animation.track_set_path(sprite_frame_track, NodePath(String(sprite_manager_path) + ":frame") )
		_set_track_defaults(sprite_frame_track)
	if hitbox_method_track == null:
		# TODO Add default key
		hitbox_method_track =animation.add_track(Animation.TYPE_METHOD)
		animation.track_set_path(hitbox_method_track, hitbox_path)
		_set_track_defaults(hitbox_method_track)

func _set_track_defaults(track):
	animation.track_set_interpolation_type(track, Animation.InterpolationType.INTERPOLATION_NEAREST)
	animation.value_track_set_update_mode(track, Animation.UpdateMode.UPDATE_DISCRETE)

func time_to_frame(time):
	return round(time/frame_time)

func frame_to_time(frame):
	return frame * frame_time

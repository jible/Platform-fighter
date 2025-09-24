@tool
extends VBoxContainer

## Variable Declaration
# UI Reference
@export var interpolate: CheckBox
@export var starting_pos_x: SpinBox
@export var starting_pos_y: SpinBox
@export var offset_info: AddRemoveField
@export var turn_on_frame: SpinBox
@export var turn_off_frame: SpinBox
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
var hitbox_visibility_track = null

# Paths
var hitbox_path 
var sprite_manager_path

# Other
var frame_time: float



# Initialization------------------------------------------------------------------------------------
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
	set_hitbox_method_keys()

func get_track_references():
	hitbox_position_track = null
	sprite_frame_track = null
	hitbox_method_track = null
	hitbox_visibility_track = null
	
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
			elif path.get_concatenated_names() == hitbox_path.get_concatenated_names() \
			and path.get_concatenated_subnames() == "visible":
				hitbox_visibility_track = track 
		elif track_type == Animation.TrackType.TYPE_METHOD:
			if path.get_concatenated_names() == hitbox_path.get_concatenated_names(): 
				hitbox_method_track = track

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
	if hitbox_visibility_track == null:
		hitbox_visibility_track = animation.add_track(Animation.TYPE_VALUE)
		animation.track_set_path(hitbox_visibility_track, hitbox_path)
		_set_track_defaults(hitbox_visibility_track)
	if hitbox_method_track == null:
		# TODO Add default key
		hitbox_method_track =animation.add_track(Animation.TYPE_METHOD)
		animation.track_set_path(hitbox_method_track, hitbox_path)
		_set_track_defaults(hitbox_method_track)

func _set_track_defaults(track):
	animation.track_set_interpolation_type(track, Animation.InterpolationType.INTERPOLATION_NEAREST)
	animation.value_track_set_update_mode(track, Animation.UpdateMode.UPDATE_DISCRETE)



# Animation Extraction and Sync---------------------------------------------------------------------
func set_hitbox_position_keys():
	offset_info.reset()
	for key in range(animation.track_get_key_count(hitbox_position_track)):
		var time = animation.track_get_key_time(hitbox_position_track, key)
		var hitbox_position = animation.track_get_key_value(hitbox_position_track, key)
		var frame = time_to_frame(time)
		
		# add keys to ui
		var new_field: PositionAnimationSetter = offset_info.internal_add_field()
		new_field.silent_populate(int(frame), hitbox_position.x, hitbox_position.y)
		new_field.frame_changed.connect(on_frame_changed.bind(new_field))
		new_field.pos_changed.connect(on_position_changed.bind(new_field))

func set_hitbox_method_keys():
	var has_turn_off = false
	var has_turn_on = false
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		if animation.method_track_get_name(hitbox_method_track,key) == "turn_on":
			has_turn_on = true
			var time = animation.track_get_key_time(hitbox_method_track, key)
			var frame = time_to_frame(time)
			turn_on_frame.set_block_signals(true)
			turn_on_frame.value = frame
			turn_on_frame.set_block_signals(false)
			
		elif animation.method_track_get_name(hitbox_method_track,key) == "turn_off":
			has_turn_off = true
			var time = animation.track_get_key_time(hitbox_method_track, key)
			var frame = time_to_frame(time)
			turn_off_frame.set_block_signals(true)
			turn_off_frame.value = frame
			turn_off_frame.set_block_signals(false)
			
	if !has_turn_on:
		animation.track_insert_key(hitbox_method_track, 0, "turn_on")
	if !has_turn_off:
		animation.track_insert_key(hitbox_method_track, frame_to_time(1), "turn_off")
	
	sync_visibility_track()

func sync_visibility_track():
	# Clear Visibility track
	for key in range(animation.track_get_key_count(hitbox_visibility_track)):
		animation.track_remove_key(hitbox_visibility_track,0)
	# Populate track with keys from method track
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		var time = animation.track_get_key_time(hitbox_method_track, key)
		var frame = time_to_frame(time)
		match animation.method_track_get_name(hitbox_method_track,key):
			"turn_on":
				animation.track_insert_key(hitbox_visibility_track, time, true)
			"turn_off":
				animation.track_insert_key(hitbox_visibility_track, time, false)



# Animation Editing Helpers-------------------------------------------------------------------------
func is_valid_frame(frame):
	for key in range(animation.track_get_key_count(hitbox_position_track)):
		var current_key_frame = time_to_frame(animation.track_get_key_time(hitbox_position_track, key))
		if current_key_frame == frame:
			return false
	return true

func time_to_frame(time):
	return round(time/frame_time)

func frame_to_time(frame):
	return frame * frame_time



# UI Signal Handlers--------------------------------------------------------------------------------
func on_position_changed(position_animation:PositionAnimationSetter):
	print('changed')
	var new_position = Vector2(position_animation.x_field.value, position_animation.y_field.value)
	var frame = position_animation.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame_to_time(frame))
	animation.track_set_key_value(hitbox_position_track, key, new_position)

func _on_interpolate_toggled(value):
	var interpolation_type = Animation.INTERPOLATION_LINEAR if value else Animation.INTERPOLATION_NEAREST
	animation.track_set_interpolation_type(hitbox_position_track, interpolation_type)

func on_frame_changed(position_animation:PositionAnimationSetter):
	print("Frame changed")
	var new_frame = position_animation.frame_field.value
	var old_frame = position_animation.old_frame
	
	if is_valid_frame(new_frame):
		var key = animation.track_find_key(hitbox_position_track, frame_to_time(old_frame))
		animation.track_set_key_time(hitbox_position_track, key, frame_to_time(new_frame))
		position_animation.old_frame = new_frame
		position_animation.old_frame = new_frame

	else:
		print("silent setting")
		position_animation.silent_set_frame(old_frame)

func _on_pos_anim_value_changed(former_frame, frame, pos):
	var key = animation.track_find_key(hitbox_position_track,frame_to_time(former_frame),Animation.FIND_MODE_NEAREST)
	animation.track_set_key_time(hitbox_position_track , key, frame_to_time(frame))


func _on_add_remove_field_field_added(position_animation: PositionAnimationSetter):
	var min_frame = 0
	for key in range(animation.track_get_key_count(hitbox_position_track) ):
		var current_key_frame = time_to_frame(animation.track_get_key_time(hitbox_position_track, key))
		if  current_key_frame == min_frame:
			min_frame += 1
		else: break
	position_animation.silent_set_frame(min_frame)
	var new_key = animation.track_insert_key(hitbox_position_track, frame_to_time(min_frame), Vector2(0,0))
	
	# Connect signals
	position_animation.frame_changed.connect(on_frame_changed.bind(position_animation))
	position_animation.pos_changed.connect(on_position_changed.bind(position_animation))


func _on_add_remove_field_field_removed(position_animation :PositionAnimationSetter):
	var frame = position_animation.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame_to_time(frame))
	animation.track_remove_key(hitbox_position_track, key)

func _on_turn_on_frame_field_value_changed(value):
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		if animation.method_track_get_name(hitbox_method_track, key) == "turn_on":
			animation.track_set_key_time(hitbox_method_track, key, frame_to_time(value))
	sync_visibility_track()

func _on_turn_off_frame_field_value_changed(value):
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		if animation.method_track_get_name(hitbox_method_track, key) == "turn_off":
			animation.track_set_key_time(hitbox_method_track, key, frame_to_time(value))
	sync_visibility_track()

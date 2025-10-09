@tool
extends VBoxContainer

# Character
@export var character_name_label: Label
var base_character: BaseCharacter
var animation_player: AnimationPlayer
var state_machine: CharacterStateMachine
var states: Array[Node]
# State
@export var state_drop_down: OptionButton
var state: CharacterState
var animation_library: AnimationLibrary
var animation: Animation




# Cluster
@export var cluster_drop_down: OptionButton
@export var add_cluster: Button
@export var remove_cluster: Button
var cluster: HitboxCluster
@export var turn_on_frame_field: SpinBox
@export var turn_off_frame_field: SpinBox
var hitboxes = []

# Hitboxes
@export var hitbox_drop_down: OptionButton
@export var add_hitbox_button: Button
@export var remove_hitbox_button: Button
var hitbox: Hitbox

@export var offset_info: AddRemoveField
@export var turn_on_frame: SpinBox
@export var turn_off_frame: SpinBox
@export var damage_value: SpinBox
@export var knockback_vector_x: SpinBox
@export var knockback_vector_y: SpinBox
@export var radius: SpinBox
@export var knockback_magnitude: SpinBox



# Animation
var sprite_manager_path: NodePath
var hitbox_path: NodePath

var sprite_frame_track
var hitbox_position_track
var hitbox_method_track
var hitbox_visibility_track

var frame_time: float

# INITILIZATION ------------------------------------------------------------------------------------

func configure(_character_root: BaseCharacter):
	base_character = _character_root
	character_name_label.text = base_character.name
	animation_player = base_character.animation_player
	state_machine = base_character.state_machine
	states = state_machine.get_children(true)
	state_drop_down.clear()
	if states.size() <= 0:return
	for state in states:
		state_drop_down.add_item(state.name)
	_on_state_drop_down_item_selected(state_drop_down.selected)

func reconfigure():configure(base_character)

# Populates hitbox ui. Call when new state is selected
# Automatically Selects the first hitbox
func update_hitboxes(_hitboxes):
	hitbox_drop_down.clear()
	hitboxes = _hitboxes
	for hitbox in hitboxes:
		hitbox_drop_down.add_item(hitbox.name)
	if hitboxes.size() > 0: # dont select if there is no hitbox
		_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)


	
func set_new_hitbox(new_hitbox):
	animation_library = animation_player.get_animation_library("")
	animation = animation_library.get_animation(state.name)
	
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
	
	secret_set(damage_value, hitbox.damage)
	secret_set(knockback_vector_x, hitbox.knockback_vector.x)
	secret_set(knockback_vector_y, hitbox.knockback_vector.y)
	secret_set(radius, hitbox.collision_shape.shape.radius)
	secret_set(knockback_magnitude, hitbox.knockback_magnitude)

func secret_set(obj, value):
	obj.set_block_signals(true)
	obj.value = value
	obj.set_block_signals(false)

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
# Animation Extraction and Sync---------------------------------------------------------------------
func auto_fill_tracks():
	if hitbox_position_track == null:
		hitbox_position_track = animation.add_track(Animation.TYPE_VALUE)
		animation.track_set_path(hitbox_position_track, NodePath(String(hitbox_path) + ":position") )
		var default_key = animation.track_insert_key(hitbox_position_track,0,hitbox.position)
		_set_track_defaults(hitbox_position_track)
	if sprite_frame_track == null:
		# TODO Add default key
		sprite_frame_track =animation.add_track(Animation.TYPE_VALUE)
		animation.track_set_path(sprite_frame_track, NodePath(String(sprite_manager_path) + ":frame") )
		_set_track_defaults(sprite_frame_track)
	if hitbox_visibility_track == null:
		hitbox_visibility_track = animation.add_track(Animation.TYPE_VALUE)
		animation.track_set_path(hitbox_visibility_track, NodePath(String(hitbox_path) + ":visible"))
		_set_track_defaults(hitbox_visibility_track)
		var default_key = animation.track_insert_key(hitbox_visibility_track, 0,false)
	if hitbox_method_track == null:
		# TODO Add default key
		hitbox_method_track =animation.add_track(Animation.TYPE_METHOD)
		animation.track_set_path(hitbox_method_track, hitbox_path)
		

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
			old_turn_on_frame = frame
			turn_on_frame.set_block_signals(false)
	
	
		elif animation.method_track_get_name(hitbox_method_track,key) == "turn_off":
			has_turn_off = true
			var time = animation.track_get_key_time(hitbox_method_track, key)
			var frame = time_to_frame(time)
			turn_off_frame.set_block_signals(true)
			turn_off_frame.value = frame
			old_turn_off_frame = frame
			turn_off_frame.set_block_signals(false)
			
	if !has_turn_on:
		animation.track_insert_key(hitbox_method_track, 0, {
			"method" : "turn_on",
			"args":[],
		})
		turn_on_frame.value = animation.track_get_key_time(hitbox_method_track, 0)
		
	if !has_turn_off:
		animation.track_insert_key(hitbox_method_track, frame_to_time(1), {
			"method" : "turn_off",
			"args":[],
		})
		turn_off_frame.value = animation.track_get_key_time(hitbox_method_track, 1)
		
	
	
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



func _set_track_defaults(track):
	animation.track_set_interpolation_type(track, Animation.InterpolationType.INTERPOLATION_NEAREST)
	animation.value_track_set_update_mode(track, Animation.UpdateMode.UPDATE_DISCRETE)

# Animation Editing Helpers-------------------------------------------------------------------------

func time_to_frame(time):
	return round(time/frame_time)
	
func frame_to_time(frame):
	return frame * frame_time

func is_valid_frame(frame):
	for key in range(animation.track_get_key_count(hitbox_position_track)):
		var current_key_frame = time_to_frame(animation.track_get_key_time(hitbox_position_track, key))
		if current_key_frame == frame:
			return false
	return true
# UI Signal Handlers--------------------------------------------------------------------------------

func on_frame_changed(position_animation:PositionAnimationSetter):

	var new_frame = position_animation.frame_field.value
	var old_frame = position_animation.old_frame
	
	if is_valid_frame(new_frame):
		var key = animation.track_find_key(hitbox_position_track, frame_to_time(old_frame))
		animation.track_set_key_time(hitbox_position_track, key, frame_to_time(new_frame))
		position_animation.old_frame = new_frame
		position_animation.old_frame = new_frame

	else:
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

var old_turn_on_frame = null

func _on_turn_on_frame_field_value_changed(value):
	var turn_on_key
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		var method_name = animation.method_track_get_name(hitbox_method_track, key)
		if method_name == "turn_on":
			turn_on_key = key
		elif method_name == "turn_off":
			if value == time_to_frame(animation.track_get_key_time(hitbox_method_track, key)):
				turn_on_frame.set_block_signals(true)
				turn_on_frame.value = old_turn_on_frame
				turn_on_frame.set_block_signals(false)
				return
	animation.track_set_key_time(hitbox_method_track, turn_on_key, frame_to_time(value))
	animation_player.seek(frame_to_time(value))
	animation_player.advance(0)
	animation_player.pause()
	old_turn_on_frame = value
	sync_visibility_track()

var old_turn_off_frame = null

func _on_turn_off_frame_field_value_changed(value):
	var turn_off_key
	
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		var method_name = animation.method_track_get_name(hitbox_method_track, key)
		if method_name == "turn_off":
			turn_off_key = key
		elif method_name == "turn_on":
			if value == time_to_frame(animation.track_get_key_time(hitbox_method_track, key)):
				turn_off_frame.set_block_signals(true)
				turn_off_frame.value = old_turn_off_frame
				turn_off_frame.set_block_signals(false)
				return
	animation.track_set_key_time(hitbox_method_track, turn_off_key, frame_to_time(value))
	old_turn_off_frame = value
	animation_player.seek(frame_to_time(value))
	animation_player.advance(0)
	animation_player.pause()
	sync_visibility_track()

func on_position_changed(position_animation:PositionAnimationSetter):
	print('changed')
	var new_position = Vector2(position_animation.x_field.value, position_animation.y_field.value)
	var frame = position_animation.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame_to_time(frame))
	animation.track_set_key_value(hitbox_position_track, key, new_position)
	animation_player.seek(frame_to_time(frame))
	animation_player.advance(0)
	animation_player.pause()



# Signal Handlers ----------------------------------------------------------------------------------
func _on_state_drop_down_item_selected(index):
	pass # Replace with function body.

func _on_refresh_button_pressed(): reconfigure()

func _on_hitbox_drop_down_item_selected(index):
	var new_hitbox = state.find_child(hitbox_drop_down.get_item_text(index))
	if !new_hitbox:
		push_error("Could not find hitbox")
		set_new_hitbox(new_hitbox)

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
var clusters: Array[HitboxCluster] = []
@export var cluster_drop_down: OptionButton
@export var add_cluster: Button
@export var remove_cluster: Button
var cluster: HitboxCluster
@export var cluster_turn_on_frame_field: SpinBox
@export var cluster_turn_off_frame_field: SpinBox
var hitboxes = []

# Hitboxes
@export var full_hitbox_ui_container: VBoxContainer
@export var hitbox_turn_on_frame_field: SpinBox
@export var hitbox_turn_off_frame_field: SpinBox
@export var hitbox_drop_down: OptionButton
@export var add_hitbox_button: Button
@export var remove_hitbox_button: Button
var hitbox: Hitbox

@export var offset_info: AddRemoveField
@export var damage_value: SpinBox
@export var knockback_vector_x: SpinBox
@export var knockback_vector_y: SpinBox
@export var height_selection: SpinBox
@export var radius_selection: SpinBox
@export var rotation_selection: SpinBox
@export var knockback_magnitude: SpinBox



# Animation
@export var frame_slider: HSlider
var sprite_manager_path: NodePath
var hitbox_path: NodePath
var cluster_path: NodePath

var cluster_method_track
var cluster_child_visibility_tracks = []


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

# Response to state selection-----------------------------------------------------------------------
func _on_state_drop_down_item_selected(index):
	var current_state_name = state_drop_down.get_item_text(index)
	state = state_machine.find_child(current_state_name)
	if !state:
		print("state does not exist")
		return
		
	animation_library = animation_player.get_animation_library("")
	animation = animation_library.get_animation(state.name)
	sprite_frame_track = get_track_reference( Animation.TrackType.TYPE_VALUE, sprite_manager_path.get_concatenated_names(), "frame")
	if !sprite_frame_track:
		print("SpriteFrame does not have anim track. Use animation updater to add the anim track then click 'Refresh'")
		return
	var sprite_manager = base_character.sprite_manager
	sprite_manager_path = animation_player.owner.get_path_to(sprite_manager)
	
	frame_time = animation.track_get_key_time(sprite_frame_track, 1)
	
	frame_slider.max_value = animation_player.get_animation(state.name).length
	animation_player.current_animation = state.name
	animation_player.seek(0,true)
	animation_player.pause()
	update_clusters()

func reconfigure():configure(base_character)

# Updates cluster ui and selects first option
func update_clusters():
	cluster_drop_down.clear()
	# As of right now, this will break if you name a cluster none
	cluster_drop_down.add_item("None")
	get_clusters()
	for _cluster in clusters:
		cluster_drop_down.add_item(_cluster.name)
	# Not sure if this emits the signal or not
	cluster_drop_down.select(0)
	_on_cluster_drop_down_item_selected(cluster_drop_down.selected)

# RESPONSE TO CLUSTER SELECTION---------------------------------------------------------------------
func _on_cluster_drop_down_item_selected(index):
	var selected_name = cluster_drop_down.get_item_text(index)
	cluster = null
	cluster_path = ""
	if selected_name != "none":
		cluster = state.find_child(selected_name, false)
		cluster_path = animation_player.owner.get_path_to(cluster)
	cluster_turn_off_frame_field.visible = (cluster != null)
	cluster_turn_on_frame_field.visible = (cluster != null)
	
	hitboxes = cluster.find_children("", "HitboxCluster", false)
	
	populate_and_get_cluster_track_references()
	sync_visibility_track(hitbox_method_track, hitbox_visibility_track)
	update_hitboxes()


func populate_and_get_cluster_track_references():
	cluster_method_track = get_track_reference(Animation.TrackType.TYPE_METHOD, cluster_path.get_concatenated_names())
	if !cluster_method_track: 
		cluster_method_track = create_track(Animation.TrackType.TYPE_METHOD, cluster_path.get_concatenated_names())
	
	
	cluster_child_visibility_tracks = []
	for cluster_child in hitboxes:
		var cluster_child_path = animation_player.owner.get_path_to(cluster_child)
		var track = get_track_reference( Animation.TrackType.TYPE_VALUE, cluster_child_path.get_concatenated_names(), "visible")
		if track:
			cluster_child_visibility_tracks.append(track)
			continue
		var new_track = create_track(Animation.TrackType.TYPE_VALUE, cluster_child_path, "visible")
		cluster_child_visibility_tracks.append(new_track)


# Populates hitbox ui. Call when new state is selected
# Automatically selects the first hitbox
func update_hitboxes():
	get_hitboxes()
	hitbox_drop_down.clear()
	for _hitbox in hitboxes:
		hitbox_drop_down.add_item(_hitbox.name)
	_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)

# RESPONSE TO HITBOX SELECTION----------------------------------------------------------------------
# When a new hitbox is selected, hit ui if there is no hitbox,
# get a reference to the true hitbox,
# get the animation paths for that hitbox,
# If it belongs to a cluster, hide its toggle fields
# populate the ui
func _on_hitbox_drop_down_item_selected(index):
	if index == -1:
		full_hitbox_ui_container.hide()
		return
	full_hitbox_ui_container.show()
	var new_hitbox = state.find_child(hitbox_drop_down.get_item_text(index))
	if !new_hitbox:
		push_error("Could not find hitbox")
		return
	hitbox_path = animation_player.owner.get_path_to(hitbox)

	populate_and_get_hitbox_track_reference()
	
	hitbox_turn_on_frame_field.visibility  = (cluster == null)
	hitbox_turn_off_frame_field.visibility = (cluster == null)
	# Extracts keys from anims and populates ui with their info
	set_hitbox_position_keys()
	set_hitbox_toggle_keys()
	
	damage_value.set_value_no_signal(hitbox.damage)
	knockback_vector_x.set_value_no_signal( hitbox.knockback_vector.x)
	knockback_vector_y.set_value_no_signal(hitbox.knockback_vector.y)
	knockback_magnitude.set_value_no_signal(hitbox.knockback_magnitude)
	radius_selection.set_value_no_signal(hitbox.collision_shape.shape.radius)
	height_selection.set_value_no_signal(hitbox.collision_shape.shape.height)
	rotation_selection.set_value_no_signal(hitbox.rotation_degrees)
	

func populate_and_get_hitbox_track_reference():
	hitbox_method_track = get_track_reference( Animation.TrackType.TYPE_METHOD, hitbox_path.get_concatenated_names())
	hitbox_position_track = get_track_reference( Animation.TrackType.TYPE_VALUE, hitbox_path.get_concatenated_names(), "position")
	hitbox_visibility_track = get_track_reference( Animation.TrackType.TYPE_VALUE, hitbox_path.get_concatenated_names(), "visible")
	# If there is a null track reference, create it 
	if hitbox_position_track == null:
		hitbox_position_track = create_track( Animation.TYPE_VALUE, hitbox_path, "position", hitbox.position)
	if hitbox_visibility_track == null:
		hitbox_visibility_track = create_track( Animation.TYPE_VALUE, hitbox_path, "visible")
	if hitbox_method_track == null:
		hitbox_method_track = create_track( Animation.TYPE_METHOD, hitbox_path)

# Helper function for making tracks and optionally adding a default key
# Does not automatically populate with key if default value is set to null
func create_track( anim_type, node_path, attribute = null, default_value = null, interpolation_type = Animation.InterpolationType.INTERPOLATION_NEAREST, update_mode = Animation.UpdateMode.UPDATE_DISCRETE):
	var new_track = animation.add_track(Animation.TYPE_VALUE)
	var track_path = NodePath(String(node_path) +  (":" +attribute) if attribute else "" )
	animation.track_set_path(new_track,  track_path)
	if default_value:
		animation.track_insert_key(new_track,0,default_value)
	if anim_type != Animation.TYPE_METHOD:
		animation.track_set_interpolation_type(new_track, interpolation_type)
		animation.value_track_set_update_mode(new_track, update_mode)
	return new_track


# Slightly expensive aproach to extracting paths, but the code is so much cleaner to use
func get_track_reference(target_track_type, target_track_path, target_track_sub_name = null):
	for track in range(animation.get_track_count()):
		var track_type = animation.track_get_type(track)
		var path = animation.track_get_path(track)
		if track_type == target_track_type:
			if path.get_concatenated_names() == target_track_path \
			and ( target_track_sub_name == null or path.get_concatenated_subnames() == target_track_sub_name): 
				return track
	return null

# Updates ui to match hitbox pos animation
func set_hitbox_position_keys():
	offset_info.reset()
	for key in range(animation.track_get_key_count(hitbox_position_track)):
		var time = animation.track_get_key_time(hitbox_position_track, key)
		var hitbox_position = animation.track_get_key_value(hitbox_position_track, key)
		var frame = time_to_frame(time)
		
		# add keys to ui
		var new_field: PositionAnimationSetter = offset_info.internal_add_field()
		new_field.silent_populate(int(frame), hitbox_position.x, hitbox_position.y)
		new_field.frame_changed.connect(on_hitbox_offset_frame_changed.bind(new_field))
		new_field.pos_changed.connect(on_hitbox_offset_position_changed.bind(new_field))

func set_hitbox_toggle_keys():
	var method_frames = extract_and_correct_on_off_time(hitbox_method_track)
	hitbox_turn_on_frame_field.value = method_frames["turn_on"]
	hitbox_turn_off_frame_field.value = method_frames["turn_off"]
	
	sync_visibility_track(hitbox_method_track, hitbox_visibility_track)


# Automatically parses given track for keys that call turn on and turn off
# If more or less than 1 key for either function, corrects it. Then returns the method frames
func extract_and_correct_on_off_time(track):
	var method_frames = {
		"turn_on": null,
		"turn_off" : null,
	}
	var default_key_frame = 0
	for key in range(animation.track_get_key_count(track) -1, -1, -1):
		var time = animation.track_get_key_time(track, key)
		var frame = time_to_frame(time)
		var method_name =  animation.method_track_get_name(track,key)
		if method_name == "turn_on" or method_name == "turn_off":
			if method_frames[method_name] != null:
				animation.track_remove_key(track,key)
				continue
			method_frames[method_name] = frame
			if default_key_frame == frame: default_key_frame += 1
	
	for key in method_frames.keys():
		if method_frames[key] == null:
			animation.track_insert_key(track, frame_to_time(default_key_frame), {
				"method" : key,
				"args":[],
			})
			method_frames[key] = default_key_frame
			default_key_frame += 1
	return method_frames


# Makes hitbox visibility track match that of the on and off keys of the method track (this allows hitbox toggling visualization in editor.
func sync_visibility_track(method_track, visibility_track):
	# Clear Visibility track
	for key in range(animation.track_get_key_count(visibility_track)):
		animation.track_remove_key(visibility_track,0)
	# Populate track with keys from method track
	for key in range(animation.track_get_key_count(method_track)):
		var time = animation.track_get_key_time(method_track, key)
		match animation.method_track_get_name(method_track,key):
			"turn_on":
				animation.track_insert_key(visibility_track, time, true)
			"turn_off":
				animation.track_insert_key(visibility_track, time, false)

# Reference collecting helpers----------------------------------------------------------------------
func get_hitboxes():
	var parent = cluster if cluster else state
	hitboxes = parent.find_children("", "Hitbox", false)

func get_clusters():
	clusters = []
	var clusters = state.find_children("", "HitboxCluster" , false)

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
func on_hitbox_offset_frame_changed(position_animation:PositionAnimationSetter):

	var new_frame = position_animation.frame_field.value
	var old_frame = position_animation.old_frame
	
	if is_valid_frame(new_frame):
		var key = animation.track_find_key(hitbox_position_track, frame_to_time(old_frame))
		animation.track_set_key_time(hitbox_position_track, key, frame_to_time(new_frame))
		position_animation.old_frame = new_frame
		position_animation.old_frame = new_frame

	else:
		position_animation.silent_set_frame(old_frame)

func on_hitbox_offset_position_changed(position_animation:PositionAnimationSetter):
	print('changed')
	var new_position = Vector2(position_animation.x_field.value, position_animation.y_field.value)
	var frame = position_animation.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame_to_time(frame))
	animation.track_set_key_value(hitbox_position_track, key, new_position)
	animation_player.seek(frame_to_time(frame))
	animation_player.advance(0)
	animation_player.pause()


# Signal Handlers ----------------------------------------------------------------------------------


func _on_refresh_button_pressed(): reconfigure()


func _on_cluster_turn_on_selection_value_changed(value):
	var turn_on_key
	for key in range(animation.track_get_key_count(cluster_method_track)):
		var method_name = animation.method_track_get_name(cluster_method_track, key)
		if method_name == "turn_on":
			turn_on_key = key
		elif method_name == "turn_off":
			if value == time_to_frame(animation.track_get_key_time(cluster_method_track, key)):
				cluster_turn_on_frame_field.set_value_no_signal(value + 1)
	animation.track_set_key_time(cluster_method_track, turn_on_key, frame_to_time(cluster_turn_on_frame_field.value))
	animation_player.seek(frame_to_time(cluster_turn_on_frame_field.value))
	animation_player.advance(0)
	animation_player.pause()
	for child_vis_track in cluster_child_visibility_tracks:
		sync_visibility_track(cluster_method_track, child_vis_track)


func _on_cluster_turn_off_selection_value_changed(value):
	var turn_off_key
	for key in range(animation.track_get_key_count(cluster_method_track)):
		var method_name = animation.method_track_get_name(cluster_method_track, key)
		if method_name == "turn_off":
			turn_off_key = key
		elif method_name == "turn_on":
			if value == time_to_frame(animation.track_get_key_time(cluster_method_track, key)):
				cluster_turn_on_frame_field.set_value_no_signal(value + 1)
	animation.track_set_key_time(cluster_method_track, turn_off_key, frame_to_time(cluster_turn_on_frame_field.value))
	animation_player.seek(frame_to_time(cluster_turn_on_frame_field.value))
	animation_player.advance(0)
	animation_player.pause()
	for child_vis_track in cluster_child_visibility_tracks:
		sync_visibility_track(cluster_method_track, child_vis_track)

# Non-anim ui signal responses----------------------------------------------------------------------
func _on_radius_selection_value_changed(value):
	var chilren = hitbox.get_children()
	var collision_area:CollisionShape2D
	for child in chilren:
		if child is CollisionShape2D:
			collision_area = child
	if !(collision_area is CollisionShape2D):
		return
	collision_area.shape.radius = value


func _on_height_selection_value_changed(value):
	var chilren = hitbox.get_children()
	var collision_area:CollisionShape2D
	for child in chilren:
		if child is CollisionShape2D:
			collision_area = child
	if !(collision_area is CollisionShape2D):
		return
	collision_area.shape.height = value

func _on_rotation_selection_value_changed(value):
	hitbox.rotation_degrees = value

func _on_knockback_magnitude_value_value_changed(value):
	hitbox.knockback_magnitude = value

func _on_knockback_x_value_changed(value):
	hitbox.knockback_vector.x = value

func _on_knockback_y_value_changed(value):
	hitbox.knockback_vector.y = value

func _on_normalize_pressed():
	hitbox.knockback_vector = hitbox.knockback_vector.normalized()

func _on_damage_selection_value_changed(value):
	hitbox.damage = value

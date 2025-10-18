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
var no_cluster_selected_name: String = "None"
var clusters: Array[Node] = []
@export var cluster_drop_down: OptionButton
@export var add_cluster: Button
@export var remove_cluster: Button
var cluster: HitboxCluster
var hitboxes = []

# Hitboxes
var hitbox_scene = preload("uid://dosttxqhf11ww")
@export var full_hitbox_ui_container: VBoxContainer
@export var hitbox_turn_on_frame_field: SpinBox
@export var hitbox_turn_off_frame_field: SpinBox
@export var hitbox_drop_down: OptionButton
@export var add_hitbox_button: Button
@export var remove_hitbox_button: Button
@export var hitbox_toggle_fields: VBoxContainer
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
	
	var sprite_manager = base_character.sprite_manager
	if !sprite_manager:
		push_error("Sprite manager reference not set on base character. Run the reference server")
		return
	sprite_manager_path = animation_player.owner.get_path_to(sprite_manager)
	
	sprite_frame_track = get_track_reference( Animation.TrackType.TYPE_VALUE, sprite_manager_path.get_concatenated_names(), "frame")
	
	
	if !sprite_frame_track:
		print("SpriteFrame does not have anim track. Use animation updater to add the anim track then click 'Refresh'")
		return
	
	
	frame_time = animation.track_get_key_time(sprite_frame_track, 1)
	
	frame_slider.max_value = animation_player.get_animation(state.name).length
	animation_player.current_animation = state.name
	animation_player.seek(0,true)
	animation_player.pause()
	update_clusters()

func reconfigure():configure(base_character)

# Updates cluster ui and selects first option
func update_clusters(cluster_index = 0):
	cluster_drop_down.clear()
	# As of right now, this will break if you name a cluster none
	cluster_drop_down.add_item(no_cluster_selected_name)
	get_clusters()
	for _cluster in clusters:
		cluster_drop_down.add_item(_cluster.name)
	# Not sure if this emits the signal or not
	cluster_drop_down.select(cluster_index)
	_on_cluster_drop_down_item_selected(cluster_drop_down.selected)

# RESPONSE TO CLUSTER SELECTION---------------------------------------------------------------------
func _on_cluster_drop_down_item_selected(index):
	var selected_name = cluster_drop_down.get_item_text(index)
	cluster = null
	if selected_name != no_cluster_selected_name:
		cluster = state.find_child(selected_name, false)
		hitboxes = cluster.find_children("", "Hitbox", false)
	else: hitboxes = null
	update_hitboxes()


# Populates hitbox ui. Call when new state is selected
# Automatically selects the first hitbox
func update_hitboxes():
	get_hitboxes()
	hitbox_drop_down.clear()
	for _hitbox in hitboxes:
		hitbox_drop_down.add_item(_hitbox.name)
	hitbox_drop_down.select(-1)
	_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)

# RESPONSE TO HITBOX SELECTION----------------------------------------------------------------------
# When a new hitbox is selected, hit ui if there is no hitbox,
# get a reference to the true hitbox,
# get the animation paths for that hitbox,
# If it belongs to a cluster, hide its toggle fields
# populate the ui
func _on_hitbox_drop_down_item_selected(index):
	if index == -1:
		hitbox = null
		full_hitbox_ui_container.hide()
		return
	full_hitbox_ui_container.show()
	hitbox = (cluster if cluster else state).find_child(hitbox_drop_down.get_item_text(index), false)
	if !hitbox:
		push_error("Could not find hitbox")
		return
	hitbox_path = animation_player.owner.get_path_to(hitbox)

	populate_and_get_hitbox_track_reference()
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
		hitbox_position_track = create_track( Animation.TrackType.TYPE_VALUE, hitbox_path, "position")
	if hitbox_visibility_track == null:
		hitbox_visibility_track = create_track( Animation.TrackType.TYPE_VALUE, hitbox_path, "visible")
	if hitbox_method_track == null:
		hitbox_method_track = create_track( Animation.TrackType.TYPE_METHOD, hitbox_path)
	
	if animation.track_get_key_count(hitbox_position_track) < 1:
		animation.track_insert_key(hitbox_position_track, 0, hitbox.position)
	if animation.track_get_key_count(hitbox_visibility_track) < 1:
		animation.track_insert_key(hitbox_visibility_track, 0, true)
# Helper function for making tracks and optionally adding a default key
# Does not automatically populate with key if default value is set to null


func create_track( anim_type, node_path: NodePath, attribute = null):
	if !node_path:
		push_error("Node Path null. Cannont construct path")
		return
	
	var new_track = animation.add_track(anim_type)
	var track_path = ( NodePath(String(node_path) +  ":" +attribute) if attribute else node_path )
	animation.track_set_path(new_track,  track_path)
	
	if anim_type != Animation.TYPE_METHOD:
		var interpolation_type = Animation.InterpolationType.INTERPOLATION_NEAREST
		var update_mode = Animation.UpdateMode.UPDATE_DISCRETE
		
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
		var frame = time
		
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
		var frame = time
		var method_name =  animation.method_track_get_name(track,key)
		if method_name == "turn_on" or method_name == "turn_off":
			if method_frames[method_name] != null:
				animation.track_remove_key(track,key)
				continue
			method_frames[method_name] = frame
			if default_key_frame == frame: default_key_frame += 1
	
	for key in method_frames.keys():
		if method_frames[key] == null:
			animation.track_insert_key(track, float(default_key_frame), {
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
	clusters = state.find_children("", "HitboxCluster" , false)

# Animation Editing Helpers-------------------------------------------------------------------------


func is_valid_frame(frame):
	for key in range(animation.track_get_key_count(hitbox_position_track)):
		var current_key_frame =animation.track_get_key_time(hitbox_position_track, key)
		if current_key_frame == frame:
			return false
	return true

# UI Signal Handlers--------------------------------------------------------------------------------
func on_hitbox_offset_frame_changed(position_animation:PositionAnimationSetter):

	var new_frame = position_animation.frame_field.value
	var old_frame = position_animation.old_frame
	
	if is_valid_frame(new_frame):
		var key = animation.track_find_key(hitbox_position_track, old_frame)
		animation.track_set_key_time(hitbox_position_track, key, new_frame)
		position_animation.old_frame = new_frame

	else:
		position_animation.silent_set_frame(old_frame)

func on_hitbox_offset_position_changed(position_animation:PositionAnimationSetter):
	var new_position = Vector2(position_animation.x_field.value, position_animation.y_field.value)
	var frame = position_animation.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame)
	animation.track_set_key_value(hitbox_position_track, key, new_position)
	animation_player.seek(frame)
	animation_player.advance(0)
	animation_player.pause()


# Signal Handlers ----------------------------------------------------------------------------------


func _on_refresh_button_pressed(): reconfigure()


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
	knockback_vector_x.value = hitbox.knockback_vector.x
	knockback_vector_y.value = hitbox.knockback_vector.y
	

func _on_damage_selection_value_changed(value):
	hitbox.damage = value

# HITBOX POSITION ANIMATION MODIFICATIONS
func _on_offset_add_remove_field_field_added(field):
	var min_frame = 0
	for key in range(animation.track_get_key_count(hitbox_position_track) ):
		var current_key_frame = animation.track_get_key_time(hitbox_position_track, key)
		if  current_key_frame == min_frame:
			min_frame += 1
		else: break
	field.silent_set_frame(min_frame)
	var new_key = animation.track_insert_key(hitbox_position_track, min_frame, Vector2(0,0))
	
	# Connect signals
	field.frame_changed.connect(on_hitbox_pos_frame_changed.bind(field))
	field.pos_changed.connect(on_position_anim_position_changed.bind(field))
	
	


func _on_offset_add_remove_field_field_removed(field):
	var frame = field.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame)
	animation.track_remove_key(hitbox_position_track, key)

func on_hitbox_pos_frame_changed(position_animation:PositionAnimationSetter):
	var new_frame = position_animation.frame_field.value
	var old_frame = position_animation.old_frame
	
	if is_valid_frame(new_frame):
		var key = animation.track_find_key(hitbox_position_track,old_frame)
		animation.track_set_key_time(hitbox_position_track, key, new_frame)
		position_animation.old_frame = new_frame
		position_animation.old_frame = new_frame
	else:
		position_animation.silent_set_frame(old_frame)

func on_position_anim_position_changed(position_animation:PositionAnimationSetter):
	var new_position = Vector2(position_animation.x_field.value, position_animation.y_field.value)
	var frame = position_animation.frame_field.value
	var key = animation.track_find_key(hitbox_position_track, frame)
	animation.track_set_key_value(hitbox_position_track, key, new_position)
	animation_player.seek(frame)
	animation_player.advance(0)
	animation_player.pause()


func _on_add_cluster_button_pressed():
	var new_cluster = HitboxCluster.new()
	
	state.add_child(new_cluster)
	new_cluster.owner = base_character
	new_cluster.name = make_cluster_name()
	cluster_drop_down.add_item(new_cluster.name)
	cluster_drop_down.select(cluster_drop_down.item_count  - 1)
	_on_cluster_drop_down_item_selected(cluster_drop_down.selected)

func make_cluster_name()-> String:
	var cluster_count= state.find_children("", "HitboxCluster").size()
	return "Cluster%d" % cluster_count

func _on_remove_cluster_button_pressed():
	if !cluster: return
	cluster_drop_down.remove_item(cluster_drop_down.selected)
	
	#this shadows another variable
	var relevant_paths = []
	for hitbox in hitboxes:
		relevant_paths.append(animation_player.owner.get_path_to(hitbox).get_concatenated_names())
	for track in range(animation.get_track_count() -1 , -1, -1):
		var track_path = animation.track_get_path(track).get_concatenated_names()
		if relevant_paths.find(track_path) != -1:
			animation.remove_track(track)
		
	state.remove_child(cluster)
	cluster.queue_free()
	cluster = null
	cluster_drop_down.select(0)
	
	_on_cluster_drop_down_item_selected(cluster_drop_down.selected)
	

func _on_remove_hitbox_button_pressed():
	if !hitbox: return
	remove_hitbox_anims()
	var parent = cluster if cluster else state
	parent.remove_child(hitbox)
	hitbox.queue_free()
	hitbox_drop_down.remove_item(hitbox_drop_down.selected)
	# Reselect the cluster, so it automatically reconfigues is hitbox array 
	# and other related variables. It will automatically select a new hitbox or tab to none
	update_clusters(cluster_drop_down.selected)

func remove_hitbox_anims():
	var tracks = [hitbox_position_track, hitbox_visibility_track, hitbox_method_track]
	# Itterate backwards so you don't delete one, changing the track id of another.
	tracks.sort()
	tracks.reverse()
	for track in tracks:
		animation.remove_track(track)
	hitbox_position_track = null
	hitbox_visibility_track = null
	hitbox_method_track = null

func _on_add_hitbox_button_pressed():
	#TODO change to instance hitbox instead of area 2d and attatching script
	var new_hitbox: Hitbox = hitbox_scene.instantiate()
	
	
	
	var collision = CollisionShape2D.new()
	new_hitbox.collision_shape = collision
	
	var shape = CapsuleShape2D.new()
	collision.shape = shape
	collision.debug_color = Color("2ea06c79")
	new_hitbox.add_child(collision)
	# Only add it to scene once everything else is ready (so obj has refrences for _ready())
	new_hitbox.base_character = base_character
	
	var parent = cluster if cluster else state
	parent.add_child(new_hitbox)
	new_hitbox.name = "hitbox"
	collision.name = "collision"
	new_hitbox.owner = get_tree().edited_scene_root
	collision.owner = get_tree().edited_scene_root

	hitbox_drop_down.add_item(new_hitbox.name)
	hitbox_drop_down.select(hitbox_drop_down.item_count - 1)
	
	get_hitboxes()
	_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)





func _on_hitbox_turn_off_selection_value_changed(value):
	var turn_off_key
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		var method_name = animation.method_track_get_name(hitbox_method_track, key)
		if method_name == "turn_off":
			turn_off_key = key
		elif method_name == "turn_on":
			if value == animation.track_get_key_time(hitbox_method_track, key):
				hitbox_turn_off_frame_field.set_value_no_signal(value + 1)
	animation.track_set_key_time(hitbox_method_track, turn_off_key, hitbox_turn_off_frame_field.value)
	animation_player.seek(hitbox_turn_off_frame_field.value)
	animation_player.advance(0)
	animation_player.pause()
	sync_visibility_track(hitbox_method_track, hitbox_visibility_track)


func _on_hitbox_turn_on_selection_value_changed(value):
	var turn_on_key
	for key in range(animation.track_get_key_count(hitbox_method_track)):
		var method_name = animation.method_track_get_name(hitbox_method_track, key)
		if method_name == "turn_on":
			turn_on_key = key
		elif method_name == "turn_off":
			if value == animation.track_get_key_time(hitbox_method_track, key):
				hitbox_turn_on_frame_field.set_value_no_signal(value + 1)
	animation.track_set_key_time(hitbox_method_track, turn_on_key, hitbox_turn_on_frame_field.value)
	animation_player.seek(hitbox_turn_on_frame_field.value)
	animation_player.advance(0)
	animation_player.pause()
	sync_visibility_track(hitbox_method_track, hitbox_visibility_track)

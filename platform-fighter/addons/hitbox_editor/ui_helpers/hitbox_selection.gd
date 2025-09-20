@tool
extends VBoxContainer


@export var hitbox_script_path:String
@export var default_hitbox_radius = 20

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

var hitbox:Hitbox
var hitbox_path
var base_character
var animation_player: AnimationPlayer
var state_node: CharacterState
var animation: Animation
func update_hitbox(_hitbox, _base_character, _state_node):
	hitbox = _hitbox
	base_character = _base_character
	animation_player = base_character.animation_player
	state_node= _state_node
	hitbox_path = animation_player.owner.get_path_to(hitbox)
	var animation_library = animation_player.get_animation_library("")
	animation= animation_library.get_animation(state_node.name)
	
	var pos_track
	var func_track
	var hitbox_position_track
	
	
	for track in range(animation.get_track_count()):
		var track_type = animation.track_get_type(track)
		var path = animation.track_get_path(track)
		if track_type == Animation.TrackType.TYPE_VALUE:
			var interpolated: bool = animation.track_get_interpolation_type(track)
			print(path)
			if path.get_concatenated_names() != hitbox_path.get_concatenated_names() \
			and path.get_concatenated_subnames() != "position": continue
			for key in animation.track_get_key_count(track):
				var time = animation.track_get_key_time(track, key)
				var position = animation.track_get_key_value(track, key)
				print("change pos", time, position)
				# add keys to ui
		elif track_type == Animation.TrackType.TYPE_METHOD:
			
			if path.get_concatenated_names() != hitbox_path.get_concatenated_names(): continue
			for key in animation.track_get_key_count(track):
				if animation.method_track_get_name(track,key) == "turn_on":
					print("turn on", animation.track_get_key_time(track, key))
					# Add keys to ui
				elif animation.method_track_get_name(track,key) == "turn_off":
					print("turn off", animation.track_get_key_time(track, key))
		
	'''
	Update ui
	
	Extract anim info from animation player given current state
	and hitbox to be animated.
	
	maintain reference to make it easily editable.
	
	Need to be able to modify position track (and if it is interpolated)
	and toggle (method) track
	and 
	'''
	

func add_hitbox(state_node):
	var new_hitbox = Area2D.new()
	new_hitbox.set_script(load(hitbox_script_path))
	
	var collision = CollisionShape2D.new()
	collision.shape = CircleShape2D.new()
	collision.shape.radius = default_hitbox_radius
	
	state_node.add_child(collision)
	
	return new_hitbox

func remove_hitbox(state_node:Node):
	state_node.remove_child(hitbox)
	hitbox.queue_free()

func add_position_anim_key(frame, position):
	var track = get_position_anim_track()
	#TODO Change this to properly extract framerate from anim
	var frame_rate = 1
	var time = frame_rate * frame
	if !track:
		track = animation.add_track(Animation.TYPE_ANIMATION)
		animation.value_track_set_update_mode(track, Animation.UpdateMode.UPDATE_DISCRETE)
		animation.track_set_interpolation_type(track, Animation.InterpolationType.INTERPOLATION_NEAREST)
	animation.track_insert_key(track,time,position)
	pass

'''  
this asumes keys auto sort in editor so the
key is just the position in the anim array
'''
func remove_position_key(key):
	var track = get_position_anim_track()
	animation.track_remove_key(track, key)

func add_method_anim_key(state_node: Node, anim:Animation, method: String, frame: int):
	pass

func add_hitbox_frame(state_node: Node, anim:Animation, hitbox: Area2D, frame: int):
	pass

func get_position_anim_track():
	for track in animation.get_track_count():
		var path = animation.track_get_path(track)
		if animation.track_get_interpolation_type(track) == Animation.TrackType.TYPE_ANIMATION\
		and path.get_concatenated_names() != hitbox_path.get_concatenated_names():
			return track
	return null

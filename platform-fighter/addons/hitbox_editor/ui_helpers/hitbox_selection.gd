@tool
extends VBoxContainer


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
var base_character

func update_hitbox(_hitbox, _base_character, state):
	hitbox = _hitbox
	base_character = _base_character
	var animation_player: AnimationPlayer = base_character.animation_player
	var animation_library = animation_player.get_animation_library("")
	
	var animation: Animation = animation_library.get_animation(state.name)
	
	var pos_track
	var func_track
	var hitbox_position_track
	var hitbox_path = animation_player.owner.get_path_to(hitbox)
	for track in range(animation.get_track_count()):
		var track_type = animation.track_get_type(track)
		if track_type == Animation.TrackType.TYPE_VALUE:
			var interpolated: bool = animation.track_get_interpolation_type(track)
			var path = animation.track_get_path(track)
			print(path)
			if path.get_concatenated_names() != hitbox_path.get_concatenated_names() \
			and path.get_concatenated_subnames() != "position": continue
			for key in animation.track_get_key_count(track):
				var time = animation.track_get_key_time(track, key)
				var position = animation.track_get_key_value(track, key)
				print("change pos", time, position)
				# add keys to ui
		elif track_type == Animation.TrackType.TYPE_METHOD:
			var path = animation.track_get_path(track)
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
	

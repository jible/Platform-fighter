extends VBoxContainer


@export var interpolate: CheckBox
@export var offset_info: VBoxContainer
@export var turn_on_frames: Label
@export var damage_value: SpinBox
@export var damage_slider: Slider
@export var knockback_x: SpinBox
@export var knockback_y: SpinBox

var hitbox:Hitbox
var base_character

func update_hitbox(_hitbox, _base_character, state_name):
	hitbox = _hitbox
	base_character = _base_character
	var animation_player = base_character.animation_player
	var animation_library = animation_player.get_animation_library("")
	var animation = animation_library.get_animation(state_name)
	
	var pos_track
	var func_track
	for track in animation.get_track_count():
		var track_type = animation.track_get_type(track)
		if track_type == Animation.TrackType.TYPE_VALUE:
			var path = animation.track_get_path(track)
	'''
	Update ui
	
	Extract anim info from animation player given current state
	and hitbox to be animated.
	
	maintain reference to make it easily editable.
	
	Need to be able to modify position track (and if it is interpolated)
	and toggle (method) track
	and 
	'''
func get_node_path_from_root(node):
	'''
	Walk up the tree until you hit the root
	'''
	var path = ""
	var root = node.owner
	var walk_node = node
	while node!= root:
		walk_node = walk_node.get_parent()
		path =  walk_node.name+ path
		if node!= root:
			path = "/" + path

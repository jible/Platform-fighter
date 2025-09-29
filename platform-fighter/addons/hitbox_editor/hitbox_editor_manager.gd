@tool
extends VBoxContainer

var hitbox_scene = preload("uid://dosttxqhf11ww")
@export var default_hitbox_radius:float = 20

@export var frame_slider: HSlider

@export var character_name_label: Label
@export var state_drop_down: OptionButton
@export var hitbox_drop_down: OptionButton
@export var hitbox_selection: VBoxContainer

var states = []
var state :CharacterState
var character_root
var state_machine
var animation_player: AnimationPlayer
var current_state_hitboxes = []


func _process(delta):
	if Engine.is_editor_hint() and character_root != null and animation_player.current_animation != "":
		frame_slider.set_block_signals(true)
		frame_slider.value = animation_player.current_animation_position
		frame_slider.set_block_signals(false)


func configure(_character_root: BaseCharacter):
	character_root = _character_root
	character_name_label.text = character_root.name
	animation_player = character_root.animation_player
	state_machine = character_root.state_machine
	states = character_root.state_machine.get_children(true)
	state_drop_down.clear()
	if states.size() <= 0:return
	for state in states:
		state_drop_down.add_item(state.name)
	_on_state_drop_down_item_selected(state_drop_down.selected)

# 
func update_hitboxes(hitboxes):
	hitbox_drop_down.clear()
	current_state_hitboxes = hitboxes
	for hitbox in current_state_hitboxes:
		hitbox_drop_down.add_item(hitbox.name)
	if current_state_hitboxes.size() > 0: # dont select if there is no hitbox
		_on_hitbox_drop_down_item_selected(hitbox_drop_down.selected)
		hitbox_selection.show()
	else:
		hitbox_selection.hide()



func add_hitbox():
	#TODO change to instance hitbox instead of area 2d and attatching script
	var new_hitbox: Hitbox = hitbox_scene.instantiate()
	new_hitbox.name = "hitbox"
	
	
	var collision = CollisionShape2D.new()
	new_hitbox.collision_shape = collision
	
	var shape = CircleShape2D.new()
	collision.shape = shape
	collision.debug_color = Color("2ea06c79")
	new_hitbox.add_child(collision)
	# Only add it to scene once everything else is ready (so obj has refrences for _ready())
	new_hitbox.base_character = character_root
	
	
	state.add_child(new_hitbox)
	
	new_hitbox.owner = get_tree().edited_scene_root
	collision.owner = get_tree().edited_scene_root
	
	
	
	hitbox_selection.update_hitbox(new_hitbox, character_root, state)
	
	# Add new hitbox to drop down and select it
	hitbox_drop_down.add_item(new_hitbox.name)
	hitbox_drop_down.select(hitbox_drop_down.item_count - 1 )
	
	hitbox_selection.show()
	
	
	return new_hitbox

func remove_hitbox():
	var selected = hitbox_drop_down.selected
	if selected == -1: return
	var hitbox_name = hitbox_drop_down.get_item_text(selected)
	print(hitbox_name)
	var hitbox = state.find_child(hitbox_name)
	print(state, hitbox)
	if !hitbox: 
		print("couldnt find hitbox")
		return
	
	hitbox_drop_down.remove_item(selected)
	if hitbox_drop_down.item_count <= 0:
		hitbox_selection.hide()
	
	
	hitbox_selection.remove_hitbox_anims()
	
	state.remove_child(hitbox)
	hitbox.queue_free()

# UI Reactions--------------------------------------------------------------------------------------
func _on_state_drop_down_item_selected(index):
	var current_state_name = state_drop_down.get_item_text(index)
	state = state_machine.find_child(current_state_name)
	if !state:
		print("state does not exist")
		return
	frame_slider.max_value = animation_player.get_animation(state.name).length
	animation_player.current_animation = state.name
	animation_player.seek(0,true)
	animation_player.pause()
	update_hitboxes(state.get_children())


func _on_hitbox_drop_down_item_selected(index):
	var new_hitbox = state.find_child(hitbox_drop_down.get_item_text(index))
	if new_hitbox:
		hitbox_selection.update_hitbox(new_hitbox, character_root, state)
	else:
		push_error("Could not find hitbox")

func _on_frame_slider_value_changed(value):
	animation_player.stop()
	animation_player.seek(value, true)
	

func _on_play_pressed():
	animation_player.play()

func _on_frame_slider_drag_started():
	animation_player.pause()


func _on_pause_pressed():
	animation_player.pause()


func _on_add_hitbox_pressed():
	add_hitbox()

func _on_remove_hitbox_pressed():
	remove_hitbox()

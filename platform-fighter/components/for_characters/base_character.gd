class_name BaseCharacter
extends Node3D

@export_range(0,3, 1) var team_number = 0
@export_range(0, 3, 1) var player_number = 0
var player_tag: PlayerTag

var configured: bool = false
var started: bool = false

var connected_to_scene = false

@export var play_scene_manager: PlaySceneManager3D

@export var base_character:BaseCharacter
@export var character_body: dp_player_body
@export var health: Health
@export var model_holder: Node
@export var animation_player: AnimationPlayer
@export var input_handler: InputHandler


func _ready():
	get_all_children()
	for decendant in all_decendants:
		decendant.process_mode = Node.PROCESS_MODE_DISABLED
		decendant.set_physics_process(false)

var all_decendants : Array[Node]

func configure_player(_team_number, _player_number, _player_tag):
	team_number = _team_number
	player_number = _player_number
	player_tag =_player_tag
	var player_holder = get_parent()
	play_scene_manager = player_holder.play_scene_manager
	character_body.mask_collision = GlobalResources.PhysicsLayers.ENVIRONMENT | GlobalResources.PhysicsLayers.PLATFORM
	
	for child in all_decendants:
		if child.has_method("configure"): child.configure()
	configured = true

func start_character():
	for child in all_decendants:
		child.process_mode = Node.PROCESS_MODE_ALWAYS
		child.set_physics_process(true)
	started = true

func tick_character():
	for child in all_decendants:
		if child.has_method("process_tick"):
			child.process_tick()

func get_all_children(parent = self):
	if parent == null: return
	if parent == self:
		all_decendants = []
	else:
		all_decendants.append(parent)
	for child in parent.get_children():
		get_all_children(child)
	

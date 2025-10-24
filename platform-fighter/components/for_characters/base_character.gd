class_name BaseCharacter
extends Node2D

@export_range(0,3, 1) var team_number = 0
@export_range(0, 3, 1) var player_number = 0

var configured: bool = false
var started: bool = false

var connected_to_scene = false

@export var play_scene_manager: PlaySceneManager

@export var base_character:BaseCharacter
@export var character_body: SpecializedCharacterBody
@export var state_machine: CharacterStateMachine
@export var behavior_manager: CharacterBehaviorManager
@export var health: Health
@export var sprite_manager: SpriteManager
@export var animation_player: AnimationPlayer
@export var input_handler: InputHandler
@export var direction_changer: DirectionChanger



func _ready():
	get_all_children()
	for decendant in all_decendants:
		decendant.process_mode = Node.PROCESS_MODE_DISABLED
		decendant.set_physics_process(false)
		
	
var all_decendants : Array[Node]
func configure_player(_team_number, _player_number):
	team_number = _team_number
	player_number = _player_number
	var player_holder = get_parent()
	play_scene_manager = player_holder.play_scene_manager
	for child in all_decendants:
		if child.has_method("configure"): child.configure()
	configured = true

func start_character():
	for child in all_decendants:
		child.process_mode = Node.PROCESS_MODE_ALWAYS
		child.set_physics_process(true)
	started = true

func get_all_children(parent = self):
	if parent == null: return
	if parent == self:
		all_decendants = []
	else:
		all_decendants.append(parent)
	for child in parent.get_children():
		get_all_children(child)
	

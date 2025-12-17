extends Node
class_name TickManager



@export var max_rollback_ticks: int = 50


var current_tick : int = 0
var game_states = []
var sample_state: Dictionary = {
	"inputs": [],
	"stage_states": [],
	"character_states": [],
}

@export var play_scene_manager: PlaySceneManager3D
@export var input_manager: InputManager
@export var character_holder: CharacterHolder
var continue_play: bool = true

func _ready():
	# Fill the states buffer with empty states
	for i in max_rollback_ticks:
		game_states.append(sample_state.duplicate(true))
		
func get_state_key(tick) ->int:
	return tick % max_rollback_ticks

# Starts the game
func start():
	pass

func _physics_process(_delta):
	if !continue_play:
		return
	
	# Serialize World State
	serialize_tick()
	
	var current_state_key = get_state_key(current_tick)
	# Some calls require the previous state's info too
	var prev_state_key = get_state_key(current_tick - 1)
	
	game_states[current_state_key].inputs = input_manager.serialize_current_controller_state()
	play_scene_manager.tick(game_states[prev_state_key].inputs, game_states[current_state_key].inputs)
	current_tick = current_tick + 1

	
	
func serialize_tick():
	var current_state_key = get_state_key(current_tick)
	# Collect Inputs
	game_states[current_state_key].inputs = input_manager.serialize_current_controller_state()
	
	
	#game_states[state_key].stage_states =
	#game_states[state_key].character_states =
	pass

func load_tick():
	pass


# Public function for managers to use
static func propogate_tick(node: Node):
	if (!node): return
	# Currently supporting both capitalized and not for gdscripts and c# scripts
	if node.has_method("tick"):
		node.tick()
	elif node.has_method("Tick"):
		node.Tick()
	for child in node.get_children():
		propogate_tick(child)

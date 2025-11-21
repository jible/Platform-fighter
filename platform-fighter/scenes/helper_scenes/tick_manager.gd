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

@export var input_manager: InputManager

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
	simulate_tick()

func serialize_tick():
	#game_states[state_key].stage_states =
	#game_states[state_key].character_states =
	pass

func load_tick():
	pass

func simulate_tick():
	var state_key = get_state_key(current_tick)
	
	# Serialize World State
	serialize_tick()
	
	# Collect Inputs
	game_states[state_key].inputs = input_manager.serialize_current_controller_state()
	
	# Emit input
	input_manager.dispatch_controller_states_for_tick
	
	
	# Process Physics
	
	# Physics signals emit
	
	# Not sure if i progress tick at start or end of a frame...
	current_tick = current_tick + 1

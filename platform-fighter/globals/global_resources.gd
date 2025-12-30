extends Node


var max_team_count = 4
var ticks_per_sec: int
var tick_length: float
var team_one_collision_bit: int = 5

# change this to suit rollback later
func get_current_match_frame():
	return Engine.get_physics_frames()

enum ConnectionMode {
	LOCAL,
	ONLINE
}
@export var connection_mode: ConnectionMode = ConnectionMode.LOCAL

var default_input_action_events = {
	ControllerState.Button_Types.LIGHT : [ KEY_E, JOY_BUTTON_A ],
	ControllerState.Button_Types.SPECIAL : [ KEY_SHIFT, JOY_BUTTON_B ],
	ControllerState.Button_Types.JUMP : [ KEY_SPACE, JOY_BUTTON_X ],
	ControllerState.Button_Types.GRAB : [ KEY_Q, JOY_BUTTON_RIGHT_SHOULDER ],
	ControllerState.Button_Types.LEFT_UP : [KEY_W],
	ControllerState.Button_Types.LEFT_DOWN : [KEY_S],
	ControllerState.Button_Types.LEFT_LEFT : [KEY_A],
	ControllerState.Button_Types.LEFT_RIGHT : [KEY_D],
}

enum PhysicsLayers {
	ENVIRONMENT,
	PLATFORM,
	PLAYER_1_COLLISION,
	PLAYER_2_COLLISION,
	PLAYER_3_COLLISION,
	PLAYER_4_COLLISION,
	PLAYER_1_HITBOX,
	PLAYER_2_HITBOX,
	PLAYER_3_HITBOX,
	PLAYER_4_HITBOX,
	PLAYER_1_HURTBOX,
	PLAYER_2_HURTBOX,
	PLAYER_3_HURTBOX,
	PLAYER_4_HURTBOX,
}

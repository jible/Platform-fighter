extends Node


var max_team_count = 4
var ticks_per_sec: int
var tick_length: float
var team_one_collision_bit: int = 5

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

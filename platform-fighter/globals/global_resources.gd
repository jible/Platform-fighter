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
	"light" : [ KEY_E, JOY_BUTTON_A ],
	"special" : [ KEY_SHIFT, JOY_BUTTON_B ],
	"jump" : [ KEY_SPACE, JOY_BUTTON_X ],
	"grab" : [ KEY_Q, JOY_BUTTON_RIGHT_SHOULDER ],
	"left_up" : [KEY_W],
	"left_down" : [KEY_S],
	"left_left" : [KEY_A],
	"left_right" : [KEY_D],
}

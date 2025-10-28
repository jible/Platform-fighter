extends Node


var max_team_count = 4
var ticks_per_sec: int
var tick_length: float


enum ConnectionMode {
	LOCAL,
	ONLINE
}
@export var connection_mode: ConnectionMode = ConnectionMode.LOCAL

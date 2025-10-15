extends Node


var max_team_count = 4
var ticks_per_sec: int
var tick_length: float
func _ready():
	ticks_per_sec = ProjectSettings.get_setting("physics/common/physics_ticks_per_second")
	tick_length = 1/ticks_per_sec

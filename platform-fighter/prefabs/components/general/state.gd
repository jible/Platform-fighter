class_name State
extends Node

var is_active:bool = false
var state_machine
var character = null

func _ready():
	state_machine = get_parent()
	character = state_machine.get_parent()
func enter_state():
	pass
	
func update_state(delta:float):
	pass
	
func exit_state():
	pass

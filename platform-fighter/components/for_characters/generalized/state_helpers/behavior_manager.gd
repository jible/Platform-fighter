class_name CharacterBehaviorManager
extends Node

var all_behaviors: Array[CharacterBehavior]
var active_behaviors: Array[CharacterBehavior]
@onready var base_character: BasePlayer = owner as BasePlayer
@onready var state_machine: CharacterStateMachine = base_character.state_machine
@onready var character_body: CharacterBody2D = base_character.character_body


func _ready():
	for child in get_children():
		all_behaviors.append(child)
	get_active_behaviors()

func get_active_behaviors():
	active_behaviors.clear()
	for behavior in all_behaviors:
		if behavior.priority >= character_body.lock_level and state_machine.current_state_name in behavior.valid_states:
			behavior.is_active = true
			active_behaviors.append(behavior)
		else:
			behavior.is_active = false


func _physics_process(_delta):
	for behavior in active_behaviors:
		if behavior.condition():
			behavior.trigger()
			return


func _on_state_machine_state_changed(_node):
	get_active_behaviors()

func _on_base_player_lock_level_changed(_lock_level):
	get_active_behaviors()

class_name PlayerBehaviorManager
extends Node

var all_behaviors: Array[PlayerBehavior]
var active_behaviors: Array[PlayerBehavior]
var character: CharacterBody2D
@export var state_machine: CharacterStateMachine

# TODO 
# Make signal for changing state lock-priority
# Connect that signal and state change signal to this and call get active behaviors on those signals
# Potentially remove tags now that the behavior system is in place. (Just list states (or their names) that apply for behaviors instead of tags)
# Decide how much you put into a base character class if you make one
# Remove condition func from character states and state base class 

func _ready():
	for child in get_children():
		all_behaviors.append(child)


func get_active_behaviors():
	active_behaviors.clear()
	for behavior in all_behaviors:
		if behavior.priority >= character.lock_level and state_machine.current_state_node.tag in behavior.valid_tags:
			active_behaviors.append(behavior)


func _physics_process(_delta):
	for behavior in active_behaviors:
		if behavior.condition():
			behavior.trigger()
			return

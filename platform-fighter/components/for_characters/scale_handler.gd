@tool
class_name ScaleHandler
extends Node

'''
This script simply sets the state machine's scale to the inverse of the base character.
This way, the hitboxes follow the sprite manager without inheriting the increased scale
'''

@export var base_character:BaseCharacter
@export var state_machine:CharacterStateMachine

func _process(delta):
	if Engine.is_editor_hint():
		state_machine.scale = Vector2(1/base_character.scale.x, 1/base_character.scale.y)

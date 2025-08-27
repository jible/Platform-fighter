@tool
extends Node

# This script automatically serves references to all of the nodes on the character 
# if they have empty variables that correspond to the names of these variables
@export_tool_button("Serve References") var button = serve_references


@export var base_character:BaseCharacter
@export var character_body:CharacterBody2D
@export var mobility_manager:MobilityManager
@export var state_machine: CharacterStateMachine
@export var behavior_manager: CharacterBehaviorManager
@export var health: Health
@export var sprite_manager: SpriteManager
@export var animation_player: AnimationPlayer

func serve_references():
	var root = base_character
	var references = {}
	var own_property_list = get_property_list()
	for property in own_property_list:
		if property.useage and PROPERTY_USAGE_EDITOR:
			print(property)
	return
	

func propogate_references(parent):
	var children = parent.get_children()
	for child in children:
		#for 
		pass

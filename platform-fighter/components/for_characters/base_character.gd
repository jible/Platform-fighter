class_name BaseCharacter
extends Node2D

@export_range(0,3, 1) var team_number = 0
@export_range(0, 3, 1) var player_number = 0

@export var play_scene_manager: PlaySceneManager

@export var base_character:BaseCharacter
@export var character_body: SpecializedCharacterBody
@export var state_machine: CharacterStateMachine
@export var behavior_manager: CharacterBehaviorManager
@export var health: Health
@export var sprite_manager: SpriteManager
@export var animation_player: AnimationPlayer
@export var input_handler: InputHandler
""" 
In godot, child script's ready call happen before parent ready calls.
This siganl is supposed to go off after the root node has received its dependents

Currently, the only thing things that are decided in the play scene are the player's 
team number and player number.

Examples of uses are:
	- The hitboxes and hurtboxes need to know what team 
	they are on to set their bitmask and layer
	- Input Handler needs to know what player's inputs to listen for
	
Note this is currently not configured since players are not instanced yet.
"""
signal configured

func configure(_team_number, _player_number):
	team_number = _team_number
	player_number = _player_number
	configured.emit()

	

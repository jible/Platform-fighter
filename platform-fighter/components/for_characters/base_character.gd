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

signal configured

func configure(_team_number, _player_number):
	team_number = _team_number
	player_number = _player_number
	configured.emit()

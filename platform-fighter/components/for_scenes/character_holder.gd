class_name CharacterHolder
extends Node2D



@export var play_scene_manager: PlaySceneManager
@export var characters_to_instance: Array[CharacterSpawnPackage]
@export var starting_positions = [ Vector2(-10, -20), Vector2(300, -20)]
var players: Array[BaseCharacter] = []

# Make sure all aspects are loaded without making the players act
func instance_players():
	for character_spawn_package in characters_to_instance:
		var character_profile = character_spawn_package.character_profile
		var scene = load(character_profile.scene_path)
		var instance: BaseCharacter = scene.instantiate()
		add_child(instance)
		players.append(instance)
		instance.name = "Player" + str(character_spawn_package.player_number)
		instance.configure_player(character_spawn_package.team_number, character_spawn_package.player_number)

# Allow players to animate and receive inputs
func start_players():
	for child in get_children():
		child.start_character()

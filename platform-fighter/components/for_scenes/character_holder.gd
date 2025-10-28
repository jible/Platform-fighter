class_name CharacterHolder
extends Node2D



@export var play_scene_manager: PlaySceneManager
@export var starting_positions = [ Vector2(-10, -20), Vector2(300, -20)]
var players: Array[BaseCharacter] = []

# Make sure all aspects are loaded without making the players act
func instance_players():
	for player in PlayerManager.all_players:
		if !player: continue
		var character_profile = player.selected_character
		var scene = load(character_profile.scene_path)
		var instance: BaseCharacter = scene.instantiate()
		add_child(instance)
		players.append(instance)
		instance.name = "Player" + str(player.player_number)
		instance.configure_player(player.team_number, player.player_number)

# Allow players to animate and receive inputs
func start_players():
	for child in get_children():
		child.start_character()

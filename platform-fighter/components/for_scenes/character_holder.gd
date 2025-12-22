class_name CharacterHolder
extends Node



@export var play_scene_manager: PlaySceneManager3D
var starting_positions = [ Vector2(-10, -20), Vector2(300, -20)]
var players: Array[BaseCharacter] = []

func config():
	instance_players()

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
		instance.configure_player(player.team_number, player.player_number, player.player_tag)
		
# Allow players to animate and receive inputs
func start_players():
	for child in get_children():
		child.start_character()

func tick():
	for character in get_children():
		TickManager.propogate_tick(character)

class_name CharacterHolder
extends Node2D


@export var play_scene_manager: PlaySceneManager

'''
In the future, the character holder will be responsible for parsing the 
info sent from the character select scene and spawning the correct player
with the correct team number.
'''

func _ready():
	var team_num = 0
	var player_num = 0
	var children = get_children()
	for child in children:
		if !child is BaseCharacter: continue
		child.configure(team_num, player_num)
		team_num += 1
		player_num += 1

class_name BaseCharacter
extends Node2D


"""
THIS SCRIPT IS THE MAIN OWNER OF ALL NODES IN THE CHARACTER
ALL NODE REFERENCES SHOULD BE CENTRALIZED THROUGH THIS NODE


All child nodes should be able to get reference to this base player by calling
var player = owner as BasePlayer
"""


@export_range(0,3, 1) var team_number = 0
@export_range(0,3,1) var player_number = 0

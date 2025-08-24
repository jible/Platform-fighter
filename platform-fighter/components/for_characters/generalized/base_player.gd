class_name BasePlayer
extends Node2D


"""
THIS SCRIPT IS THE MAIN OWNER OF ALL NODES IN THE CHARACTER
ALL NODE REFERENCES SHOULD BE CENTRALIZED THROUGH THIS NODE


All child nodes should be able to get reference to this base player by calling
var player = owner as BasePlayer
"""


@export var character_body:CharacterBody2D
@export var mobility_manager:MobilityManager
@export var state_machine: CharacterStateMachine
@export var behavior_manager: CharacterBehaviorManager
@export var health: Health
@export var sprite_manager: SpriteManager
@export var animation_player: AnimationPlayer

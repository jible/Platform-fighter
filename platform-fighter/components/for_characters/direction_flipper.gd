class_name DirectionChanger
extends Node2D

@export var base_character: BaseCharacter

@export var auto_flip_sprite: bool = true
@export var auto_flip_hitboxes: bool = true

var hitboxes: Array[Hitbox] = []
var hurtboxes: Array[Hurtbox] = []

'''
This script handles mirroring hitboxes and sprites
'''
func get_hurtboxes():
	base_character

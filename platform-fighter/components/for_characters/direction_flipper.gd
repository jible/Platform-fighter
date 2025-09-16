class_name DirectionChanger
extends Node2D

@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine
@export var character_body: SpecializedCharacterBody
@export var sprite_manager: SpriteManager

@export var auto_flip_sprite: bool = true
@export var auto_flip_hitboxes: bool = true
@export var auto_flip_hurtboxes: bool = true

var hitboxes: Array[Hitbox] = []
var hurtboxes: Array[Hurtbox] = []
var direction = 1

'''
This script handles mirroring hitboxes and sprites
'''


func _process(_delta):
	var current_dir = sign(character_body.velocity.x) 
	if direction != current_dir and current_dir != 0 and state_machine.current_state_node.can_turn_around:
		if auto_flip_sprite:
			sprite_manager.flip_h = !sprite_manager.flip_h
		if auto_flip_hitboxes:
			state_machine.scale.x = -1 * state_machine.scale.x
		direction = current_dir

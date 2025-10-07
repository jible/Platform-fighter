class_name DirectionChanger
extends Node2D

@export var base_character: BaseCharacter
@export var state_machine: CharacterStateMachine
@export var character_body: SpecializedCharacterBody
@export var sprite_manager: SpriteManager
@export var input_handler: InputHandler

@export var auto_flip_sprite: bool = true
@export var auto_flip_hitboxes: bool = true
@export var auto_flip_hurtboxes: bool = true

var hitboxes: Array = []
var hurtboxes: Array[Hurtbox] = []
var direction = 1

'''
This script handles mirroring hitboxes and sprites
'''
func _ready():
	hitboxes = state_machine.find_children("", "Hitbox")

func _process(_delta):
	var current_dir = sign(character_body.velocity.x) 
	if state_machine.current_state_node.can_turn_around:
		flip(current_dir)

# There are cases where rather than using velocity, you just want to check 
# for player input to decide player direction
func try_input_turn_around():
	var input_dir = input_handler.get_left_stick().x 
	flip(input_dir)

func flip(dir):
	if dir == direction or dir == 0: return
	
	if auto_flip_sprite:
		sprite_manager.flip_h = dir == -1
	if auto_flip_hitboxes:
		for hitbox in hitboxes:
			hitbox.position.x *= -1
	direction = dir


func _on_state_machine_state_changed(new_state_node):
	if !base_character.connected_to_scene: return
	if new_state_node.can_turn_around_before:try_input_turn_around()

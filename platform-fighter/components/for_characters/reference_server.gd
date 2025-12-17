@tool
extends Node

'''
This script gives all nodes with the "reference_server" property a reference to this node
 When the Serve References button is pressed
 This node stores all manager/major node references that a character will only have one of
 So, if a node needs a reference to the character body, they will type:
 var character_body = server_refence.character_body
'''
@export_tool_button("Serve References") var button = serve_references

@export var base_character:BaseCharacter
@export var character_body: SpecializedCharacterBody
@export var state_machine: CharacterStateMachine
@export var behavior_manager: CharacterBehaviorManager
@export var health: Health
@export var sprite_manager: SpriteManager
@export var animation_player: AnimationPlayer
@export var input_handler: InputHandler
@export var direction_changer: DirectionChanger


var type_to_group = {
	Hitbox : "Hitbox"
	
}

'''
Note: After adding new nodes, you have to reload the scene in order for those nodes 
to be reached by this script.
'''
func serve_references():
	propogate_references(base_character)
	return

func propogate_references(parent: Node):
	if parent == null:
		return
	
	
	
	for prop in get_property_list():
		if not (prop.usage & PROPERTY_USAGE_EDITOR):
			continue
			
		var reference_name = prop.name
		if reference_name == "script":
			continue
		if reference_name in parent and not parent[reference_name] and self[reference_name]:
			parent[reference_name] = self[reference_name]
	
	var children = parent.get_children(true)
	for child in children:
		propogate_references(child)

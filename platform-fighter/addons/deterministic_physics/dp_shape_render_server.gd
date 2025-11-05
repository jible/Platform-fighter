@tool
class_name DP_ShapeRenderServer
extends Node
'''
This script itterates through each collision shape in the scene and draws it to its correct position
'''
@export var shape_renderer: Node2D
@export var show_shapes: bool = true

var all_shapes: Array[DP_Collision_Shape] = []
@export var search_root: Node
func _ready():
	get_tree().node_added.connect(_on_node_added)
	get_tree().node_removed.connect(_on_node_removed)
	all_shapes = []
	get_all_shapes(search_root)
	
func get_all_shapes(parent):
	if !parent: return
	if parent is DP_Collision_Shape:
		all_shapes.append(parent)
	for child in parent.get_children():
		get_all_shapes(child)

func _process(_delta):
	if show_shapes and shape_renderer: shape_renderer.queue_redraw()

func _on_node_added(node:Node):
	if node is DP_Collision_Shape:
		all_shapes.append(node)

func _on_node_removed(node:Node):
	if node is DP_Collision_Shape:
		all_shapes.erase(node)

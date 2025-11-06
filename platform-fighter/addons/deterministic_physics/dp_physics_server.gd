@tool
class_name DP_PhysicsServer
extends Node

'''
This should be a singleton (global script)
'''
var all_shapes: Array[DP_Collision_Shape] = []
@export var search_root: Node

func _ready():
	get_tree().node_added.connect(_on_node_added)
	get_tree().node_removed.connect(_on_node_removed)
	all_shapes = []
	get_all_shapes(search_root)
	
	
''' Call this function every time a frame passes!
It handles all deterministic physics interactions between DP shapes
'''
func handle_collisions():
	for a in all_shapes:
		for b in all_shapes:
			if a == b:continue
			if !(a.collision_mask & b.collision_layer): continue
			# If the other object is a trigger, it will receive the trigger
			if (b.is_trigger): continue
			var currently_overlaps = a.check_overlap(b)
			if a.is_trigger and currently_overlaps:
				a.overlaps[GlobalResources.get_current_match_frame()][b] = true
			var previously_overlaped = a.overlaps[GlobalResources.get_current_match_frame()].has(b)
			if currently_overlaps and !previously_overlaped:
				pass
				# Emit enter
			elif !currently_overlaps and previously_overlaped:
				pass
				# emit exited


# All shaper holder helpers
func get_all_shapes(parent):
	if !parent: return
	if parent is DP_Collision_Shape:
		all_shapes.append(parent)
	for child in parent.get_children():
		get_all_shapes(child)

func _on_node_added(node:Node):
	if node is DP_Collision_Shape:
		all_shapes.append(node)

func _on_node_removed(node:Node):
	if node is DP_Collision_Shape:
		all_shapes.erase(node)

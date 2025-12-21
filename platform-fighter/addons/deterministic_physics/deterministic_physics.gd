@tool
extends EditorPlugin

var physics_server: dp_physics_server
var physics_renderer: dp_shape_renderer_3d


var scene_root: Node
func _enable_plugin():
	pass


func _disable_plugin():
	# Remove autoloads here.
	pass


func _enter_tree():
	scene_root = get_tree().edited_scene_root if Engine.is_editor_hint() else get_tree().current_scene
	_on_scene_changed(scene_root)
	scene_changed.connect(_on_scene_changed)

func _exit_tree():
	pass
	#root.remove_child(physics_server)
	#physics_server.queue_free()
	#root.remove_child(physics_renderer)
	#physics_renderer.queue_free()

func _on_scene_changed(root: Node):
	#clean_up_managers()
	if (! root): return
	physics_server = dp_physics_server.new()
	physics_renderer= dp_shape_renderer_3d.new()
	
	root.add_child(physics_server)
	root.add_child(physics_renderer)
	
func clean_up_managers():
	if (physics_renderer and physics_renderer.is_valid_instance()):
		var parent = physics_renderer.get_parent()
		if (parent):
			parent.remove_child(physics_renderer)
			physics_renderer.queue_free()
			physics_renderer = null
	
	if (physics_server and physics_server.is_valid_instance()):
		var parent = physics_server.get_parent()
		if (parent):
			parent.remove_child(physics_server)
			physics_server.queue_free()
			physics_server = null

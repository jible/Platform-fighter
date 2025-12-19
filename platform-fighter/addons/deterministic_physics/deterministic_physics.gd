@tool
extends EditorPlugin

var physics_server: dp_physics_server
var physics_renderer: dp_shape_renderer_3d

func _enable_plugin():
	pass


func _disable_plugin():
	# Remove autoloads here.
	pass


func _enter_tree():
	physics_server = dp_physics_server.new()
	physics_renderer = dp_shape_renderer_3d.new()
	add_child(physics_server)
	add_child(physics_renderer)

func _exit_tree():
	remove_child(physics_server)
	physics_server.queue_free()
	remove_child(physics_renderer)
	physics_renderer.queue_free()
	

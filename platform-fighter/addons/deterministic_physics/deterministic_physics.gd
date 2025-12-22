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
	_on_scene_changed(scene_root)
	scene_changed.connect(_on_scene_changed)

func _exit_tree():
	remove_child(physics_server)
	physics_server.queue_free()
	remove_child(physics_renderer)
	physics_renderer.queue_free()


func _on_scene_changed(root: Node):
	if (! root): return
	DpPhysicsServer.configure(root)
	DpShapeRenderer3d.configure(root)

@tool
extends EditorPlugin

var hitbox_editor: Control
const editor_scene: String = "res://addons/hitbox_editor/hitbox_editor.tscn"
func _enter_tree():
	hitbox_editor = preload(editor_scene).instantiate()
	add_control_to_dock(DOCK_SLOT_LEFT_BL, hitbox_editor)


func _exit_tree():
	remove_control_from_docks(hitbox_editor)
	hitbox_editor.free()

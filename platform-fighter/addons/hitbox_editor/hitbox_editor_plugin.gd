@tool
extends EditorPlugin

var hitbox_editor: Control
const editor_scene: String = "res://addons/hitbox_editor/hitbox_editor.tscn"
func _enter_tree():
	hitbox_editor = preload(editor_scene).instantiate()
	add_control_to_bottom_panel(hitbox_editor, "Hitbox Editor")
	scene_changed.connect(configure_editor)
	configure_editor(get_tree().edited_scene_root)

func configure_editor(scene_root):
	if !scene_root or (! scene_root is BaseCharacter):
		return
	hitbox_editor.configure(scene_root)


func _exit_tree():
	remove_control_from_bottom_panel(hitbox_editor)
	hitbox_editor.free()

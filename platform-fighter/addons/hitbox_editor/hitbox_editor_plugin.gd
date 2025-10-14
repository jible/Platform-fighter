@tool
extends EditorPlugin

var hitbox_editor: Control
var in_bottom_panel: bool = false
const editor_scene: String = "res://addons/hitbox_editor/hitbox_editor_v2.tscn"
func _enter_tree():
	hitbox_editor = preload(editor_scene).instantiate()
	scene_changed.connect(configure_editor)
	configure_editor(get_tree().edited_scene_root)

func configure_editor(scene_root):
	
	if !scene_root or (! scene_root is BaseCharacter):
		if in_bottom_panel:
			remove_control_from_bottom_panel(hitbox_editor)
			in_bottom_panel = false
		return
	if !in_bottom_panel:
		add_control_to_bottom_panel(hitbox_editor, "Hitbox Editor")
		in_bottom_panel = true
	hitbox_editor.configure(scene_root)
	


func _exit_tree():
	remove_control_from_bottom_panel(hitbox_editor)
	in_bottom_panel = false
	hitbox_editor.free()

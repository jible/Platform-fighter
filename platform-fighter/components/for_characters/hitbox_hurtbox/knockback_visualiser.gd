@tool
class_name HitboxVisualiser
extends Line2D

var hitbox:Hitbox
var kb_vector = Vector2(0,0)

func _ready():
	if !Engine.is_editor_hint():
		hide()
		return
	show()
	var parent = get_parent()
	if !parent is Hitbox:
		push_error("Hitbox Visualiser on non-hitbox")
		return
	hitbox = parent
	reset_points()
	
func reset_points():
	clear_points()
	add_point(Vector2(0,0))
	add_point(Vector2(0,0))

func _process(_delta):
	if !Engine.is_editor_hint():return
	if get_point_count() != 2: reset_points()
	
	var new_vector = hitbox.knockback_vector
	if new_vector == kb_vector:return
	
	kb_vector = new_vector
	remove_point(1)
	add_point(kb_vector * hitbox.collision_shape.shape.radius)

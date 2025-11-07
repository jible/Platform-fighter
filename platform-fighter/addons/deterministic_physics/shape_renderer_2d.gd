@tool
class_name ShapeRenderer2D
extends Node2D

var standard_color = Color(0.0, 0.631, 0.0, 1.0)
@export var dp_physics_server: DP_PhysicsServer
@export var render_shapes_in_editor: bool = true
@export var render_shapes_in_play: bool = true

func _process(delta):
	if (Engine.is_editor_hint() and render_shapes_in_editor) or (!Engine.is_editor_hint() and render_shapes_in_play):
		queue_redraw()

func _draw():
	if !dp_physics_server: return
	
	for shape in dp_physics_server.all_shapes:
		if shape == null:
			continue
		if shape is DP_Sphere:
			draw_collision_sphere(shape)
		if shape is DP_Rectangle:
				draw_collision_rectangle(shape)
		if shape is DP_Capsule:
				draw_collision_capsule(shape)

func draw_collision_sphere(sphere: DP_Sphere):
	draw_circle(sphere.position.to_standard_vector(), sphere.radius.to_float(), standard_color)

func draw_collision_rectangle(rectangle: DP_Rectangle):
	var shape = Rect2(rectangle.position.to_standard_vector(),Vector2(rectangle.width.to_float(), rectangle.height.to_float()))
	draw_rect(shape,rectangle.debug_color)
	
func draw_collision_capsule(capsule: DP_Capsule):
	var pos = capsule.position.to_standard_vector()
	var radius = capsule.radius.to_float()
	var height = capsule.height.to_float()
	var half_body = (height /2.0) - radius
	
	var top_circle_center = pos + Vector2(0, - half_body)
	var bot_circle_center = pos + Vector2(0, + half_body)
	
	var rect_top_left = pos + Vector2(-radius, -half_body)
	var rect_size = Vector2(2 * radius, 2 * half_body)
	var rect_shape = Rect2(rect_top_left,rect_size)
	draw_rect(rect_shape, standard_color)
	
	draw_circle(top_circle_center,radius, standard_color)
	draw_circle(bot_circle_center,radius, standard_color)
	

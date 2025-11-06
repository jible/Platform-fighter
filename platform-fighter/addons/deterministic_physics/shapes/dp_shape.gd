extends Node
class_name  DP_Collision_Shape


@export var collision_layer: int
@export var collision_mask: int
@export var is_trigger: bool
@export var is_static: bool = false

var position: DM_Vector2 = DM_Vector2.from_ints(0,0)
var rotation: DM_Decimal = DM_Decimal.from_int(0)


@export var editor_pos: Vector2 = position.to_standard_vector():
	get():
		return position.to_standard_vector()
	set(value):
		position = DM_Vector2.from_floats_dangerous(value.x, value.y)

extends Node
class_name  DP_Collision_Shape


@export var collision_layer: int
@export var collision_mask: int
@export var is_trigger: bool
@export var is_static: bool = false


var position: DM_Vector2 = DM_Vector2.new()
var rotation: DM_Decimal = DM_Decimal.from_int(0)

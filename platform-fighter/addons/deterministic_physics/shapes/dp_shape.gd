class_name  DP_Shape

enum Shapes{
	SPHERE,
	CAPSULE,
	RECTANGLE,
	TRIANGLE,
}

var position: DM_Vector2
var rotation: DM_Decimal = DM_Decimal.from_int(0)
var is_static: bool = false

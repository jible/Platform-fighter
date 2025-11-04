class_name DM_Vector2

var x:DM_Decimal
var y:DM_Decimal


func _init():
	x = DM_Decimal.from_int(0)
	y = DM_Decimal.from_int(0)
	
static func from_ints(_x: DM_Decimal, _y: DM_Decimal):
	var v = DMV

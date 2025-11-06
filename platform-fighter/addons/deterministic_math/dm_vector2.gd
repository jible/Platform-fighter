extends RefCounted
class_name DM_Vector2

var x:DM_Decimal= DM_Decimal.from_int(0)
var y:DM_Decimal= DM_Decimal.from_int(0)


func _init():
	x = DM_Decimal.from_int(0)
	y = DM_Decimal.from_int(0)
	
static func from_ints(_x: int, _y: int):
	var v = DM_Vector2.new()
	v.x = DM_Decimal.from_int(_x)
	v.y = DM_Decimal.from_int(_y)
	return v

static func from_floats_dangerous(_x: float, _y: float):
	var v = DM_Vector2.new()
	v.x = DM_Decimal.from_floats_dangerous(_x)
	v.y = DM_Decimal.from_floats_dangerous(_y)
	return v

func to_standard_vector():
	var output = Vector2.ZERO
	output.x = x.to_float()
	output.y = y.to_float()
	return output

func add(other: DM_Vector2):
	var v = DM_Vector2.new()
	v.x = x.add(other.x)
	v.y = y.add(other.y)
	return v


func sub(other: DM_Vector2):
	var v = DM_Vector2.new()
	v.x = x.sub(other.x)
	v.y = y.sub(other.y)
	return v


func mult(coefficient: DM_Decimal):
	var v = DM_Vector2.new()
	v.x = x.mult(coefficient)
	v.y = y.mult(coefficient)
	return v

func div(divisor: DM_Decimal):
	var v = DM_Vector2.new()
	v.x = x.div(divisor)
	v.y = y.div(divisor)
	return v

func get_magnitude():
	var magnitude = ( (x.power(2)).add( y.power(2)) ).square_root()
	return magnitude

func normalize():
	var magnitude = get_magnitude()
	if magnitude == 0: return DM_Vector2.new()
	return div(magnitude)

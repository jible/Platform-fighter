class_name DM_Decimal

''' 
This is a custom number data type which is the base of the deterministic math system
This number exists in [-2^16, 2^16]

'''


const SHIFT: int = 16
const SCALE: int = 1<< SHIFT
var raw: int



static func from_int(i: int):
	var d = DM_Decimal.new()
	d.raw = i * SHIFT 
	return d

static func from_str(s: String):
	var d = DM_Decimal.new()
	var seperated = s.split(".")
	var whole = seperated[0]
	d.raw += whole * SCALE
	var frac = 0 if seperated.size() == 1 else seperated[1]
	d.raw += frac
	return d

func copy():
	var d = DM_Decimal.new()
	d.raw = raw
	return d

func add(other: DM_Decimal):
	var d = DM_Decimal.new()
	d.raw = raw + other.raw
	return d
	
func sub(other: DM_Decimal):
	var d = DM_Decimal.new()
	d.raw = raw - other.raw
	return d
	
func mult(other: DM_Decimal):
	var d = DM_Decimal.new()
	d.raw = (raw * other.raw) >> SHIFT
	return d
	
func div(other: DM_Decimal):
	var d = DM_Decimal.new()
	@warning_ignore("integer_division")
	d.raw = raw / other.raw
	return d

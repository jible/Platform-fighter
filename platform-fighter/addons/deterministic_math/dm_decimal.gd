class_name DM_Decimal

''' 
This is a custom number data type which is the base of the deterministic math system
This number exists in [-2^16, 2^16]
'''

const SHIFT: int = 16
const SCALE: int = 1<< SHIFT
var raw: int = 0

func to_int():
	return raw >> SHIFT

static func from_int(i: int):
	var d = DM_Decimal.new()
	d.raw = i << SHIFT 
	return d


#Incorrect!
static func str_to_raw(s:String):
	var seperated = s.split(".")
	var whole = int(seperated[0])
	var output = whole * SCALE
	
	if seperated.size() > 1:
		var f_str = seperated[1]
		var frac_val = int(f_str)
		var frac_scale = pow(10.0, f_str.length()) 
		output += int(float(frac_val)  / frac_scale * SCALE)
	return output

static func from_str(s: String):
	var d = new()
	d.raw = str_to_raw(s)
	return d

func val_to_string()->String:
	var whole = to_int()
	var frac = abs(raw & (SCALE - 1))
	var frac_val = int(float(frac) / SCALE * 10000.0)
	return "%d.%04d" % [whole, frac_val]
	

func to_float()->float:
	return float(raw)/float(SCALE) 

static func from_floats_dangerous(f)-> DM_Decimal:
	var d = DM_Decimal.new()
	var shifted = f * SCALE
	d.raw = int(shifted)
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
	d.raw = (raw << SHIFT) / other.raw
	return d

func power(exponent:int):
	var d = DM_Decimal.new()
	d.raw = SCALE
	for i in range(exponent):
		d.raw = (d.raw * raw) >> SHIFT
	return d

func _root_helper(x: int):
	if x <= 0:return
	
	var result = x
	var bit = 1 <<30
	while bit > x:
		bit >>= 2
	
	var res = 0
	while bit != 0:
		if x >= res + bit:
			x -= res + bit
			res = (res >> 1) + bit
		else:
			res >>= 1
		bit >>= 2
	return res

func square_root():
	if raw <= 0:
		return DM_Decimal.from_int(0)
	var shifted = raw << SHIFT
	var d = DM_Decimal.new()
	d. raw = _root_helper(shifted)
	return d

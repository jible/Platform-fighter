@tool
extends DP_Collision_Shape
class_name DP_Rectangle


var height:DM_Decimal = DM_Decimal.from_int(50)
var width:DM_Decimal = DM_Decimal.from_int(50)




func check_overlap(other: DP_Collision_Shape):
	var overlap = false
	if other is DP_Rectangle:
		pass

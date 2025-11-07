@tool
extends DP_Collision_Shape
class_name DP_Rectangle


var height:DM_Decimal = DM_Decimal.from_int(50)
var width:DM_Decimal = DM_Decimal.from_int(50)




func check_overlap(other: DP_Collision_Shape):
	if other is DP_Rectangle:
		var overlap = (position.x.is_less_than(other.position.x.add(other.width)) and \
						position.x.add(width).is_greater_than(other.position.x)) and \
						(position.y.is_less_than(other.position.y.add(other.height)) and \
						position.y.add(height).is_greater_than(other.position.y) )
		if overlap:
			overlaps[DP_Collision_Shape.get_overlap_key(GlobalResources.get_current_match_frame())][self] = true
			print("overlapping on", GlobalResources.get_current_match_frame())
		
		return overlaps

@tool
extends DP_Collision_Shape
class_name DP_Sphere

var radius : DM_Decimal= DM_Decimal.from_int(100)


func check_overlap(other: DP_Collision_Shape):
	if !overlaps[GlobalResources.get_current_match_frame()]:
		populate_current_frame()
	if other is DP_Sphere:
		var distance = position.sub(other.position)
		var magnitude = distance.get_magnitude()
		var normalized = distance.normalize()
		var min_dist = radius.add(other.radius)
		#  Still need to handle collision right here! 
		# Push a out of b such that they are minimum distance apart by the same vector
		var target_position = other.position.add(normalized.mult(min_dist))
		return min_dist.is_greater_than(magnitude)
			

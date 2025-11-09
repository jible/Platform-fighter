@tool
extends DP_Collision_Shape
class_name DP_Rectangle


var height:DM_Decimal = DM_Decimal.from_int(50)
var width:DM_Decimal = DM_Decimal.from_int(50)

@export var editor_height: int = height.to_int():
	get():
		return height.to_int()
	set(value):
		height = DM_Decimal.from_int(value)
@export var editor_wdith: int = width.to_int():
	get():
		return width.to_int()
	set(value):
		width = DM_Decimal.from_int(value)


func check_overlap(other: DP_Collision_Shape):
	if other is DP_Rectangle:
		var overlap = (position.x.is_less_than(other.position.x.add(other.width)) and \
						position.x.add(width).is_greater_than(other.position.x)) and \
						(position.y.is_less_than(other.position.y.add(other.height)) and \
						position.y.add(height).is_greater_than(other.position.y) )
		if overlap:
			overlaps[DP_Collision_Shape.get_overlap_key(GlobalResources.get_current_match_frame())][self] = true
			if !is_trigger:
				var frame_previous = get_overlap_key(GlobalResources.get_current_match_frame() - 1)
				var prev_pos = positions[frame_previous]
				var vel = position.sub(prev_pos)
				if vel.x == DM_Decimal.from_int(0) and vel.y ==DM_Decimal.from_int(0):
					print("overlapping with 0 velocity")
					return
				var other_expanded_min = other.position.sub(DM_Vector2.from_dm_decimals(width, height))
				var other_expanded_max = other.position.add(DM_Vector2.from_dm_decimals(other.width,other.height))
				
				
				
				var t_enter_x = null
				var t_enter_y = null
				var t_exit_x = null
				var t_exit_y = null
				
				if vel.x.is_equal(DM_Decimal.from_int(0)):
					t_enter_x = ( other_expanded_min.x.sub(prev_pos.x)).div(vel.x) if vel.x != DM_Decimal.from_int(0) else null 
					t_exit_x =(other_expanded_max.x.sub(prev_pos.x)).div(vel.x) if vel.x != DM_Decimal.from_int(0) else null 
				
				if vel.y.is_equal(DM_Decimal.from_int(0)):
					t_enter_y = ( other_expanded_min.x.sub(prev_pos.y)).div(vel.y) if vel.y != DM_Decimal.from_int(0) else null 
					t_exit_y =(other_expanded_max.x.sub(prev_pos.y)).div(vel.y) if vel.y != DM_Decimal.from_int(0) else null 
				
				if vel.x.is_less_than(DM_Decimal.from_int(0)):
					var temp = t_enter_x
					t_enter_x = t_exit_x
					t_exit_x = temp
				if vel.y.is_less_than(DM_Decimal.from_int(0)):
					var temp = t_enter_y
					t_enter_y = t_exit_y
					t_exit_y = temp
				var t_enter:DM_Decimal 
				if t_enter_x and t_enter_y:
					t_enter = t_enter_x.max(t_enter.y)
				else:
					t_enter = t_enter_x if t_enter_x else t_enter_y
				var t_exit:DM_Decimal 
				if t_exit_x and t_exit_y:
					t_exit = t_exit_x.max(t_exit.y)
				else:
					t_exit = t_exit_x if t_exit_x else t_exit_y
				if t_enter.is_greater_than(t_exit):return
				if t_enter.is_greater_than(DM_Decimal.from_int(1)):return
				if t_enter.is_less_than(DM_Decimal.from_int(0)):return
				print("trinyg to move")
				position = prev_pos.add( vel.mult(t_enter)  )
		return overlaps

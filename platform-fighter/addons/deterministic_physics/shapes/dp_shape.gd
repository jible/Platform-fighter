extends Node
class_name  DP_Collision_Shape


@export var collision_layer: int
@export var collision_mask: int
@export var is_trigger: bool
@export var is_static: bool = false
@export var debug_color: Color = Color(0.0, 0.529, 0.0, 1.0)
var position: DM_Vector2 = DM_Vector2.from_ints(0,0)
var rotation: DM_Decimal = DM_Decimal.from_int(0)
static var max_overlap_queue_size: int = 50
var overlaps: Array[Dictionary] = []
var positions: Array[DM_Vector2] = []
static func get_current_overlap_key():
	return get_overlap_key(GlobalResources.get_current_match_frame())

static func get_overlap_key(frame: int):
	return frame % max_overlap_queue_size

func _ready():
	overlaps.resize(max_overlap_queue_size)
	overlaps.fill({})
	positions.resize(max_overlap_queue_size)
	for i in range(positions.size()):
		positions[i] = DM_Vector2.from_ints(0,0)

@export var editor_pos: Vector2 = position.to_standard_vector():
	get():
		return position.to_standard_vector()
	set(value):
		position = DM_Vector2.from_floats_dangerous(value.x, value.y)


func check_overlap(other: DP_Collision_Shape) -> bool:
	push_error("collision handling not configured on this shape")
	return false

func populate_current_frame():
	var current_frame_index = GlobalResources.get_current_match_frame()
	overlaps[current_frame_index] = {}
	positions[current_frame_index] = position.get_copy()

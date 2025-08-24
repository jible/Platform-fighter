class_name Hitbox
extends Area2D

"""
HITBOXES
Hitboxes are responsible for 90% of hit detection
Whenever a hitbox overlaps a hurtbox, it will check its overlap list to decide if it can hit it.
If it hits it, it adds its owner's reference to its overlap list.
Once the hitbox turns off, its overlap list is cleared.
For special multi-hit moves, you can make explicit calls to clear the overlap list without needing 
to turn it off and back on.

HURTBOXES
The only instance where a hurtbox is responsible for detecting a hit is when a hurtbox turns on. 
When a hurtbox turns on, it checks all overlapping objects. It then tells all of those objects that it has 
been turned on. Due to layer configuring it should only be able to overlap hitboxes. 
Each hitbox will decide how to handle the hit as necessary, given the overlap details.

HITBOX CLUSTERS
A hitbox cluster is a collection of hitboxes. Some moves may hav multiple hitboxes active at the same time, 
but enemies should only be able to be hurtby one of them at once. In that case, use a hitbox cluster.
Hitbox clusters parent hitboxes that belong to them. If a hitbox belonging to a hit box cluster overlaps a 
hurtbox, it refers to its hitbox cluster's hit_list. If the hurtbox is in that list, don't hit it. If it is, 
hit it and add it to the list.

An example of a use for this is a move with a sour spot and a sweet spot. If you land a move in the right spot, 
it should deal more damage, but if it is in the middle, it should not get hit by the weak and strong spot.
"""


signal landed_hit(target, hit_data)

var on: bool = true


var hit_data = {}
@export var damage: int = 0
@export var knockback_magnitude: float = 0
@export var knockback_direction: Vector2 = Vector2(0,0) :
	set(value):
		knockback_direction = value.normalized()

var cluster = null

var successful_hit_list: Array[Health] = []

func _ready():
	hit_data = {
		"damage": damage,
		"knockback_magnitude" : knockback_magnitude,
		"knockback_direction" : knockback_direction,
	}
	var parent = get_parent()
	if is_instance_of(parent, HitboxCluster):
		cluster = parent
		# Make its list reference a reference to the cluster's list
		successful_hit_list = cluster.successful_hit_list


func turn_off():
	if !on:return
	monitoring = false
	monitorable = false
	if !cluster:
		successful_hit_list = []

func turn_on():
	if on:return
	monitoring = true
	monitorable = true


func _on_area_entered(hurtbox:Hurtbox):
	var other_health = hurtbox.health
	if !other_health or other_health in successful_hit_list:return 
	successful_hit_list.append(other_health)
	
	hurtbox.hit_by(self, hit_data)
	emit_signal("landed_hit", hurtbox, hit_data)

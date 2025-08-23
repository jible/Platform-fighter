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



@export var damage: int = 0
@export var knockback: float = 0
@export var normalized_knockback_direction: Vector2 =Vector2(0,0) 

var cluster = null

var cluster_member: bool = false
var successful_hit_list: Array[Hurtbox] = []

func _ready():
	var parent = get_parent()
	if is_instance_of(parent, HitboxCluster):
		cluster = parent
		# Make its list reference a reference to the cluster's list instead
		# That way it can directly modify the same list as the rest of the cluster members. 
		successful_hit_list = cluster.successful_hit_list
		
		
func on_overlap(hurtbox: Hurtbox):
	var player = hurtbox.get_player()
	if player in successful_hit_list:
		return
	
	successful_hit_list.append(player)
	hurtbox.hit_by(self)
	
	

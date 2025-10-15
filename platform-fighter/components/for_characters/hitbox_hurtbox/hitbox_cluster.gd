class_name HitboxCluster
extends Node2D


var successful_hit_list: Array[Health] = []

# New hitbox cluster behavior:
# All hitboxes belonging to a cluster share a hit list: this successful hit list
# This hit list will stay for the duration of the state and only be removed once the current state is over
# This allows cluster members to turn on and off asyncronously, which makes for more flexible behavior. 



# State calls this when the state is over
func clear_hit_list():
	successful_hit_list = []

class_name HitboxCluster
extends Node2D


var successful_hit_list: Array[Health] = []

func turn_off():
	successful_hit_list = []
	for child in get_children():
		child.turn_off()

func turn_on():
	for child in get_children():
		child.turn_on()

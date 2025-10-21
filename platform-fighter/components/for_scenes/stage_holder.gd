class_name StageHolder
extends Node2D

@export var stage_path: String = ""
var stage: Node

func instance_stage():
	var scene = load(stage_path)
	var instance = scene.instantiate()
	add_child(instance)
	instance.name = "stage"
	stage = instance

@tool
class_name InputManager
extends Node


"""
This script parses inputs and feeds them to the player

In the future it should handle them taking account for the player's specialized binds
"""

var drift_threshold = .1

const button_default_controls: Dictionary[int, String] ={
	0:"B",
	1:"A",
	2:"Y",
	3:"X",
	9:"L",
	10:"R",
	11:"d_up",
	12:"d_down",
	13:"d_left",
	14:"d_right",
}

const axis_default_controls: Dictionary[int, String] = {
	0: "stick_1_horizontal",
	1: "stick_1_vertical",
	2: "stick_2_horizontal",
	3: "stick_2_vertical",
	4: "ZL",
	5: "ZR",
}



func _input(event):
	if event is InputEventJoypadButton:
		print(button_default_controls[event.button_index])
	elif event is InputEventJoypadMotion:
		if abs(event.axis_value) >  drift_threshold:
			print(axis_default_controls[event.axis])

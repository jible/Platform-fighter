class_name ControllerState
extends Resource


enum Button_Types {
	SPECIAL,
	LIGHT,
	GRAB,
	JUMP,
	LEFT_UP,
	LEFT_RIGHT,
	LEFT_LEFT,
	LEFT_DOWN,
}


var button_states: Array[bool] = []

func _init():
	button_states.resize(Button_Types.size())
	for button_type in Button_Types.values():
		button_states[button_type] = false

var sticks = {
	0 : Vector2.ZERO,
	1 : Vector2.ZERO
}

func get_copy()->ControllerState:
	var new = ControllerState.new()
	new.button_states = button_states.duplicate()
	new.sticks = sticks.duplicate(true)
	return new


func get_encoded():
	var buffer = StreamPeerBuffer.new()
	var button_binary = 0
	for button in range(button_states.size()):
		var new_value = button_states[button]
		button_binary += new_value << button
	buffer.put_16(button_binary)
	
func set_button(button, value: bool):
	button_states[button] = value

func get_button(button: Button_Types):
	return button_states.get(button)

#func config_from_encoded(encoded):
	#pass

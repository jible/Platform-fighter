extends CharacterState

var mobility_manager: MobilityManager

func _ready():
	super()
	mobility_manager = character.mobility_manager


func condition():
	return Input.is_action_just_pressed("jump") and mobility_manager.can_jump()

func enter_state():
	mobility_manager.jump()

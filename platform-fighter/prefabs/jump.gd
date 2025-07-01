extends CharacterState

var mobility_manager: MobilityManager

func _ready():
	super()
	mobility_manager = character.mobility_manager

func enter_state():
	mobility_manager.jump()

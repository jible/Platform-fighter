extends CharacterState

var mobility_manager: MobilityManager

func _ready():
	super()
	mobility_manager = character.mobility_manager

func enter_state():
	mobility_manager.jump()
	#TODO Change this to happen at the end of anim
	state_machine.change_state("Ariel")

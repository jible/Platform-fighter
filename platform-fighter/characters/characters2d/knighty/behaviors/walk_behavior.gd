extends CharacterBehavior


@export var character_body: SpecializedCharacterBody
# WHEN MAKING A CLASS BEHAVIOR, COPY AND PASTE THESE FUNCTIONS INTO IT
# If the priority is correct and the state is correct, check this condition every frame. If it is true, do trigger the behavior  
func condition()->bool:
	return abs(character_body.velocity.x) > 3
	
# When the condition is true, perform this action
func trigger()->void:
	state_machine.change_state("Walk")
	pass

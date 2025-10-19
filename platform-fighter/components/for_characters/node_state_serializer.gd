extends Resource
class_name NodeStateSerializer

'''
Extracts and imbues state into the node its assigned to  
'''

@export var property_name:String

func extract_state(owner: Node):
	return owner[property_name]

func imbue_state(owner, value):
	owner[property_name] = value

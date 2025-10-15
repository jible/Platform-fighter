class_name RbAnimationKey
extends Resource

enum KeyType {
	METHOD,
	PROPERTY
}

var type: KeyType
var path: NodePath
var attribute: String
var value: Variant

func configure(_path: NodePath, _type: KeyType, _attribute: String, _value: Variant):
	type = _type
	path = _path
	attribute = _attribute
	value = _value

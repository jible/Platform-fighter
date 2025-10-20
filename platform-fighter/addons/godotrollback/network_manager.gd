extends Node

const PORT := 9000
const LOCALHOST:= "127.0.0.1"
const MAXCLIENTS:=  2
'''
I mostly used the documentation, but this helped fill in what didn't make sense:
https://chatgpt.com/c/68f68708-6c54-8333-9842-a086480ac1ea '''

signal started_to_host

enum ConnectionType { 
	HOST,
	CLIENT,
}
var connection_type : ConnectionType

var peer: ENetMultiplayerPeer

func _ready():
	print("press h to host or j to join")
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_host)
	multiplayer.connection_failed.connect(_on_connected_fail)
	multiplayer.server_disconnected.connect(_on_server_disconnected)
	
func _process(delta):
	if connection_type == null:
		if Input.is_action_just_pressed("debug_host"):
			start_game(PORT)
		if Input.is_action_just_pressed("debug_join"):
			join_game(LOCALHOST, PORT)
		
func start_game(port):
	peer = ENetMultiplayerPeer.new()
	var err = peer.create_server(port, MAXCLIENTS)
	if err != OK:
		push_error("Failed to create server: %s" % err)
	
	multiplayer.multiplayer_peer = peer
	connection_type = ConnectionType.HOST
	print("Hosting on port %d" % port)
	
func join_game(ip, port):
	peer = ENetMultiplayerPeer.new()
	var err = peer.create_client(ip, port)
	if err != OK:
		push_error("Failed to create server: %s" % err)
	
	multiplayer.multiplayer_peer = peer
	connection_type = ConnectionType.CLIENT
	
	print("Joining on port %d" % port)            

func _on_peer_connected(id):
	print("peer connected with ID: ", id)
	pass

func _on_server_disconnected():
	print("peer disconnected")
	pass

func _on_connected_to_host():
	pass

func _on_connected_fail():
	print("connection failed")
	pass

func _on_peer_disconnected(id):
	pass

@rpc("any_peer")
func receive_message(msg: String):
	print("got message: " , msg)

func send_test_message():
	rpc("receive_message", "Hello from peer %s" % multiplayer.get_unique_id())

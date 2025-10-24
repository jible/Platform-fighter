extends Node

const PORT := 9000
const TARGET_IP:= "127.0.0.1"
const MAXCLIENTS:=  2
var current_lobby_size: int = 0
var target_lobby_size: int = 2

var rollback_port: int
var rollback_ip: String

var host_rollback_ip = null
var host_rollback_port = null

var host_ip = null
var host_port = null
'''
I mostly used the documentation, but this helped fill in what didn't make sense:
https://chatgpt.com/c/68f68708-6c54-8333-9842-a086480ac1ea 


This is used for establishing a connection with another system, but not the rollback connection.
The play scene will have a rollback communication manager.
'''

signal started_to_host

signal message_receieved(msg: String)

enum ConnectionType { 
	HOST,
	CLIENT,
	DISCONNECTED
}

var connection_type : ConnectionType = ConnectionType.DISCONNECTED
var readied_up: bool = false
var peer: ENetMultiplayerPeer

func _ready():
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_host)
	multiplayer.connection_failed.connect(_on_connection_fail)
	multiplayer.server_disconnected.connect(_on_server_disconnected)

func start_game(port):
	print("starting")
	peer = ENetMultiplayerPeer.new()
	var err = peer.create_server(port, MAXCLIENTS)
	if err != OK:
		push_error("Failed to create server: %s" % err)
	
	multiplayer.multiplayer_peer = peer
	connection_type = ConnectionType.HOST
	print("Hosting on port %d" % port)
	host_rollback_port = PORT + 1
	host_rollback_ip = get_safe_ip()
	print_lobby_status()
	

func join_game(ip, port):
	print("joining")
	peer = ENetMultiplayerPeer.new()
	var err = peer.create_client(ip, port)
	if err != OK:
		push_error("Failed to create server: %s" % err)
	
	multiplayer.multiplayer_peer = peer
	connection_type = ConnectionType.CLIENT

func print_lobby_status():
	if connection_type != ConnectionType.HOST:
		return
	var all_peers = multiplayer.get_peers()
	print("Lobby Size: ", all_peers.size() + 1)
	print(str("ip: " + str(TARGET_IP) +", port: " + str(PORT)))
	for peer_id in all_peers:
		var player = peer.get_peer(peer_id)
		var player_ip = player.get_remote_address()
		var player_port = player.get_remote_port()
		print(str("ip: " + str(player_ip) +", port: " + str(player_port)))

func _on_peer_connected(id):
	print_lobby_status()

func _on_server_disconnected():
	print_lobby_status()

func _on_connected_to_host():
	print_lobby_status()

func _on_connection_fail():
	print("connection failed")

func _on_peer_disconnected(id):
	pass


func get_host_info():
	if connection_type != ConnectionType.CLIENT:
		return
	rpc_id(1,"give_host_connection_properties")
	
@rpc("any_peer")
func give_host_connection_properties():
	var requester_id = multiplayer.get_remote_sender_id()
	if connection_type!= ConnectionType.HOST: return
	rpc_id(requester_id, "receive_host_connection_properties", TARGET_IP, PORT, host_rollback_ip, host_rollback_port)

signal received_host_connection_properties()

@rpc("any_peer")
func receive_host_connection_properties(ip_address, port, rollback_address, rollback_port):
	host_ip = ip_address
	host_port = port
	host_rollback_ip = rollback_address
	host_rollback_port = rollback_port
	
	received_host_connection_properties.emit()


func get_safe_ip():
	var ips = IP.get_local_addresses()
	for address in ips:
		if address.begins_with("192."):
			return address

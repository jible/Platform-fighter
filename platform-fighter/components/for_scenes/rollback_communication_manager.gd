class_name RollbackCommunicationManager
extends Node



enum PacketType {
	NOTIFY_HOST_OF_CONNECTION,
	POLL_ROUND_TRIP_TICKS,
	ANSWER_ROUND_TRIP_TICKS,
	CLIENT_DATA,
	MESSAGE,
	INPUT,
}
@export var play_scene_manager: PlaySceneManager
@export var input_manager: InputManager

var udp: PacketPeerUDP
var game_time: int = 0 
var round_trip_ticks: int
var latency
var host_data = {}
var client_data = []
var client_latency = []
func start_connection():
	match NetworkManager.connection_type:
		NetworkManager.ConnectionType.HOST:
			start_host_rollback(NetworkManager.host_rollback_port, NetworkManager.host_rollback_ip)
		NetworkManager.ConnectionType.CLIENT:
			prepare_to_join_rollback()

func start_host_rollback(port, ip_address):
	udp = PacketPeerUDP.new()
	var err = udp.bind(port, ip_address)
	if err!= OK:
		push_error("Failed to bind udp on port %s" % port)
		return
	host_data["port"] = port
	host_data["address"] = ip_address

func prepare_to_join_rollback():
	udp = PacketPeerUDP.new()
	var err = udp.bind(0)
	if err != OK:
		push_error("Failed to bind UDP client socket")
		return
	NetworkManager.received_host_connection_properties.connect(connect_to_host)
	NetworkManager.get_host_info()

func send_encoded_input_packet(encoded_packet):
	var buffer = StreamPeerBuffer.new()
	buffer.put_8(PacketType.INPUT)
	buffer.put_data(encoded_packet)
	


func connect_to_host():
	print("got host connection proprerties")
	var ip = NetworkManager.get_safe_ip()
	print("Connecting to:", ip)
	udp.connect_to_host(NetworkManager.host_rollback_ip, NetworkManager.host_rollback_port)
	notify_host_of_connection()

func _process(_delta):
	if GlobalResources.connection_mode == GlobalResources.ConnectionMode.LOCAL: return
	if udp and udp.get_available_packet_count():
		var packet = udp.get_packet()
		handle_packet(packet)
	if Input.is_action_just_pressed("attack"):
		if NetworkManager.connection_type == NetworkManager.ConnectionType.HOST:
			send_message("hello from host")
		else:
			print("not host")
func handle_packet(packet: PackedByteArray):
	if packet.size() < 1:
		# Packet dropped bytes
		return
	var packet_type = packet[0]
	match packet_type:
		PacketType.POLL_ROUND_TRIP_TICKS:
			var start_time_bytes = packet.slice(1)
			answer_round_trip_ticks(start_time_bytes)
		PacketType.ANSWER_ROUND_TRIP_TICKS:
			var start_time_bytes = packet.slice(1)
			var stream = StreamPeerBuffer.new()
			stream.set_data_array(start_time_bytes)
			var start_time = stream.get_64()
			receive_round_trip_ticks(start_time)
		PacketType.NOTIFY_HOST_OF_CONNECTION:
			handle_client_connection()
		PacketType.CLIENT_DATA:
			receive_client_connection_data(packet)
		PacketType.MESSAGE:
			receive_message(packet)
		PacketType.INPUT:
			input_manager.receive_network_input(packet)

func handle_client_connection():
	var new_client_data = {
		"port": udp.get_packet_port(),
		"address":  udp.get_packet_ip(),
	}
	# Add new client data
	client_data.append(new_client_data)
	for client_index in range(client_data.size()):
		send_all_client_data(client_index)
	
	poll_round_trip_ticks()

func send_all_client_data(target_client_index):
	var buffer = StreamPeerBuffer.new()
	buffer.put_u8(PacketType.CLIENT_DATA)
	buffer.put_u8(client_data.size())
	for c in client_data:
		var address_bytes = c.address.to_utf8_buffer()
		buffer.put_8(address_bytes.size())
		buffer.put_data(address_bytes) 
		buffer.put_u16(c.port)
	send_data_to_client(buffer.get_data_array(), target_client_index)
	print(client_data)
	
func send_data_to_client(data, target_client_index):
	var target_client_data = client_data[target_client_index]
	udp.set_dest_address(target_client_data.address, target_client_data.port)
	udp.put_packet(data)

func receive_client_connection_data(packet):
	var parse_buffer = StreamPeerBuffer.new()
	parse_buffer.set_data_array(packet)
	# Set cursor to key of address length
	parse_buffer.seek(1)
	var client_count = parse_buffer.get_u8()
	client_data.clear()
	for i in client_count:
		var length_of_address = parse_buffer.get_u8()
		var address = parse_buffer.get_string(length_of_address)
		var port = parse_buffer.get_u16()
		client_data.append({"address": address, "port": port})
	
func send_message(message: String):
	var b = StreamPeerBuffer.new()
	b.put_8(PacketType.MESSAGE)
	b.put_string(message)
	send_data_to_client( b.get_data_array(), 0)

func receive_message(data):
	var b = StreamPeerBuffer.new()
	b.set_data_array(data)
	b.seek(1)
	print("received message")
	print(b.get_string())
	return

func notify_host_of_connection():
	print("notifying host")
	udp.put_packet(PackedByteArray([PacketType.NOTIFY_HOST_OF_CONNECTION]))

func poll_round_trip_ticks():
	var current_time_buffer= StreamPeerBuffer.new()
	current_time_buffer.put_64(Engine.get_physics_frames())
	udp.put_packet(PackedByteArray([PacketType.POLL_ROUND_TRIP_TICKS]) + current_time_buffer.data_array)

func answer_round_trip_ticks(start_time: PackedByteArray):
	udp.put_packet(PackedByteArray([PacketType.ANSWER_ROUND_TRIP_TICKS]) + start_time)

func receive_round_trip_ticks(start_time: int):
	var current_time = Engine.get_physics_frames()
	round_trip_ticks = ( current_time- start_time)
	if current_time < start_time:
		round_trip_ticks += (1 << 64)
		
	@warning_ignore("integer_division")
	latency = round_trip_ticks/2
	client_latency.append(latency)

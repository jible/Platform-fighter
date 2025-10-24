class_name RollbackCommunicationManager
extends Node



enum PacketType {
	NOTIFY_HOST_OF_CONNECTION,
	POLL_ROUND_TRIP_TICKS,
	ANSWER_ROUND_TRIP_TICKS,
	INPUT,
}

var udp: PacketPeerUDP
var game_time: int = 0 
var packet_queue: Array[PackedByteArray] = []
var round_trip_ticks: int
var latency
var peers = []
var client_address
var client_port
func start_connection():
	match NetworkManager.connection_type:
		NetworkManager.ConnectionType.HOST:
			start_host_rollback(NetworkManager.PORT + 1)
		NetworkManager.ConnectionType.CLIENT:
			prepare_to_join_rollback()

func start_host_rollback(port):
	udp = PacketPeerUDP.new()
	var err = udp.bind(port)
	if err!= OK:
		push_error("Failed to bind udp on port %s" % port)
		return

func prepare_to_join_rollback():
	udp = PacketPeerUDP.new()
	var err = udp.bind(0)
	if err != OK:
		push_error("Failed to bind UDP client socket")
		return
	NetworkManager.received_host_connection_properties.connect(func():
		print("got host connection proprerties")
		var ip = IP.get_local_addresses()[0]
		print("Connecting to:", ip)
		udp.connect_to_host(ip, NetworkManager.host_port + 1)
		notify_host_of_connection()
	)
	NetworkManager.get_host_info()

func _process(_delta):
	if udp and udp.get_available_packet_count():
		var packet = udp.get_packet()
		handle_packet(packet)
		print(packet)

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

func handle_client_connection():
	client_port = udp.get_packet_port()
	client_address = udp.get_packet_ip()
	
	udp.set_dest_address(client_address, client_port)
	
	print("client joined")
	poll_round_trip_ticks()
	

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
	print("rount_trip_ticks: ", round_trip_ticks)
	print("latency (in ticks)~ ", latency)

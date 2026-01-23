using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

public partial class RollbackManager : Node
{
    public RollbackManager GloablInstance;
    public static int MAX_ROLLBACK_FRAMES = 50;


    public enum PacketType
    {
        NOTIFY_HOST_OF_CONNECTION,
        POLL_ROUND_TRIP_TICKS,
        CLIENT_DATA,
        MESSAGE,
        INPUT,
    }

    
    [Signal] // The packet type is an int since it doesn't like when you put non-godot types (like c# enums) in signals
    public delegate void UdpDataReceivedEventHandler(int packetType, byte[] Packet);    

    PacketPeerUdp udp;
    int RoundTripTicks = 0;
    HashSet<Tuple<string, int>> ClientData= new();
    List<object> ClientLatency = [];

    public override void _EnterTree()
    {
        GloablInstance = this;
    }

    public Tuple<string, int> MakeUserID( string Address, int Port)
    {
        return new (Address, Port);
    }

    private void StartHostRollback(int Port, String IpAddress)
    {
        udp = new PacketPeerUdp();
        var err = udp.Bind(Port, IpAddress);
        if (err != Error.Ok)
        {
            throw new ArgumentException($"Failed to bind UDP on port {Port}");
        }
    }

    public void NotifyHostOfConnection()
    {
        byte[] Packet = [(byte)PacketType.NOTIFY_HOST_OF_CONNECTION];
        udp.PutPacket(Packet);
    }

    public void JoinRollback(string ClientAddress, int ClientPort, string HostAddress, int HostPort)
    {
        udp = new PacketPeerUdp();
        var err = udp.Bind(ClientPort, ClientAddress);
        if (err != Error.Ok)
        {
            throw new ArgumentException("Failed to bind UDP  client socket");
        }
        udp.ConnectToHost(HostAddress, HostPort);
        udp.SetDestAddress(HostAddress, HostPort);
    }

    public void SendEncodedPackets(PacketType packetType, byte[] EncodedPacket)
    {
        
        byte[] Buffer = new byte[1 + EncodedPacket.Length];
        Buffer[0] = (byte)packetType;
        Array.Copy(EncodedPacket, 0, Buffer, 1, EncodedPacket.Length);


        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
        {
            foreach (var target in ClientData)
            {
                udp.SetDestAddress(target.Item1, target.Item2);
                udp.PutPacket(Buffer); 
            }
        } else if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.CLIENT)
        {
            udp.PutPacket(Buffer);   
        }
    }

    public void HandlePacket(byte[] Packet, string Address, int Port)
    {
        if (Packet.Count() < 1)
        {
            // Dropped bytes
            return;
        }

        PacketType packetType = (PacketType)Packet[0];
        byte[] PacketWithNoPrefix = Packet[1..];
        
        EmitSignal("UdpDataReceived", (int)packetType, PacketWithNoPrefix);

        // switch case for handling message the rollback manager will use (like latency polling)
        switch(packetType){
            case PacketType.NOTIFY_HOST_OF_CONNECTION:
                ClientData.Add(MakeUserID(Address, Port));
                break;
        }
    }




    public override void _Process(double delta)
    {
        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.DISCONNECTED)
        {
            return;
        }

        if ( udp != null && udp.GetAvailablePacketCount() != 0)
        {
         var Packet = udp.GetPacket();
            
            string Address = udp.GetPacketIP();
            int Port = udp.GetPacketPort();
            HandlePacket(Packet, Address,Port);
            
        }


    }

}




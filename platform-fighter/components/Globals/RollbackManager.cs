using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class RollbackManager : Node
{
    public RollbackManager GloablInstance;
    public static int MAX_ROLLBACK_FRAMES = 50;


    enum PacketType
    {
        NOTIFY_HOST_OF_CONNECTION,
        POLL_ROUND_TRIP_TICKS,
        CLIENT_DATA,
        MESSAGE,
        INPUT,
    }

    
    NetworkManager.ConnectionType connectionType;


    PacketPeerUdp udp;
    int RoundTripTicks = 0;
    UserUdpData HostData;
    List<UserUdpData> ClientData= [];
    List<object> ClientLatency = [];

    public override void _Ready()
    {
        GloablInstance = this;
    }

    private void StartHostRollback(int Port, String IpAddress)
    {
        udp = new PacketPeerUdp();
        var err = udp.Bind(Port, IpAddress);
        if (err != Error.Ok)
        {
            throw new ArgumentException($"Failed to bind UDP on port {Port}");
        }
        HostData = new(Port, IpAddress);
    }

    public void PrepareToJoinRollback()
    {
        udp = new PacketPeerUdp();
        var err = udp.Bind(0);
        if (err != Error.Ok)
        {
            throw new ArgumentException("Failed to bind UDP  client socket");
        }

    }


}


public class UserUdpData
{
    public int Port;
    public string IpAddress;
    public UserUdpData(int _port,string _ipAddress)
    {
        Port = _port;
        IpAddress = _ipAddress;
    }
}

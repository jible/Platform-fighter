using Godot;
using System;

public partial class NetworkManager : Node
{

    /*
    This class facilitates communication between 2 systems. 
    It uses Godot's Enet communication to coordinate and establish lobbies. 
    Instances of these on seperate systems exchange required info to make the UDP connection for 
    rollback netcode.

    To view UDP connection methods, view RollbackManager.cs

    */
    public enum ConnectionType
    {
        HOST,
        CLIENT,
        DISCONNECTED
    }

    public static int DefaultTestingPort = 8000;
    public static string DefaultTargetIp = "127.0.0.1";
    int MAX_CLIENTS = 2;


    int CurrentLobbySize = 0;
    int TargetLobbySize = 2;

    UserNetworkData RollBackData;
    int RollbackPort;
    int RollbackIp;


    string HostRollbackIp;
    int HostRollbackPort;

    public delegate void StartedToHostEventHandler();
    public delegate void MessageReceivedEventHandler(string Message);

    public static NetworkManager GlobalInstance;
    public ConnectionType connectionType = ConnectionType.DISCONNECTED;

    bool ReadiedUp = false;
    ENetMultiplayerPeer Peer;

    public override void _Ready()
    {
        GlobalInstance = this; 

        Multiplayer.PeerConnected += (Peer) =>
        {
            PrintLobbyStatus();
        };

        Multiplayer.PeerDisconnected += (Peer) =>
        {
            PrintLobbyStatus();
        };

        Multiplayer.ConnectedToServer += () =>
        {
            PrintLobbyStatus();
        };

        Multiplayer.ConnectionFailed += () =>
        {
            PrintLobbyStatus();
        };

        Multiplayer.ServerDisconnected += () =>
        {
            PrintLobbyStatus();
        };
    }

    public void StartGame(int Port)
    {
        Peer = new ENetMultiplayerPeer();

        var err = Peer.CreateServer(Port, MAX_CLIENTS);
        if (err != Error.Ok)
        {
            throw new ArgumentException($"Failed to create server: {err}");    
        }

        Multiplayer.MultiplayerPeer = Peer;
        connectionType = ConnectionType.HOST;
        GD.Print($"Hosting on port {Port}");

        HostRollbackPort = Port + 1;
        HostRollbackIp = GetSafeIp();
        PrintLobbyStatus();
    }


    public void JoinGame(string _ip, int _port)
    {
        Peer = new ENetMultiplayerPeer();
        var err = Peer.CreateClient(_ip, _port);
        if (err != Error.Ok)
        {
            throw new ArgumentException ($"Failed to join server");
        }

        Multiplayer.MultiplayerPeer = Peer;
        connectionType = ConnectionType.CLIENT;
    }

    public void PrintLobbyStatus()
    {
        if (connectionType != ConnectionType.HOST) return;

        var AllPeers = Multiplayer.GetPeers();
        GD.Print($"Lobby Size: {AllPeers.Length + 1}-----------------------------------------");
        GD.Print($"HOST IP: {HostRollbackIp}, Port: {HostRollbackPort}");

        foreach (var PeerId in AllPeers)
        {
            ENetPacketPeer Player = Peer.GetPeer(PeerId);
            string PlayerIP = Player.GetRemoteAddress();
            int PlayerPort = Player.GetRemotePort();
            GD.Print($"Peer{PeerId} IP:{PlayerIP}, Port: {PlayerPort}");
        }
        GD.Print("---------------------------------------------------------------------------");
    }

    public void GetHostInfo()
    {
        if (connectionType != ConnectionType.CLIENT)
        {
            return;
        }

        RpcId(1, "GiveHostConnectionProperties");
    }

    [Rpc]
    public void GiveHostConnectionProperties()
    {
        var RequesterID = Multiplayer.GetRemoteSenderId();
        if (connectionType != ConnectionType.HOST) return;
        RpcId(RequesterID, "ReceiveHostConnectionProperties", HostRollbackIp, HostRollbackPort);
    }

    [Rpc]
    public void ReceiveHostConnectionProperties(string rollbackAddress, int rollbackePort)
    {
        HostRollbackIp = rollbackAddress;
        HostRollbackPort = rollbackePort;
    }

    public string GetSafeIp()
    {
        String[] Ips = IP.GetLocalAddresses();

        foreach (var address in Ips)
        {
            if (address.StartsWith("192.") || address.StartsWith("10."))
            {
                return address;
            }
        }
        return "127.0.0.1";
    }
}


public class UserNetworkData
{
    public int EnetPort;
    public string IpAddress;
    public int RollbackPort;
    public UserNetworkData(int _EnetPort,string _IpAddress, int _RollbackPort = -1)
    {   
        EnetPort = _EnetPort;
        IpAddress = _IpAddress;
        RollbackPort = _RollbackPort;
        if (RollbackPort < 0)
        {
            RollbackPort = EnetPort + 1;
        }
    }
}
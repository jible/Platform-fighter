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

    int PORT = 9000;
    string TARGET_IP = "127.0.0.1";
    int MAX_CLIENTS = 2;


    int CurrentLobbySize = 0;
    int TargetLobbySize = 2;

    
    int RollbackPort;
    int RollbackIp;


    string HostRollbackIp;
    int HostRollbackPort;

    public delegate void StartedToHostEventHandler();
    public delegate void MessageReceivedEventHandler(string Message);


    ConnectionType connectionType = ConnectionType.DISCONNECTED;

    bool ReadiedUp = false;
    ENetMultiplayerPeer Peer;

    public override void _Ready()
    {
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

        HostRollbackPort = PORT + 1;
        HostRollbackIp = GetSafeIp();
        PrintLobbyStatus();
    }


    public void JoinGame(String _ip, int _port)
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
        GD.Print($"HOST IP: {TARGET_IP}, Port: {PORT}");

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

        RpcId(1, "give_host_connection_properties");
    }

    [Rpc]
    public void GiveHostConnectionProperties()
    {
        var RequesterID = Multiplayer.GetRemoteSenderId();
        if (connectionType != ConnectionType.HOST) return;
        RpcId(RequesterID, "receive_host_connection_properties", HostRollbackIp, HostRollbackPort);
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

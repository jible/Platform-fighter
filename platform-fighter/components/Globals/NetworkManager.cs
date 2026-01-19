using Godot;
using System;
using System.Reflection.PortableExecutable;

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

    [Signal]
    public delegate void PlayerAddedRequestEventHandler(int RemotePlayerPeerID);
    [Signal]
    public delegate void PlayerAddedNotificationEventHandler(int PlayerNumber, int PeerId,  bool IsLocal);    

    string HostRollbackIp;
    int HostRollbackPort;
    public static NetworkManager GlobalInstance;
    public ConnectionType connectionType = ConnectionType.DISCONNECTED;

    bool ReadiedUp = false;
    ENetMultiplayerPeer Peer;

    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalInstance = this;
    }
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

    // Lobby info functions
    // RPC PREFIX INDICATES WHAT MACHINE THE FUNCTION SHOULD BE RUN ON (not called on) 
    // CTH = CLIENT_TO_HOST
    // HTC = HOST_TO_CLIENT
    public void RequestAddPlayer(){
        RpcId ( MultiplayerPeer.TargetPeerServer, "OnAddPlayerRequest");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)] 
    public void OnAddPlayerRequest()
    {
        int RemotePlayerPeerID = Multiplayer.GetRemoteSenderId();
        EmitSignal("PlayerAddedRequest", RemotePlayerPeerID);
    }   
    
    public void NotifyPlayerAdded(int PlayerNumber, int PlayerPeerID)
    {
        RpcId ( MultiplayerPeer.TargetPeerBroadcast, "OnNotifyPlayerAdded", PlayerNumber, PlayerPeerID);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void OnNotifyPlayerAdded(int PlayerNumber, int PeerId)
    {
        bool IsLocal = PeerId == Peer.GetUniqueId();
        GD.Print(IsLocal);
        EmitSignal("PlayerAddedNotification", PlayerNumber, PeerId, IsLocal);
    }
    

    // Helper Functions
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
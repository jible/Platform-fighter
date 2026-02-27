using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using Godot.NativeInterop;
using System.Security.Cryptography.X509Certificates;

public partial class NetworkManager : Node
{

    /*
    This class facilitates communication between 2 systems. 
    It uses Godot's Enet communication to coordinate and establish lobbies. 
    Instances of these on seperate systems exchange required info to make the UDP connection for 
    rollback netcode.

    To view UDP connection methods, view RollbackManager.cs

    */
    public static NetworkManager GlobalInstance;

    // Enums--------------------------------------------------------------------------------------------------------------------------
    public enum ConnectionType
    {
        HOST,
        CLIENT,
        DISCONNECTED
    }           
    public enum NetworkMessageType
    {
        // Host Methods
        PlayerAdded,
        EnterMatch,
        SyncLobbyState,

        // Client Methods
        RequestAddPlayer,

    }

    // Return true if host can use this method
    public bool IsHostMessage(NetworkMessageType MessageType)
    {
        return MessageType is
            NetworkMessageType.PlayerAdded or 
            NetworkMessageType.EnterMatch or 
            NetworkMessageType.SyncLobbyState;
    }
    // Returns true if client can use this method
    public bool IsClientMessage( NetworkMessageType messageType)
    {
        return messageType is NetworkMessageType.RequestAddPlayer;
    }

               
    
    // Default Lobby Location/ Settings--------------------------------------------------------------------------------------------------------------------------

    public static int DefaultTestingPort = 8000;
    public static string DefaultTargetIp = "127.0.0.1";
    int MAX_CLIENTS = 2;

    // Runtime Lobby Data --------------------------------------------------------------------------------------------------------------------------
    public LobbyManager lobbyManager;

    // Host Data
    int HostPort;
    string HostIP;
    // Signals --------------------------------------------------------------------------------------------------------------------------
    [Signal]
    public delegate void MessageReceivedEventHandler(NetworkMessageType messageType, Godot.Collections.Dictionary MessageData, int Sender);    
    
    // Lobby State--------------------------------------------------------------------------------------------------------------------------
    public ConnectionType connectionType = ConnectionType.DISCONNECTED;

    ENetMultiplayerPeer Peer;
    // Standard Methods --------------------------------------------------------------------------------------------------------------------------
    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalInstance = this;

    }

    public override void _Ready()
    {
        base._Ready();
        lobbyManager = new();
        lobbyManager.ConfigLobby();
    }


    public void SendMessageSpecificClient(NetworkMessageType messageType, Godot.Collections.Dictionary MessageData, int ClientId)
    {
        if (connectionType != ConnectionType.HOST)
        {
            GD.Print("This is a host only method");
        }
        RpcId(ClientId, "ReceiveMessage",(int) messageType, MessageData);
    }
    
    public void SendMessage(NetworkMessageType messageType, Godot.Collections.Dictionary MessageData)
    {
        if (connectionType != ConnectionType.HOST && IsHostMessage(messageType))
        {
            GD.Print("Attempting to send Host Message from non-host");
        }
        if (connectionType != ConnectionType.CLIENT && IsClientMessage(messageType))
        {
            GD.Print("Attempting to send client Message from non-client");
        }
        Rpc("ReceiveMessage",(int) messageType, MessageData);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ReceiveMessage(int messageID, Godot.Collections.Dictionary MessageData)
    {
        int Sender = Multiplayer.GetRemoteSenderId();
        EmitSignal ("MessageReceived", messageID, MessageData, Sender);
    }



    // Host Methods --------------------------------------------------------------------------------------------------------------------------
    public void StartLobby( string Address, int Port)
    {
        Peer = new ENetMultiplayerPeer();

        var err = Peer.CreateServer(Port, MAX_CLIENTS);
        if (err != Error.Ok)
        {
            throw new ArgumentException($"Failed to create server: {err}");    
        }
        HostPort = Port;
        HostIP = Address;


        Multiplayer.MultiplayerPeer = Peer;
        connectionType = ConnectionType.HOST;
        GD.Print($"Hosting on port {Port} at address {Address}");

        PrintLobbyStatus();
    }

    public void JoinLobby(string _ip, int _port)
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

    // Universal Methods --------------------------------------------------------------------------------------------------------------------------
    public void PrintLobbyStatus()
    {
        if (connectionType != ConnectionType.HOST) return;

        var AllPeers = Multiplayer.GetPeers();
        GD.Print($"Lobby Size: {AllPeers.Length + 1}-----------------------------------------");
        GD.Print($"HOST IP: {HostIP}, Port: {HostPort}");

        foreach (var PeerId in AllPeers)
        {
            ENetPacketPeer Player = Peer.GetPeer(PeerId);
            string PlayerIP = Player.GetRemoteAddress();
            int PlayerPort = Player.GetRemotePort();
            GD.Print($"Peer{PeerId} IP:{PlayerIP}, Port: {PlayerPort}");
        }
        GD.Print("---------------------------------------------------------------------------");
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

    public int GetPeerId()
    {
        return Peer.GetUniqueId();
    }
    
}

public class LobbyManager
{
    public void ConfigLobby()
    {
        NetworkManager.GlobalInstance.MessageReceived += (MessageType, MessageData, Sender) =>
        {
            switch (MessageType)
            {
                case NetworkManager.NetworkMessageType.SyncLobbyState:
                    OnReceiveLobbySyncData((Godot.Collections.Array)MessageData["LobbyState"]);
                    break;
                case NetworkManager.NetworkMessageType.RequestAddPlayer:
                    PlayerManager.GlobalInstance.AttemptAddRemotePlayer(Sender);
                    break;
                case NetworkManager.NetworkMessageType.PlayerAdded:
                    int PlayerNumber = (int)MessageData["PlayerNumber"];
                    int PeerId = NetworkManager.GlobalInstance.GetPeerId();
                    bool IsLocal = PeerId == (int)MessageData["PeerId"];
                    PlayerManager.GlobalInstance.OverrideAddPlayer(PlayerNumber, PeerId, IsLocal);
                    break;
            }
        };

        PlayerManager.GlobalInstance.PlayerAdded += (PlayerNumber) =>
        {
            if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
            {
                int NewPlayerPeerId = PlayerManager.GlobalInstance.AllPlayers[PlayerNumber].RemotePeerID;
                NotifyPlayerAdded(PlayerNumber, NewPlayerPeerId);
            }
        };

        NetworkManager.GlobalInstance.Multiplayer.PeerConnected += SendLobbySyncData;
    }
    public void SendLobbySyncData(long PeerId)
    {
        if (NetworkManager.GlobalInstance.connectionType != NetworkManager.ConnectionType.HOST) return;
        Godot.Collections.Array LobbyStateData = new Godot.Collections.Array(); 
        for (int i = 0; i < PlayerManager.MaxPlayerCount; i++)
        {
            PlayerProfile playerProfile = PlayerManager.GlobalInstance.AllPlayers[i];
            if (playerProfile == null)
            {
                LobbyStateData.Add(
                    new Dictionary
                    {
                        {"IsNull", true},
                    }
                );
                continue;
            }
            Dictionary PlayerDataDict = new Dictionary
            {
              {"PlayerNumber", i},
              {"PeerId", playerProfile.RemotePeerID},
              {"PlayerTag", playerProfile.playerTag}  
            };
            LobbyStateData.Add(PlayerDataDict);
        }
        Godot.Collections.Dictionary MessageData = new();
        MessageData["LobbyState"] = LobbyStateData;


        NetworkManager.GlobalInstance.SendMessageSpecificClient(NetworkManager.NetworkMessageType.SyncLobbyState, MessageData, (int)PeerId);
    }
    
    public void NotifyPlayerAdded(int PlayerNumber, int PeerId)
    {
        Godot.Collections.Dictionary MessageData = new Godot.Collections.Dictionary()
        {
          {"PlayerNumber", PlayerNumber}  ,
          {"PeerId", PeerId}
        };
        NetworkManager.GlobalInstance.SendMessage(NetworkManager.NetworkMessageType.PlayerAdded, MessageData);

    }

    // Message Responses


    public void OnReceiveLobbySyncData(Godot.Collections.Array PlayerData)
    {
        for (int i = 0; i < PlayerManager.MaxPlayerCount; i++)
        {
            Dictionary PlayerDataDict = (Dictionary)PlayerData[i];
            Godot.Variant IsNull;
            if (PlayerDataDict.TryGetValue("IsNull", out IsNull))
            {
                continue;
            }
            PlayerManager.GlobalInstance.OverrideAddPlayer(i, (int)PlayerDataDict["PeerId"], false);
        }
    }
}




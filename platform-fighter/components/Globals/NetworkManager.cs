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

    public enum FastNetworkMessageType
    {
        Input,

    }
    

    
    // Default Lobby Location/ Settings--------------------------------------------------------------------------------------------------------------------------
    public static int MAX_ROLLBACK_FRAMES = 50;

    public static int DefaultTestingPort = 8000;
    public static string DefaultTargetIp = "127.0.0.1";
    int MAX_CLIENTS = 2;

    public static int MESSAGE_CHANNEL_COUNT = 1;
    // Runtime Lobby Data --------------------------------------------------------------------------------------------------------------------------
    public LobbyManager lobbyManager;

    // Host Data
    int HostPort;
    string HostIP;
    // Signals --------------------------------------------------------------------------------------------------------------------------
    [Signal]
    public delegate void MessageReceivedEventHandler(int messageType, Godot.Collections.Dictionary MessageData, int Sender);    
    [Signal]
    public delegate void FastMessageReceivedEventHandler(int messageType, byte[] MessageData, int Sender);    
    
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

    public void SendFastMessage(FastNetworkMessageType messageType, byte[] MessageData, int TargetID)
    {
        Peer.SetTargetPeer(TargetID);
        Peer.TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable;
        Rpc("ReceiveFastMessage",(int) messageType, MessageData);
    }

    public void SendMessageSpecificClient(NetworkMessageType messageType, Godot.Collections.Dictionary MessageData, int ClientId)
    {
        Peer.SetTargetPeer(ClientId);
        Peer.TransferMode = MultiplayerPeer.TransferModeEnum.Reliable;
        RpcId(ClientId, "ReceiveMessage",(int) messageType, MessageData);
    }
    
    public void SendMessage(NetworkMessageType messageType, Godot.Collections.Dictionary MessageData)
    {
        // Peer.SetTargetPeer();
        Peer.TransferMode = MultiplayerPeer.TransferModeEnum.Reliable;
    
        Rpc("ReceiveMessage",(int) messageType, MessageData);
    }

    [Rpc(
    MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = false
    )]
    public void ReceiveMessage(int messageID, Godot.Collections.Dictionary MessageData)
    {
        int Sender = Multiplayer.GetRemoteSenderId();
        EmitSignal ("MessageReceived", messageID, MessageData, Sender);
    }
    [Rpc(
    MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = false
    )]
    public void ReceiveFastMessage(int messageID, byte[] MessageData)
    {
        int Sender = Multiplayer.GetRemoteSenderId();
        EmitSignal ("FastMessageReceived", messageID, MessageData, Sender);
    }

    // Host Methods --------------------------------------------------------------------------------------------------------------------------
    public void StartLobby( string Address, int Port)
    {
        Peer = new ENetMultiplayerPeer();

        var err = Peer.CreateServer(Port, MAX_CLIENTS, MESSAGE_CHANNEL_COUNT);
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
        var err = Peer.CreateClient(_ip, _port, MESSAGE_CHANNEL_COUNT);
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
            switch ((NetworkManager.NetworkMessageType)MessageType)
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



/*
Message I sent to the discord but may need to send to the forum!

Hello folks! I'm trying to use Godot enet packet peer for a multiplayer game and I'm encountering an issue.
I have a connection between 2 instances of the game on my computer and they can communicate and successfully make remote procedure calls (rpc's). I use this to configure "lobbies". During the match, I don't need remote function calls, I would like the smallest packets possible by sending the bytes directly. i am successfully sending packets via the function "Multiplayer.MultiplayerPeer.PutPacket". However, when the other instance receives the packet, it perceives it as a an rpc call and gives an error saying that the target node couldn't be found.

This is the method that works (sending data via remote function calls). 
```
    public void SendMessage(NetworkMessageType messageType, Godot.Collections.Dictionary MessageData)
    {
        Multiplayer.MultiplayerPeer.TransferChannel = NormalMessageChannel;
        Rpc("ReceiveMessage",(int) messageType, MessageData);
    }

    [Rpc(
    MultiplayerApi.RpcMode.AnyPeer,
    CallLocal = false
    )]
    public void ReceiveMessage(int messageID, Godot.Collections.Dictionary MessageData)
    {
        int Sender = Multiplayer.GetRemoteSenderId();
        EmitSignal ("MessageReceived", messageID, MessageData, Sender);
    }
```

Then i try to send raw bytes via this method 

```
 public void SendFastMessage(FastNetworkMessageType messageType, byte[] MessageData, int TargetID)
    {
        Multiplayer.MultiplayerPeer.SetTargetPeer(TargetID);
        Multiplayer.MultiplayerPeer.TransferChannel = FastMessageChannel;
        if (MessageData == null) 
        {
            MessageData = [];
        }
        byte[] Prepended = new byte[MessageData.Count() + 1];
        Prepended[0] = (byte)messageType;
        MessageData.CopyTo(Prepended,1);
        var err = Multiplayer.MultiplayerPeer.PutPacket(Prepended);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Packet send failed: {err}");
        }
        Multiplayer.MultiplayerPeer.SetTargetPeer(0);
        

    }
```

and receive with these methods, which I call everye frame:
```
public void GetAllPackets()
    {
        if (connectionType ==ConnectionType.DISCONNECTED) return;

        while (Multiplayer.MultiplayerPeer.GetAvailablePacketCount() > 0)
        {
            byte [] packet = Multiplayer.MultiplayerPeer.GetPacket();
            int channel = Multiplayer.MultiplayerPeer.GetPacketChannel();
            if (channel != FastMessageChannel)
            {
                return;
            }
            int SenderPeerId = Multiplayer.MultiplayerPeer.GetPacketPeer();
            HandlePacket(packet, SenderPeerId);
            
        }
    }


    public void HandlePacket(byte[] Packet, int SenderPeerId)
    {
        if (Packet.Count() < 1) return;

        NetworkMessageType messageType = (NetworkMessageType)Packet[0];
        byte[] ClippedMessageData = Packet[1..];

        EmitSignal("FastMessageReceived", (int)messageType, ClippedMessageData, SenderPeerId);
    }
```

When I send the packets, the instance receiving the packets gives these 2 errors:
```
E 0:00:16:716   get_cached_object: ID 45 not found in cache of peer 1.
  <C++ Error>   Parameter "recv_node" is null.
  <C++ Source>  modules/multiplayer/scene_cache_interface.cpp:280 @ get_cached_object()

```
and 
```
E 0:00:16:681   process_rpc: Invalid packet received. Requested node was not found.
  <C++ Error>   Parameter "node" is null.
  <C++ Source>  modules/multiplayer/scene_rpc_interface.cpp:208 @ process_rpc()
```
I poked at the documentation and a forum post or 2 and couldn't find anything!
I've tried using different "channels" for these calls but that didn't seem to help. Anyone have any ideas?

*/
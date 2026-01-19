using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node
{
    public List<PlayerProfile> AllPlayers = [];
    public static int MaxPlayerCount = 4;
    public static  PlayerManager GlobalInstance;

    public Queue<InputEvent> QueuedControllers= new();

    [Signal] public delegate void PlayerAddedEventHandler(int PlayerNumber);
    [Signal] public delegate void PlayerRemovedEventHandler(int PlayerNumber);

    public void QueueController(InputEvent inputEvent)
    {
        QueuedControllers.Enqueue(inputEvent);
    }
    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalInstance = this;
    }

    public override void _Ready()
    {
        for (int i = 0; i < MaxPlayerCount; i++)
        {
            AllPlayers.Add(null);
        }
        NetworkManager.GlobalInstance.PlayerAddedRequest += (RemotePlayerPeerID) => {
            AttemptAddRemotePlayer(RemotePlayerPeerID);
        };
        NetworkManager.GlobalInstance.PlayerAddedNotification += (PlayerNumber, PeerId, IsLocal) =>
        {
            OverrideAddPlayer(PlayerNumber, PeerId, IsLocal);
        };
    }


    
    // Not related to what im doing right now, but proccess is misspelled So fix this <- TODO 
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("debug_slow_game"))
        {
            foreach (var item in AllPlayers)
            {
                GD.Print(item);
            }
        }
    }

    // This function is for the network manager to add players from other systems.
    public void OverrideAddPlayer(int PlayerNumber, int PeerId, bool IsLocal)
    {
        int DeviceNumber = -1;
        PlayerProfile.ControllerTypes ControllerType = PlayerProfile.ControllerTypes.REMOTE_PLAYER;
        if (IsLocal)
        {
            // There should be no case where a player slot is overriden and it was already populated
            if (AllPlayers[PlayerNumber] != null)
            {
                GD.Print("something is wrong");
            }

            InputEvent QueuedController;
            bool Success = QueuedControllers.TryDequeue(out QueuedController);
            if (!Success)
            {
                GD.Print("Failed to find queued controller");
                throw new Exception("Player Added with no queued controlelr");
            }

            DeviceNumber = QueuedController.Device;
            ControllerType  = GetControllerType(QueuedController);
        } 
        

        PlayerProfile playerProfile = new();
        playerProfile.Configure(PlayerNumber, DeviceNumber, ControllerType, PeerId);
        AllPlayers[PlayerNumber] = playerProfile;
        EmitSignal("PlayerAdded", PlayerNumber);

    }

    // Method for host
    public void AttemptAddRemotePlayer(int RemotePlayerPeerID)
    {
        int PlayerNumber = GetFreePlayerNumber();
        if (PlayerNumber == -1) {
            GD.Print("no available player slots for remote player");
            // TODO: Tell the client that the slot wasn't available
            return;
        }
        PlayerProfile playerProfile = new();
        playerProfile.Configure(PlayerNumber, -1, PlayerProfile.ControllerTypes.REMOTE_PLAYER, RemotePlayerPeerID);

        AllPlayers[PlayerNumber] = playerProfile;
        EmitSignal("PlayerAdded", PlayerNumber);
    }

    public void AttemptAddPlayer(InputEvent input)
    {
        if (GetPlayerNumberFromInput(input ) != -1) return;
        int PlayerNumber = GetFreePlayerNumber();
        if (PlayerNumber == -1) return;


        PlayerProfile playerProfile = new();
        playerProfile.Configure(PlayerNumber, input.Device, GetControllerType(input));

        AllPlayers[PlayerNumber] = playerProfile;
        EmitSignal("PlayerAdded", PlayerNumber);
    }

     public void AttemptRemovePlayer(InputEvent input)
    {
        int PlayerNumber = GetPlayerNumberFromInput(input);
        GD.Print(PlayerNumber);
        if (PlayerNumber == -1) return;
        AllPlayers[PlayerNumber] = null;
        EmitSignal("PlayerRemoved", PlayerNumber);
    }


    // Outputs -1 if input not bound to player number;
    public int GetPlayerNumberFromInput(InputEvent input)
    { 
        for (int i = 0; i < MaxPlayerCount; i++)
        {
            PlayerProfile player = AllPlayers[i];
            if (
                player != null && 
                input.Device == player.InputDeviceNumber && 
                GetControllerType(input) == player.ControllerType
            )return i;
        }
        
        return -1;
    }

    public int GetFreePlayerNumber()
    {
         int PlayerNumber = -1;
        for (int i = 0 ; i < MaxPlayerCount; i++)
        {
            if ( AllPlayers[i] == null)
            {
                PlayerNumber = i;
                break;
            }
        }
        return PlayerNumber;
    }

    public PlayerProfile.ControllerTypes GetControllerType(InputEvent input)
    {
        if (input is InputEventJoypadButton || input is InputEventJoypadMotion)
        {
            return PlayerProfile.ControllerTypes.CONTROLLER;
        } else if (input is InputEventKey)
        {
            return PlayerProfile.ControllerTypes.KEYBOARD;
        } else
        {
            return PlayerProfile.ControllerTypes.UNACCEPTED_INPUT;
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node
{
    public List<PlayerProfile> AllPlayers = [];
    public static int MaxPlayerCount = 4;
    public static  PlayerManager GlobalInstance;
    [Signal] public delegate void PlayerAddedEventHandler(int PlayerNumber);
    [Signal] public delegate void PlayerRemovedEventHandler(int PlayerNumber);


    public override void _Ready()
    {
        GlobalInstance = this;
        for (int i = 0; i < MaxPlayerCount; i++)
        {
            AllPlayers.Add(null);
        }
    }

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


    public void AttemptAddPlayer(InputEvent input)
    {
        if (GetPlayerNumberFromInput(input ) != -1) return;
        int PlayerNumber = -1;
        for (int i = 0 ; i < MaxPlayerCount; i++)
        {
            if ( AllPlayers[i] == null)
            {
                PlayerNumber = i;
                break;
            }
        }
        // No valid player slots
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
            throw new ArgumentException("Unexpected Input Type Received");
        }
    }
}

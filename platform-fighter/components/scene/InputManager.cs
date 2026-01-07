using Godot;
using System;

public partial class InputManager : Node
{
    [Export] PlayerManager playerManager;
    public float DirftThreshold = .1f;

    public int KeyboardCharNum = 0;
    public ControllerState[][] AllControllerStates = [];
    public ControllerState[] CurrentControllerStates;
    public ControllerState[] FinalizedControllerState;
    
    [Signal] public delegate void ButtonEventEventHandler(ControllerState.ButtonTypes Button, int PlayerNumber, bool Pressed);

    
    public override void _Ready()
    {
        CurrentControllerStates = new ControllerState[PlayerManager.MaxPlayerCount];
        FinalizedControllerState = new ControllerState[PlayerManager.MaxPlayerCount];
        for (int i = 0; i < PlayerManager.MaxPlayerCount; i++)
        {
            CurrentControllerStates[i] = new();
            FinalizedControllerState[i] = new();
        }
    }


    public override void _Input(InputEvent @event)
    {
        PlayerProfile.ControllerTypes ControllerType;
        if (@event is InputEventKey)
        {
            ControllerType = PlayerProfile.ControllerTypes.KEYBOARD;
        }
        else if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)
        {
            ControllerType = PlayerProfile.ControllerTypes.CONTROLLER;
        } else return;
        

        int player_number = playerManager.GetPlayerNumberFromInput(@event);
        if (player_number == -1 ) return;

        PlayerProfile playerProfile = playerManager.AllPlayers[player_number];
        




    }


}

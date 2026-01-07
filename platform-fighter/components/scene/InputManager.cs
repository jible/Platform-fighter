using Godot;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;

public partial class InputManager : Node
{
    [Export] PlayerManager playerManager;
    public float DirftThreshold = .1f;

    public int KeyboardCharNum = 0;
    public ControllerState[][] AllControllerStates = [];
    public ControllerState[] CurrentControllerStates;
    public ControllerState[] FinalizedControllerState;
    
    public HashSet<ControllerState.LeftStickDirections> LeftStickDirections = new()
    {
        ControllerState.LeftStickDirections.LEFT_UP,
        ControllerState.LeftStickDirections.LEFT_DOWN,
        ControllerState.LeftStickDirections.LEFT_LEFT,
        ControllerState.LeftStickDirections.LEFT_RIGHT,
    };
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
        if (!(@event is InputEventKey || @event is InputEventJoypadButton || @event is InputEventJoypadMotion))
        {return;}
        

        int PlayerNumber = playerManager.GetPlayerNumberFromInput(@event);
        if (PlayerNumber == -1 ) return;

        PlayerProfile playerProfile = playerManager.AllPlayers[PlayerNumber];
        PlayerTag playerTag = playerProfile.playerTag; 

        if (@event is InputEventJoypadButton JoyEvent) HandleJoypadButton( JoyEvent, playerTag, PlayerNumber);
        else if (@event is InputEventJoypadMotion StickMotion) HandleJoypadMotion(StickMotion, PlayerNumber);
        else if (@event is InputEventKey KeyEvent) HandleKeyboardInput(KeyEvent, playerTag, PlayerNumber);

    }

    public void HandleJoypadButton( InputEventJoypadButton JoyEvent, PlayerTag playerTag, int player_number)
    {
        ControllerState.ButtonTypes Button = (ControllerState.ButtonTypes)playerTag.ReverseMap[JoyEvent.ButtonIndex];
        CurrentControllerStates[player_number].SetButton(Button, @JoyEvent.IsPressed());
    }

    public void HandleJoypadMotion(InputEventJoypadMotion StickMotion, int PlayerNumber)
    {
        JoyAxis Axis = StickMotion.Axis;
        if (Axis == JoyAxis.TriggerLeft || Axis == JoyAxis.TriggerRight)
        {
            return;
            // TODO: Add code for parsing triggers and treating them as normal input

        }

        // This is the case where it is actual stick handling

        float Magnitude = StickMotion.AxisValue;
        if (Math.Abs(Magnitude) < DirftThreshold)
        {
            Magnitude = 0;
        }

        int StickNumber = (int)Axis / 2;

        StickState Stick = CurrentControllerStates[PlayerNumber].StickStates[StickNumber];
        // This is a cheeky solution to check if the value changed on the x or y axis
        StickState.Axis StickAxis = ((int)Axis % 2 == 0) ? StickState.Axis.X : StickState.Axis.Y;
        Stick.SetAxis(StickAxis, Magnitude);
    }

    public void HandleKeyboardInput(InputEventKey KeyEvent, PlayerTag playerTag, int PlayerNumber)
    {
        Object Button;
        bool Success = playerTag.ReverseMap.TryGetValue(KeyEvent.Keycode, out Button);

        CurrentControllerStates[PlayerNumber].SetButton(Button, KeyEvent.IsPressed());
    }

    public void SerializeCurrentControllerState()
    {
        // If keyboard, build input vector
        
        
        for (int PlayerNumber = 0; PlayerNumber < FinalizedControllerState.Length; PlayerNumber++)
        {
            PlayerProfile playerProfile = playerManager.AllPlayers[PlayerNumber];
            if (playerProfile.ControllerType == PlayerProfile.ControllerTypes.KEYBOARD)
            {
                ApplyKeyboardDirectionInputs(playerProfile, PlayerNumber);
            }
            FinalizedControllerState[PlayerNumber] = CurrentControllerStates[PlayerNumber].GetCopy();
        }
    }
    Dictionary< ControllerState.LeftStickDirections, Vector2> InputToDirection = new()
    {
        {ControllerState.LeftStickDirections.LEFT_DOWN, Vector2.Down},
        {ControllerState.LeftStickDirections.LEFT_LEFT, Vector2.Left},
        {ControllerState.LeftStickDirections.LEFT_RIGHT, Vector2.Right},
        {ControllerState.LeftStickDirections.LEFT_UP, Vector2.Up},
    };


    public void ApplyKeyboardDirectionInputs(PlayerProfile playerProfile, int PlayerNumber)
    {
        Vector2 Direction = new();
        
        for (int j = 0; j < (int)ControllerState.LeftStickDirections.COUNT; j++)
        {
            foreach (var action in playerProfile.playerTag.ActionMap[j])
            {
                if (Input.IsKeyPressed((Key)action))
                {
                    Direction += InputToDirection[(ControllerState.LeftStickDirections)j];
                }
                
            }
        }
        CurrentControllerStates[PlayerNumber].StickStates[0].SetFromVector( Direction);
    }
    
}

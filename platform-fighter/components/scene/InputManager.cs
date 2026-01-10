using Godot;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;

public partial class InputManager : Node
{
    PlayerManager playerManager;
    [Export] TickManager tickManager;
    public float DriftThreshold = .1f;

    public ControllerState[][] AllControllerStates;
    public ControllerState[] CurrentControllerStates;
    
    [Signal] public delegate void ButtonEventEventHandler(ControllerState.ButtonTypes Button, int PlayerNumber, bool Pressed);


    Dictionary< ControllerState.ButtonTypes, Vector2> LeftStickInputToDirection = new()
    {
        {ControllerState.ButtonTypes.LEFT_DOWN, Vector2.Down},
        {ControllerState.ButtonTypes.LEFT_LEFT, Vector2.Left},
        {ControllerState.ButtonTypes.LEFT_RIGHT, Vector2.Right},
        {ControllerState.ButtonTypes.LEFT_UP, Vector2.Up},
    };
    Dictionary< ControllerState.ButtonTypes, Vector2> RightStickInputToDirection = new()
    {
        {ControllerState.ButtonTypes.RIGHT_DOWN, Vector2.Down},
        {ControllerState.ButtonTypes.RIGHT_LEFT, Vector2.Left},
        {ControllerState.ButtonTypes.RIGHT_RIGHT, Vector2.Right},
        {ControllerState.ButtonTypes.RIGHT_UP, Vector2.Up},
    };
    
    public override void _Ready()
    {
        playerManager = PlayerManager.GlobalInstance;
        CurrentControllerStates = new ControllerState[PlayerManager.MaxPlayerCount];
        AllControllerStates = new ControllerState[RollbackManager.MAX_ROLLBACK_FRAMES][];
        for (int Frame = 0; Frame <RollbackManager.MAX_ROLLBACK_FRAMES; Frame++)
        {
            AllControllerStates[Frame] = new ControllerState[PlayerManager.MaxPlayerCount];
            for (int PlayerNumber = 0; PlayerNumber < PlayerManager.MaxPlayerCount; PlayerNumber++)
            {
                AllControllerStates[Frame][PlayerNumber] = new();
            }
        }
        for (int PlayerNumber = 0; PlayerNumber < PlayerManager.MaxPlayerCount; PlayerNumber++)
        {
            CurrentControllerStates[PlayerNumber] = new();
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

    public ControllerState[] GetInputsForTick(int Tick)
    {
        ControllerState[] Inputs = AllControllerStates[Tick];
        return Inputs;
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
        if (Math.Abs(Magnitude) < DriftThreshold)
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
        ControllerState.ButtonTypes Button;
        bool Success = playerTag.ReverseMap.TryGetValue(KeyEvent.Keycode, out Button);
        if (!Success) return;
        CurrentControllerStates[PlayerNumber].SetButton(Button, KeyEvent.IsPressed());
    }

    public void SerializeCurrentControllerState(int Frame)
    {
        for (int PlayerNumber = 0; PlayerNumber < CurrentControllerStates.Length; PlayerNumber++)
        {
            PlayerProfile playerProfile = playerManager.AllPlayers[PlayerNumber];
            // If keyboard, build input vector
            if (playerProfile == null) continue;
            if (playerProfile.ControllerType == PlayerProfile.ControllerTypes.KEYBOARD)
            {
                ApplyKeyboardDirectionInputs(playerProfile, PlayerNumber);
            }


            AllControllerStates[Frame][PlayerNumber] = CurrentControllerStates[PlayerNumber].GetCopy();
        }
        
    }

    public void ApplyKeyboardDirectionInputs(PlayerProfile playerProfile, int PlayerNumber)
    {
        Vector2 LeftStickDirection = new();
        Vector2 RightStickDirection = new();
        
        for (int StickInputs = (int)ControllerState.ButtonTypes.SERIALIZABLE_END + 1; StickInputs < (int)ControllerState.ButtonTypes.COUNT; StickInputs++)
        {
            foreach (var action in playerProfile.playerTag.ActionMap[(ControllerState.ButtonTypes)StickInputs])
            {
                Vector2 DirectionToAdd;
                if (!Input.IsKeyPressed((Key)action)) { continue; }
                
                bool LeftContains = LeftStickInputToDirection.TryGetValue(  (ControllerState.ButtonTypes)StickInputs, out DirectionToAdd);
                if (LeftContains) LeftStickDirection += DirectionToAdd * StickState.MAX_STICK_AXIS_VALUE;
                bool RightContains = RightStickInputToDirection.TryGetValue(  (ControllerState.ButtonTypes)StickInputs, out DirectionToAdd);
                if (RightContains)RightStickDirection  += DirectionToAdd * StickState.MAX_STICK_AXIS_VALUE;
                
            }
        }
        CurrentControllerStates[PlayerNumber].StickStates[0].SetFromVector( LeftStickDirection);
        CurrentControllerStates[PlayerNumber].StickStates[1].SetFromVector( RightStickDirection);

    }
    

    public bool PollForInput(ControllerState.ButtonTypes Button, int PlayerNumber)
    {
        // Not gonna put safety checks over this since it 
        // should only error here when player number management is wrong.
        return AllControllerStates[tickManager.GetStateKey(tickManager.GetCurrentTick() ) ][PlayerNumber].GetButton(Button);
    }

    public DM_Vector2 PollForStickState(int StickNumber, int PlayerNumber)
    {
        GD.Print(StickNumber, PlayerNumber, tickManager.GetCurrentTick());
        ControllerState State = AllControllerStates[tickManager.GetStateKey(tickManager.GetCurrentTick() ) ][PlayerNumber];
        StickState StickState =  State.StickStates[StickNumber];
        return StickState.ToRangedVector();
    }
}

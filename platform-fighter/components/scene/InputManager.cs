using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;

public partial class InputManager : Node
{
    PlayerManager playerManager;
    [Export] TickManager tickManager;
    public float DriftThreshold = .1f;
    // This needs a better name: Its how many frames of inputs you send over the network each time you send a packet
    const int FramesOfInputsToSend = 5;
    public const int PacketBytesPerPlayer = FramesOfInputsToSend * ControllerState.EncodedSize; 

    // Stores an int of the earliest frame you should rollback to if you need to rollback
    public int? RollbackTargetFrame = null;
    public ControllerState[][] AllControllerStates;
    public ControllerState[] CurrentControllerStates;
    public Queue<byte[]> RemoteInputQueue = new();
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
        AllControllerStates = new ControllerState[NetworkManager.MAX_ROLLBACK_FRAMES][];
        for (int Frame = 0; Frame <NetworkManager.MAX_ROLLBACK_FRAMES; Frame++)
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

        NetworkManager.GlobalInstance.FastMessageReceived += (MessageType, MessageData, Sender) =>
        {
            if ((NetworkManager.FastNetworkMessageType) MessageType == NetworkManager.FastNetworkMessageType.Input)
            {
                ApplyPeerInput(MessageData, Sender);
            }
        };
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
        ControllerState.ButtonTypes Button; 
        bool Success = playerTag.ReverseMap.TryGetValue(JoyEvent.ButtonIndex, out Button);
        if (!Success) return;
        CurrentControllerStates[player_number].SetButton(Button, JoyEvent.IsPressed());
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
        GD.Print(Stick.ToRangedVector().ToStandardVector());
    }

    public void HandleKeyboardInput(InputEventKey KeyEvent, PlayerTag playerTag, int PlayerNumber)
    {

        ControllerState.ButtonTypes Button;
        bool Success = playerTag.ReverseMap.TryGetValue(KeyEvent.Keycode, out Button);
        if (!Success) return;
        if ((int) Button > (int) ControllerState.ButtonTypes.SERIALIZABLE_END) return;
        CurrentControllerStates[PlayerNumber].SetButton(Button, KeyEvent.IsPressed());
    }

    public void HandleStickInputs(PlayerProfile playerProfile, int PlayerNumber)
    {
        float lX = Input.GetJoyAxis(playerProfile.InputDeviceNumber, JoyAxis.LeftX);
        float ly = Input.GetJoyAxis(playerProfile.InputDeviceNumber, JoyAxis.LeftY);
        CurrentControllerStates[PlayerNumber].StickStates[0].SetFromVector(new Vector2(lX,ly));

        float rx = Input.GetJoyAxis(playerProfile.InputDeviceNumber, JoyAxis.LeftX);
        float ry = Input.GetJoyAxis(playerProfile.InputDeviceNumber, JoyAxis.LeftY);
        CurrentControllerStates[PlayerNumber].StickStates[1].SetFromVector(new Vector2(rx,ry));
    }

    public void SerializeCurrentControllerState(int Tick)
    {
        int TickKey = tickManager.GetStateKey(Tick);
        for (int PlayerNumber = 0; PlayerNumber < CurrentControllerStates.Length; PlayerNumber++)
        {
            PlayerProfile playerProfile = playerManager.AllPlayers[PlayerNumber];
            // If keyboard, build input vector
            if (playerProfile == null) continue;
            if (playerProfile.ControllerType == PlayerProfile.ControllerTypes.KEYBOARD)
            {
                ApplyKeyboardDirectionInputs(playerProfile, PlayerNumber);
            } else if (playerProfile.ControllerType == PlayerProfile.ControllerTypes.CONTROLLER)
            {
                HandleStickInputs(playerProfile, PlayerNumber);
            }

            AllControllerStates[TickKey][PlayerNumber] = CurrentControllerStates[PlayerNumber].GetCopy();
        }

        // Once the current controller states have been serialized, send them to the other players
        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.DISCONNECTED) return;
        // As of now, just send the message on the host. in the future it may be better to change this to wait
        // Until client packets have been received, but honestly its probably fine just sending them since 
        // the controller array stores whatever it thinks is right at this moment.
        int TargetId = (int)((NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST) ? MultiplayerPeer.TargetPeerBroadcast : MultiplayerPeer.TargetPeerServer);
        NetworkManager.GlobalInstance.SendFastMessage(NetworkManager.FastNetworkMessageType.Input, EncodeInputs(Tick), TargetId);
    }

    public byte[] EncodeInputs( int Tick )
    {
        int StartFrame = Tick - FramesOfInputsToSend;
        int OutputWalker = 0;

        List<int> PlayersToEncode = [];
        for ( int j = 0; j < PlayerManager.MaxPlayerCount; j++)
        {
            PlayerProfile CurrentPlayer = playerManager.AllPlayers[j];
            if (CurrentPlayer == null) continue;

            // The host should encode all players
            if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
            {
                PlayersToEncode.Add(j);
                continue;
            }

            // The client should only encode local players (players with a registered controller on this system )
            if ( CurrentPlayer.InputDeviceNumber == -1) continue;
            PlayersToEncode.Add(j);
        }

        int PacketSize = PlayersToEncode.Count * PacketBytesPerPlayer;

        byte[] Output = new byte[PacketSize + 4];


        // Bytes representing the Starting frame number

        BinaryPrimitives.WriteUInt32LittleEndian(Output, (UInt32) StartFrame);
        OutputWalker += 4;
        for (int Frame = StartFrame; Frame < Tick; Frame++)
        {
            foreach (var PlayerNumber in PlayersToEncode) 
            {
                byte[] controllerState = AllControllerStates[tickManager.GetStateKey(Frame)][PlayerNumber].GetEncoded();
                controllerState.CopyTo(Output, OutputWalker);
                OutputWalker += ControllerState.EncodedSize;
            }
        }
        return Output;
    }

    public void ApplyPeerInput(byte[] InputData, int Sender)
    {
        // This method is parsing the last "FramesOfInputs" inputs from another machine and 
        // it will apply those inputs to whatever players aren't on this machine


        // Input Data Structure:

        /*
        
        4 bytes [Starting Frame]

        Repeat "FramesOfInputsManayTimes{
            Repeat for each player on the machine that sent the message{
                [3 Bytes of controller data]

            }            

        }
        */

        // Decode the starting frame. It is the first 4 bytes of the message, making a uint32
        int StartFrame = (int)BinaryPrimitives.ReadUInt32LittleEndian(InputData[..4]);

        // find what controllers you are listening for by itterating through 
        // all players and seeing which ones correspond to the given Sender ID 
        // keep a list of those player numbers
        List<int> RemotePlayersSent = [];
        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
        {
            foreach (var player in PlayerManager.GlobalInstance.AllPlayers)
            {
                if (player == null) continue;
                if (player.RemotePeerID == Sender)
                {
                    RemotePlayersSent.Add(player.PlayerNumber);
                }
            }
        }

        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.CLIENT)
        {
            foreach (var player in PlayerManager.GlobalInstance.AllPlayers)
            {
                if (player == null) continue;
                // The host broadcasts all active players, so just add all players.
                RemotePlayersSent.Add(player.PlayerNumber);
            }
        }


        int MaxBytes = RemotePlayersSent.Count * PacketBytesPerPlayer; 
        // Check if there are fewer bytes than expected
        if (MaxBytes != RemotePlayersSent.Count * ControllerState.EncodedSize * FramesOfInputsToSend)
        {
            GD.PushError("Received Input Data that does not fit format");
        }

        int EndFrame = StartFrame + FramesOfInputsToSend - 1;
        int ByteIndex = 4; // This skips the first 4 bits since those were used to parse the frame number.
        for (int frame = StartFrame; frame <= EndFrame ; frame += 1)
        {
            int FrameIndex = tickManager.GetStateKey(frame);
            foreach (int PlayerNumber in RemotePlayersSent)
            {
                byte[] ControllerStateBytes = InputData[ByteIndex .. (ByteIndex+ControllerState.EncodedSize)]; // assuming the end is not inclusive
                ByteIndex += ControllerState.EncodedSize;

                // Check if there is a diff
                ControllerState DecodedState = ControllerState.FromEncoded(ControllerStateBytes);


                if (! AllControllerStates[FrameIndex][PlayerNumber].Equals( DecodedState))
                {
                    if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
                    {
                        GD.Print("diff");
                        GD.Print("Local:  " + BitConverter.ToString(AllControllerStates[FrameIndex][PlayerNumber].GetEncoded()));
                        GD.Print("Remote: " + BitConverter.ToString(ControllerStateBytes));
                    }
                    
                    // If there is, record it to rememeber to rollback
                    if  (RollbackTargetFrame == null || frame < RollbackTargetFrame)
                    {
                        RollbackTargetFrame = frame;
                    }
                    AllControllerStates[FrameIndex][PlayerNumber] = DecodedState;

                }
            }
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
                Vector2 LeftDir;
                Vector2 RightDir;
                if (!Input.IsKeyPressed((Key)action)) { continue; }
                bool LeftContains = LeftStickInputToDirection.TryGetValue(  (ControllerState.ButtonTypes)StickInputs, out LeftDir);
                if (LeftContains) LeftStickDirection += LeftDir;
                bool RightContains = RightStickInputToDirection.TryGetValue(  (ControllerState.ButtonTypes)StickInputs, out RightDir);
                if (RightContains)RightStickDirection  += RightDir;
            }
        }
        CurrentControllerStates[PlayerNumber].StickStates[0].SetFromVector( LeftStickDirection.Normalized());
        CurrentControllerStates[PlayerNumber].StickStates[1].SetFromVector( RightStickDirection.Normalized());
    }
    

    public bool PollForInput(ControllerState.ButtonTypes Button, int PlayerNumber)
    {
        // Not gonna put safety checks over this since it 
        // should only error here when player number management is wrong.
        return AllControllerStates[tickManager.GetStateKey(tickManager.GetCurrentTick() ) ][PlayerNumber].GetButton(Button);
    }

    public DM_Vector2 PollForStickState(int StickNumber, int PlayerNumber)
    {
        ControllerState State = AllControllerStates[tickManager.GetStateKey(tickManager.GetCurrentTick() ) ][PlayerNumber];
        StickState StickState =  State.StickStates[StickNumber];
        return StickState.ToVector();
    }
}

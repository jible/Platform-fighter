using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text.RegularExpressions;

// Collects every player's input once per tick and keeps it for rollback and the network.
// Local players are polled through their tag's binds: keyboard players read the keyboard, controller players read their own device.
// Remote players come in through the network instead, and are predicted until they arrive
public partial class InputManager : Node
{
    PlayerManager playerManager;
    // Set by whatever builds the match, since the tick manager is a plain C# object and can't be exported
    public TickManager tickManager;
    // This needs a better name: Its how many frames of inputs you send over the network each time you send a packet
    const int FramesOfInputsToSend = 5;
    public const int PacketBytesPerPlayer = FramesOfInputsToSend * ControllerState.EncodedSize;

    // Stores an int of the earliest frame you should rollback to if you need to rollback
    public int? RollbackTargetFrame = null;
    public ControllerState[][] AllControllerStates;
    public int?[] LatestReceivedFrame;
    public Queue<byte[]> RemoteInputQueue = new();

    // The actions that are buttons, and the button each sets
    private static readonly (GameAction Action, ControllerState.ButtonTypes Button)[] ButtonActions =
    {
        (GameAction.Jump, ControllerState.ButtonTypes.JUMP),
        (GameAction.Light, ControllerState.ButtonTypes.LIGHT),
        (GameAction.Special, ControllerState.ButtonTypes.SPECIAL),
        (GameAction.Grab, ControllerState.ButtonTypes.GRAB),
    };

    public override void _Ready()
    {
        playerManager = PlayerManager.GlobalInstance;
        AllControllerStates = new ControllerState[NetworkManager.MAX_ROLLBACK_FRAMES][];
        for (int Frame = 0; Frame <NetworkManager.MAX_ROLLBACK_FRAMES; Frame++)
        {
            AllControllerStates[Frame] = new ControllerState[PlayerManager.MaxPlayerCount];
            for (int PlayerNumber = 0; PlayerNumber < PlayerManager.MaxPlayerCount; PlayerNumber++)
            {
                AllControllerStates[Frame][PlayerNumber] = new();
            }
        }
        LatestReceivedFrame = new int?[PlayerManager.MaxPlayerCount];
        for (int PlayerNumber = 0; PlayerNumber < PlayerManager.MaxPlayerCount; PlayerNumber++)
        {
            LatestReceivedFrame[PlayerNumber] = null;
        }

        NetworkManager.GlobalInstance.FastMessageReceived += (MessageType, MessageData, Sender) =>
        {
            if ((NetworkManager.FastNetworkMessageType) MessageType == NetworkManager.FastNetworkMessageType.Input)
            {
                ApplyPeerInput(MessageData, Sender);
            }
        };
    }

    public int? GetLatestConfirmedFrame()
    {
        int? output = null;
        foreach (int? Frame in LatestReceivedFrame)
        {
            if (Frame != null && (output == null || output < Frame))
            {
                output = Frame;
            }
        }
        return output;
    }

    // Polls every local player into this tick's saved input. Remote players are left alone,
    // since their input comes from the network and may already be here for this tick
    public void SerializeCurrentControllerState(int Tick)
    {
        int TickKey = TickManager.GetStateKey(Tick);
        for (int PlayerNumber = 0; PlayerNumber < PlayerManager.MaxPlayerCount; PlayerNumber++)
        {
            PlayerProfile playerProfile = playerManager.AllPlayers[PlayerNumber];
            if (playerProfile == null) continue;
            InputTag tag = TagManager.Get(playerProfile.TagId);
            if (playerProfile.ControllerType == PlayerProfile.ControllerTypes.KEYBOARD)
            {
                AllControllerStates[TickKey][PlayerNumber] = PollKeyboard(tag);
            } else if (playerProfile.ControllerType == PlayerProfile.ControllerTypes.CONTROLLER)
            {
                AllControllerStates[TickKey][PlayerNumber] = PollController(tag, playerProfile.InputDeviceNumber);
            }
        }

        // Once the current controller states have been serialized, send them to the other players
        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.DISCONNECTED) return;
        // As of now, just send the message on the host. in the future it may be better to change this to wait
        // Until client packets have been received, but honestly its probably fine just sending them since 
        // the controller array stores whatever it thinks is right at this moment.
        int TargetId = (int)((NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST) ? MultiplayerPeer.TargetPeerBroadcast : MultiplayerPeer.TargetPeerServer);
        NetworkManager.GlobalInstance.SendFastMessage(NetworkManager.FastNetworkMessageType.Input, EncodeInputs(Tick), TargetId);
    }

    // Keyboard players only read their tag's keys, since Godot treats every keyboard as one device.
    // Two players can share a keyboard as long as their tags bind different keys
    private static ControllerState PollKeyboard(InputTag tag)
    {
        return Poll(tag, bind => bind.Type == BindType.Key && Input.IsPhysicalKeyPressed((Key)bind.Code) ? 1 : 0);
    }

    // Controller players only read their own device's buttons and sticks
    private static ControllerState PollController(InputTag tag, int device)
    {
        return Poll(tag, bind => bind.Type switch
        {
            BindType.JoyButton => Input.IsJoyButtonPressed(device, (JoyButton)bind.Code) ? 1 : 0,
            BindType.JoyAxis => AxisStrength(Input.GetJoyAxis(device, (JoyAxis)bind.Code) * bind.Direction, tag.Deadzone),
            _ => 0,
        });
    }

    // Builds a tick's input from how strongly each of the tag's binds is held, from 0 to 1.
    // The sticks come from the move and aim actions, so keys, d-pads and sticks all drive them the same way.
    // Up is +y, matching the simulation: up minus down, so Godot's screen space y never gets in
    private static ControllerState Poll(InputTag tag, Func<Bind, float> strength)
    {
        var state = new ControllerState();
        // Can't happen while deleted tags fall back to a default, but reads as nothing held if it does
        if (tag == null) return state;

        foreach (var (action, button) in ButtonActions)
        {
            state.SetButton(button, ActionStrength(tag, action, strength) > 0);
        }
        state.LeftStick.SetFromVector(StickVector(tag, GameAction.MoveUp, GameAction.MoveDown, GameAction.MoveLeft, GameAction.MoveRight, strength));
        state.RightStick.SetFromVector(StickVector(tag, GameAction.AimUp, GameAction.AimDown, GameAction.AimLeft, GameAction.AimRight, strength));
        return state;
    }

    // The strongest of the action's binds
    private static float ActionStrength(InputTag tag, GameAction action, Func<Bind, float> strength)
    {
        float strongest = 0;
        foreach (Bind bind in tag.BindsFor(action))
        {
            if (bind.Code == Bind.InvalidCode) continue;
            strongest = Math.Max(strongest, strength(bind));
        }
        return strongest;
    }

    private static Vector2 StickVector(InputTag tag, GameAction up, GameAction down, GameAction left, GameAction right, Func<Bind, float> strength)
    {
        var stick = new Vector2(
            ActionStrength(tag, right, strength) - ActionStrength(tag, left, strength),
            ActionStrength(tag, up, strength) - ActionStrength(tag, down, strength));
        // Two keys at once would reach past the edge of a real stick
        return stick.LengthSquared() > 1 ? stick.Normalized() : stick;
    }

    // How far the axis is pushed in the bind's direction past the deadzone, rescaled so the deadzone's edge is 0
    private static float AxisStrength(float pushed, float deadzone)
    {
        return pushed <= deadzone ? 0 : Math.Min(1, (pushed - deadzone) / (1 - deadzone));
    }

    public void PredictRemoteInputs(int frame)
    {
        int frameIndex = TickManager.GetStateKey(frame);
        ControllerState[] CurrentStates = AllControllerStates[frameIndex];

        int PrevFrameIndex = TickManager.GetStateKey(frame - 1);
        ControllerState[] PreviousStates = AllControllerStates[PrevFrameIndex];

        for (int PlayerNumber = 0 ; PlayerNumber < PlayerManager.MaxPlayerCount; PlayerNumber++)
        {
            PlayerProfile CurrentPlayer = playerManager.AllPlayers[PlayerNumber];
            if (CurrentPlayer == null)continue;
            if (CurrentPlayer.InputDeviceNumber != -1 )continue;
            // If player exists and is remote:
            if (LatestReceivedFrame[PlayerNumber] == null || LatestReceivedFrame[PlayerNumber] < frame)
            {
                // If you haven't received the input for this frame, predict it ( copy the previous state)
                CurrentStates[PlayerNumber].CopyFromState(PreviousStates[PlayerNumber]);
            }
        }
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
                byte[] controllerState = AllControllerStates[TickManager.GetStateKey(Frame)][PlayerNumber].GetEncoded();
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
            int FrameIndex = TickManager.GetStateKey(frame);
            foreach (int PlayerNumber in RemotePlayersSent)
            {
                LatestReceivedFrame[PlayerNumber] = frame;
                if (playerManager.AllPlayers[PlayerNumber].InputDeviceNumber != -1)
                {
                    // If the player is local, do not read what comes from other player. Just keep walking along the bytes
                    ByteIndex += ControllerState.EncodedSize;
                    continue;
                }
                byte[] ControllerStateBytes = InputData[ByteIndex .. (ByteIndex+ControllerState.EncodedSize)]; // assuming the end is not inclusive
                ByteIndex += ControllerState.EncodedSize;

                // Check if there is a diff
                ControllerState DecodedState = ControllerState.FromEncoded(ControllerStateBytes);


                if (! AllControllerStates[FrameIndex][PlayerNumber].Equals( DecodedState))
                {
                    
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


    // TicksAgo reads an earlier tick's saved input, like the tick before for a just pressed check
    public bool PollForInput(ControllerState.ButtonTypes Button, int PlayerNumber, int TicksAgo = 0)
    {
        // Not gonna put safety checks over this since it
        // should only error here when player number management is wrong.
        return AllControllerStates[TickManager.GetStateKey(tickManager.CurrentTick - TicksAgo) ][PlayerNumber].GetButton(Button);
    }

    public DMVector PollForStickState(int StickNumber, int PlayerNumber)
    {
        ControllerState State = AllControllerStates[TickManager.GetStateKey(tickManager.CurrentTick) ][PlayerNumber];
        if (StickNumber == (int)ControllerState.StickTypes.LEFT)
        {
            return State.LeftStick.ToVector();
        }else
        {
            return State.RightStick.ToVector();
        }
    }
}

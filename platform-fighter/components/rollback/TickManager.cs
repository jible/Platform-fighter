using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

// Turns real time into ticks and handles rollback, while what a tick actually does is the step handed in when it's built.
// Rolls back when the input manager gets remote inputs for a tick that already ran, and checks for desyncs
// by sending hashes of the rollback state from the client to the host.
// Hooks into the network manager, so Dispose it when the match ends
public class TickManager : IDisposable
{
    // Ticks of rollback state kept. Tick numbers wrap around this, see GetStateKey
    public static int MaxTicks => NetworkManager.MAX_ROLLBACK_FRAMES;
    // Gameplay values are per second, this converts them to per tick
    public const int TicksPerSecond = 60;
    private const double TickDuration = 1.0 / TicksPerSecond;
    // Most ticks one frame will run to catch up on real time
    private const int MaxTicksPerFrame = 5;
    // Real time not yet spent on a tick
    private double timeSinceLastTick;

    public int CurrentTick { get; private set; }
    // True while resimulating ticks that already ran, so display code can skip them
    public bool IsRollingBack { get; private set; }

    private readonly InputManager inputManager;
    private readonly Action step;
    // Hashes the client sent for confirmed ticks, checked against the host's own on the next tick
    private readonly List<byte[]> peerGameHashes = new();
    private readonly NetworkManager.FastMessageReceivedEventHandler onFastMessage;

    public TickManager(InputManager inputManager, Action step)
    {
        this.inputManager = inputManager;
        this.step = step;
        onFastMessage = (messageType, messageData, sender) =>
        {
            if (messageType != (int)NetworkManager.FastNetworkMessageType.ShowHash) return;
            peerGameHashes.Add(messageData);
        };
        NetworkManager.GlobalInstance.FastMessageReceived += onFastMessage;
    }

    public void Dispose()
    {
        NetworkManager.GlobalInstance.FastMessageReceived -= onFastMessage;
    }

    // Index into the rollback buffers for a tick. Handles negative ticks, like the one before tick 0
    public static int GetStateKey(int tick)
    {
        int key = tick % MaxTicks;
        return key < 0 ? key + MaxTicks : key;
    }

    // The simulation runs TicksPerSecond ticks per second of real time, whatever the frame rate,
    // so it plays at the same speed on every machine. A fast frame can run no ticks, a slow one several.
    // Returns how many ticks ran
    public int Advance(double deltaTime)
    {
        timeSinceLastTick += deltaTime;
        int ticksThisFrame = 0;
        while (timeSinceLastTick >= TickDuration && ticksThisFrame < MaxTicksPerFrame)
        {
            timeSinceLastTick -= TickDuration;
            ticksThisFrame++;
            Tick();
        }
        // Time past the cap is dropped, so after a hitch the game slows down for a moment instead of
        // running a burst of ticks that makes the next frame slower still
        if (timeSinceLastTick >= TickDuration) timeSinceLastTick = 0;

        return ticksThisFrame;
    }

    private void Tick()
    {
        // Remote inputs arrived for a tick that already ran, so redo everything from there
        if (inputManager.RollbackTargetFrame is int rollbackTarget && rollbackTarget < CurrentTick)
        {
            Resimulate(rollbackTarget, CurrentTick - 1);
            inputManager.RollbackTargetFrame = null;
        }

        inputManager.SerializeCurrentControllerState(CurrentTick);
        if (NetworkManager.GlobalInstance.connectionType != NetworkManager.ConnectionType.DISCONNECTED)
        {
            inputManager.PredictRemoteInputs(CurrentTick);
        }

        SerializableDataManager.SaveAll(GetStateKey(CurrentTick));
        step();

        int? safeTick = inputManager.GetLatestConfirmedFrame();
        if (safeTick != null)
        {
            if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.CLIENT)
            {
                SendSafeHash((int)safeTick);
            }
            else if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
            {
                CompareHashes();
            }
        }

        CurrentTick += 1;
    }

    // Loads the state saved at startTick and runs every tick up to and including endTick again
    private void Resimulate(int startTick, int endTick)
    {
        if (startTick > endTick)
        {
            GD.PushError("Cannot resimulate ticks in negative order");
        }

        IsRollingBack = true;
        SerializableDataManager.LoadAll(GetStateKey(startTick));
        // Ends on endTick + 1, the tick that was about to run
        for (CurrentTick = startTick; CurrentTick <= endTick; CurrentTick++)
        {
            inputManager.PredictRemoteInputs(CurrentTick);
            SerializableDataManager.SaveAll(GetStateKey(CurrentTick));
            step();
        }
        IsRollingBack = false;
    }

    // The hash of a tick both machines have every input for, so it should match the host's
    private void SendSafeHash(int targetTick)
    {
        int gameHash = GetGameHash(targetTick);

        int targetId = (int)MultiplayerPeer.TargetPeerServer;
        byte[] gameBytes = new byte[4 + 4]; // 4 for the tick and 4 for the hash
        BinaryPrimitives.WriteUInt32LittleEndian(gameBytes.AsSpan(0, 4), (uint)targetTick);
        BinaryPrimitives.WriteUInt32LittleEndian(gameBytes.AsSpan(4, 4), (uint)gameHash);

        NetworkManager.GlobalInstance.SendFastMessage(NetworkManager.FastNetworkMessageType.ShowHash, gameBytes, targetId);
    }

    private void CompareHashes()
    {
        foreach (byte[] peerGameHashBytes in peerGameHashes)
        {
            int frame = (int)BinaryPrimitives.ReadUInt32LittleEndian(peerGameHashBytes.AsSpan(0, 4));
            int peerGameHash = (int)BinaryPrimitives.ReadUInt32LittleEndian(peerGameHashBytes.AsSpan(4, 4));

            if (peerGameHash != GetGameHash(frame))
            {
                GD.PrintRich("Game Hash mismatch at frame: ", frame, DebugPrintColors.ErrorColor);
            }
        }
        peerGameHashes.Clear();
    }

    public int GetGameHash(int tick)
    {
        return SerializableDataManager.HashAll(GetStateKey(tick));
    }
}

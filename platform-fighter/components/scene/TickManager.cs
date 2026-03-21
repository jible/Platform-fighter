using Godot;
using Godot.NativeInterop;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

public partial class TickManager : Node
{
    /*
    This manager is in charge of progressing the game tick by tick. 
    It notifies the play manager when a tick has passed and 
    */
    public bool IsRollingBack = false;
    int CurrentTick = 0;
    public int GetCurrentTick()
    {
        return CurrentTick;
    }

    ISerializable[] RollbackObjects = [];
    Dictionary<ISerializable, ISaveHandler> SaveHandlers = [];

    [Export] PlayManager playManager;
    [Export] InputManager inputManager;
    [Export] CharacterHolder characterHolder;
    bool ContinuePlay = true;
    public List<Byte[]> PeerGameHashes = [];
    public int[] LocalGameHashes;

    public override void _Ready()
    {
        // listen for fast message signal
        LocalGameHashes = new int[NetworkManager.MAX_ROLLBACK_FRAMES];

        NetworkManager.GlobalInstance.FastMessageReceived += (MessageType, MessageData, Sender) =>
        {
            if (MessageType != (int)NetworkManager.FastNetworkMessageType.ShowHash) return;
            PeerGameHashes.Add(MessageData);

        };
    }


    // This must be called once all objects have been instanced
    public void PrepForRollback()
    {
        RollbackObjects = CollectSerializableNodes(playManager);
        foreach (var rollbackObject in RollbackObjects)
        {
            Type objectType = rollbackObject.GetType();
            Type stateType = objectType.GetNestedType("SerializedState");
            if (objectType == null)
            {
                GD.Print("This shouldn't happen but testing!");                
            }
            if (stateType == null)
            {
                GD.Print(objectType, " Does not have 'SerializedState' subclass for saving");
            }
            Type HandlerType = typeof(SaveHandler<,>).MakeGenericType(objectType,stateType);
            ISaveHandler handler = (ISaveHandler)Activator.CreateInstance(HandlerType, rollbackObject);
            SaveHandlers[rollbackObject] = handler;
        }
        // Populate current tick
        return;
    }

    public int GetStateKey(int frame)
    {
        if (frame < 0)
        {
            frame += NetworkManager.MAX_ROLLBACK_FRAMES;
        }
        return frame % NetworkManager.MAX_ROLLBACK_FRAMES;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!ContinuePlay) return;
        Tick();
    }

    public void Tick()
    {
        if (dp_physics_server.GlobalInstance == null || dp_shape_renderer_3d.GlobalInstance == null)
        {return;}

        if (dp_physics_server.GlobalInstance == null || dp_shape_renderer_3d.GlobalInstance == null)
        {
            GD.Print("Physics engine and renderer not ready");
        }
        
        // Serialize the current tick
        int CurrentStateKey = GetStateKey(CurrentTick);




        // Before handling the current tick, check if you need to rollback
        if (inputManager.RollbackTargetFrame != null && (int)inputManager.RollbackTargetFrame < CurrentTick)
        {
            GD.Print("rolling back to ", inputManager.RollbackTargetFrame);
            int LatestTick = CurrentTick;
            IsRollingBack = true;

            Resimulate((int)inputManager.RollbackTargetFrame, CurrentTick - 1);
            CurrentTick = LatestTick;
            IsRollingBack = false;
            inputManager.RollbackTargetFrame = null;
        }

        inputManager.SerializeCurrentControllerState(CurrentTick);
        if (NetworkManager.GlobalInstance.connectionType != NetworkManager.ConnectionType.DISCONNECTED)
        {
            inputManager.PredictRemoteInputs(CurrentTick);
        }

        SerializeCurrentTick(CurrentStateKey);
        ComparHashes(CurrentStateKey);
        CallProcesses();

        CurrentTick += 1;
    }

    public void ComparHashes(int CurrentStateKey)
    {
        int GameHash = GetGameHash(CurrentTick); 
        // if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
        // {
        //     GD.Print("Frame: " , CurrentTick, " Host Game Hash: ", GameHash);
        // }
        // if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.CLIENT)
        // {
        //     GD.Print("Frame: " , CurrentTick, " Client Game Hash: ", GameHash);
            
        // }

        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.HOST)
        {   
            LocalGameHashes [CurrentStateKey] = GameHash;


            // Handle State hashes here!
            foreach (byte[] PeerGameHashBytes in PeerGameHashes)
            {
                int Frame = (int)BinaryPrimitives.ReadUInt32LittleEndian(PeerGameHashBytes.AsSpan(0,4));
                int PeerGameHash = (int)BinaryPrimitives.ReadUInt32LittleEndian(PeerGameHashBytes.AsSpan(4,4));

                int SentStateKey = GetStateKey(Frame);
                if (PeerGameHash != LocalGameHashes[SentStateKey])
                {
                    GD.Print("Game Hash mismatch at frame: ", Frame);
                }

            }
            PeerGameHashes.Clear();
            
        }
        if (NetworkManager.GlobalInstance.connectionType == NetworkManager.ConnectionType.CLIENT)
        {
            int TargetId = (int)MultiplayerPeer.TargetPeerServer;
            byte[] GameBytes = new byte[4 + 4]; // 4 for the tick and 4 for the hash 
            BinaryPrimitives.WriteUInt32LittleEndian(GameBytes.AsSpan(0,4), (UInt32) CurrentTick);
            BinaryPrimitives.WriteUInt32LittleEndian(GameBytes.AsSpan(4,4), (UInt32) GameHash);
            
            NetworkManager.GlobalInstance.SendFastMessage(NetworkManager.FastNetworkMessageType.ShowHash, GameBytes, TargetId);
        }
    }



    public int GetGameHash(int Tick)
    {
        int CurrentTickKey = GetStateKey(Tick);

        

        List<int> hashes = [];
        foreach( ISerializable serializable in RollbackObjects)
        {
            hashes.Add(SaveHandlers[serializable].GetHash(CurrentTickKey));
        }
        return DeterministicCombineHashes(hashes.ToArray());

    
    }

    public void Resimulate(int StartTick, int EndTick)
    {
        if (StartTick > EndTick)
        {
            GD.PushError("Cannot resimulate ticks in negative order");
        }
        
        // Load the world state back to the starting tick
        int startStateKey = GetStateKey(StartTick);
        LoadTick(startStateKey);

        CurrentTick = StartTick;

        for ( ; CurrentTick <= EndTick; CurrentTick ++)
        {
            int CurrentStateKey = GetStateKey(CurrentTick);
            inputManager.PredictRemoteInputs(CurrentTick);

            SerializeCurrentTick(CurrentStateKey);
            CallProcesses();
        }
    }

    public void SerializeCurrentTick(int CurrentTickKey)
    {
        foreach( ISerializable serializable in RollbackObjects)
        {
            SaveHandlers[serializable].Save(CurrentTickKey);
        }
    }

    public void CallProcesses()
    {
        // Call Seperate cause the play manager isn't marked tickable
        playManager.Tick();
        PropogateTick(playManager);
    }

    public void LoadTick(int TickKey)
    {
        foreach( ISerializable serializable in RollbackObjects)
        {
            SaveHandlers[serializable].Load(TickKey);
        }
    }

    // Propogates methods to objects with the tick interface
    public static void PropogateTick(Node node)
    {
        if (node == null) return;
        if (node is ITickable tickable)
        {
            tickable.Tick();
        }
        foreach (var child in node.GetChildren())
        {
            PropogateTick(child);
        }
    }

    // Method that gets a list of all nodes that are serializable.
    public ISerializable[] CollectSerializableNodes(Node Root)
    {
        var Collector = new ObjectTypeCollector<ISerializable>();
        return Collector.CollectTargetNodes(Root);
    }

    // Method that gets a list of all nodes that are serializable.
    public ITickable[] CollectTickableNodes(Node Root)
    {
        var Collector = new ObjectTypeCollector<ITickable>();
        return Collector.CollectTargetNodes(Root);
    }

    public static int DeterministicCombineHashes(params int[] Hashes)
    {
        uint hash = 2166136261u;
        foreach (int i  in Hashes)
        {
            hash = (uint)((hash ^ i) * 16777619u);
        }
        return (int)hash;
    }
}


   
// Helper Class for getting objects of a given type
public class ObjectTypeCollector<TargetType>
    {
        public TargetType[] CollectTargetNodes(Node Root)
        {
            List<TargetType> Output = new();
            PropogateCollecting(Root, Output);
            return Output.OrderBy(n=> n.GetType().Name).ThenBy(n=> n.GetHashCode()).ToArray();
        }
        // Helper method for getting all serializable nodes
        private void PropogateCollecting(Node Parent, List<TargetType> Output)
        {
            if (Parent == null) return;
            if (Parent is TargetType serializable)
            {
                Output.Add(serializable);
            }
            foreach (var child in Parent.GetChildren())
            {
                PropogateCollecting(child, Output);
            }
        }
    }
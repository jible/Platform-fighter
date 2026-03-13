using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
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


    // This must be called once all objects have been instanced
    public void PrepForRollback()
    {
        RollbackObjects = CollectSerializableNodes(playManager).ToArray();
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
        inputManager.SerializeCurrentControllerState(CurrentTick);

        // Before handling the current tick, check if you need to rollback
        if (inputManager.RollbackTargetFrame != null)
        {
            int LatestTick = CurrentTick;
            IsRollingBack = true;
            Resimulate((int)inputManager.RollbackTargetFrame, CurrentTick - 1);
            CurrentTick = LatestTick;
            IsRollingBack = false;
            inputManager.RollbackTargetFrame = null;
        }
        
        // Dispatch Inputs + Call processes
        
        SerializeCurrentTick(CurrentStateKey);
        CallProcesses();

        
        CurrentTick += 1;
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
            SerializeCurrentTick(CurrentStateKey);
            CallProcesses();
            GD.Print("in da loop");
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
    public List<ISerializable> CollectSerializableNodes(Node Root)
    {
        var Collector = new ObjectTypeCollector<ISerializable>();
        return Collector.CollectTargetNodes(Root);
    }

    // Method that gets a list of all nodes that are serializable.
    public List<ITickable> CollectTickableNodes(Node Root)
    {
        var Collector = new ObjectTypeCollector<ITickable>();
        return Collector.CollectTargetNodes(Root);
    }
}


   
// Helper Class for getting objects of a given type
public class ObjectTypeCollector<TargetType>
    {
        public List<TargetType> CollectTargetNodes(Node Root)
        {
            List<TargetType> Output = new();
            PropogateCollecting(Root, Output);
            return Output;
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
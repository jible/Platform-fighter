using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    ISerializable[] RollbackObjects;

    [Export] PlayManager playManager;
    [Export] InputManager inputManager;
    [Export] CharacterHolder characterHolder;
    bool ContinuePlay = true;

    public override void _Ready()
    {
        RollbackObjects = CollectSerializableNodes(playManager).ToArray();
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
        inputManager.SerializeCurrentControllerState(CurrentStateKey);
        SerializeCurrentTick(CurrentStateKey);

        // Before handling the current tick, check if you need to rollback
        if (inputManager.RollbackTargetFrame != null)
        {
            int LatestTick = CurrentTick;
            IsRollingBack = true;
            Resimulate((int)inputManager.RollbackTargetFrame, CurrentTick - 1);
            CurrentTick = LatestTick;
            IsRollingBack = false;
        }      

        // Dispatch Inputs + Call processes
        CallProcesses();
        CurrentTick += 1;
    }

    public void Resimulate(int StartTick, int EndTick)
    {
        // Load the world state back to the starting tick
        LoadTick(StartTick);

        for ( ; CurrentTick <= EndTick; CurrentTick ++)
        {
            int CurrentStateKey = GetStateKey(CurrentTick);
            if (CurrentTick != StartTick)  SerializeCurrentTick(CurrentStateKey);
            
        }
    }


    public void SerializeCurrentTick(int CurrentTickKey)
    {
        foreach( ISerializable serializable in RollbackObjects)
        {
            serializable.saveHandler.Save(CurrentTickKey);
        }
    }


    public void CallProcesses()
    {
        // Call Seperate cause the play manager isn't marked tickable
        playManager.Tick();
        PropogateTick(playManager);
    }

    public void LoadTick(int Tick)
    {
        foreach( ISerializable serializable in RollbackObjects)
        {
            serializable.saveHandler.Save(Tick);
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
        List<ISerializable> Output = new();
        PropogateSerialziableCollecting(Root, Output);
        return Output;
    }
    // Helper method for getting all serializable nodes
    public void PropogateSerialziableCollecting(Node Parent, List<ISerializable> Output)
    {
        if (Parent == null) return;
        if (Parent is ISerializable serializable)
        {
            Output.Add(serializable);
        }
        foreach (var child in Parent.GetChildren())
        {
            PropogateSerialziableCollecting(child, Output);
        }
    }
}

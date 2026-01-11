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

    int CurrentTick = 0;
    public int GetCurrentTick()
    {
        return CurrentTick;
    }

    Dictionary<Node, Dictionary<String, object>>[] States;

    [Export] PlayManager playManager;
    [Export] InputManager inputManager;
    [Export] CharacterHolder characterHolder;
    bool ContinuePlay = true;

    public override void _Ready()
    {
        States = new Dictionary<Node, Dictionary<string, object>>[ RollbackManager.MAX_ROLLBACK_FRAMES];
        for ( int i = 0; i < RollbackManager.MAX_ROLLBACK_FRAMES; i++)
        {
            States[i] = [];
        }
        // Populate current tick
        return;
    }

    public int GetStateKey(int frame)
    {
        if (frame < 0)
        {
            frame += RollbackManager.MAX_ROLLBACK_FRAMES;
        }
        return frame % RollbackManager.MAX_ROLLBACK_FRAMES;

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

        SerializeCurrentTick(CurrentStateKey);
        // Dispatch Inputs + Call processes
        CallProcesses(CurrentStateKey);
        CurrentTick += 1;
    }


    public void SerializeCurrentTick(int CurrentTickKey)
    {
        inputManager.SerializeCurrentControllerState(CurrentTickKey);

        Dictionary<Node,Dictionary<String, object>> CurrentTickStates = [];
        PropogateSerialize(playManager, CurrentTickStates);
        States[CurrentTickKey] = CurrentTickStates;
    }

    public void CallProcesses(int CurrentTickKey)
    {
        inputManager.SerializeCurrentControllerState(CurrentTickKey );
        playManager.Tick();
        PropogateTick(playManager);
    }

    public void SimulateMultipleTicks(int StartTick, int EndTick)
    {
        if (EndTick < StartTick)
        {
            throw new ArgumentException("Cannot simulate ticks in negative order");
        }

        LoadTick(StartTick);

        for (int i = StartTick ; i < EndTick; i++)
        {
            Tick();
        }
    }

    public void LoadTick(int Tick)
    {
        PropogateLoadState(playManager, States[Tick]);
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

    public static void PropogateSerialize(Node node, Dictionary<Node,Dictionary<String, object>> SerializationData)
    {
        if (node == null) return;
        if (node is ISerializable serializable)
        {SerializationData[node] = serializable.SerializeState();}

        foreach (var child in node.GetChildren())
        {PropogateSerialize(child, SerializationData);}
    }

    public static void PropogateLoadState(Node node, Dictionary<Node,Dictionary<String, object>> SerializationData)
    {
        if (node == null) return;
        if (node is ISerializable serializable)
        {
            serializable.LoadState(SerializationData[node]);
        }
        foreach (var child in node.GetChildren())
        {
            PropogateLoadState(child, SerializationData);
        }
    }
}

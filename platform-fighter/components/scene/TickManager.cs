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
	[Export] int MaxRollbackTicks = 50;

    int CurrentTick = 0;


    List<Dictionary<Node, Dictionary<String, object>>> States = [];

    [Export] PlayManager playManager;
    [Export] InputManager inputManager;
    [Export] CharacterHolder characterHolder;
    bool ContinuePlay = true;

    public override void _Ready()
    {
        for ( int i = 0; i < MaxRollbackTicks; i++)
        {
            // Find a proper means to store game state
            States.Append([]);
        }
        // Populate current tick
        return;
    }

    public int GetStateKey(int frame)
    {
        return frame % MaxRollbackTicks;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!ContinuePlay) return;
        if (dp_physics_server.GlobalInstance == null || dp_shape_renderer_3d.GlobalInstance == null)
        {return;}

        // Serialize the current tick
        int CurrentStateKey = GetStateKey(CurrentTick);
        int PreviousStateKey = GetStateKey(CurrentTick - 1);

        // Itterate through all nodes in this scene.
        // If they have a method for Serializing their state, call it
        Dictionary<Node,Dictionary<String, object>> CurrentTickStates = [];
        PropogateSerialize(playManager, CurrentTickStates);
        States[GetStateKey(CurrentTick)] = CurrentTickStates;
        playManager.Tick();
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
        if (node is ITickable tickable)
        {SerializationData[node] = tickable.SerializeState();}

        foreach (var child in node.GetChildren())
        {PropogateSerialize(child, SerializationData);}
    }

    public static void PropogateLoadState(Node node, Dictionary<Node,Dictionary<String, object>> SerializationData)
    {
        if (node == null) return;
        if (node is ITickable tickable)
        {
            tickable.LoadState(SerializationData[node]);
        }
        foreach (var child in node.GetChildren())
        {
            PropogateLoadState(child, SerializationData);
        }
    }

}

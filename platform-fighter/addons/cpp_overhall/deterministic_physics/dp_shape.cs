using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[Tool]
public partial class DP_Shape : Node
{
    // World Space
    DM_Vector2 position;
    DM_Vector2[] positions = [];
    [Export]
    public Vector2 editor_position
    {
        get { return position.ToStandardVector(); }
        set { position = new DM_Vector2(value); }
    }


    // Collision Data
    [Export]
    public DP_ShapeType ShapeData;
    [Export]
    public int layer_collision = 0;
    [Export]
    public int mask_collision = 0;
    [Export]
    public bool is_trigger = false;
    [Export]
    public bool is_static = false;
    [Export]
    public Color color = new Color();

    // Trigger overlap handling
    Dictionary<DP_Shape, bool>[] overlaps = [];


    // Position buffer
    static int max_physics_rollback = 50;
    static int MaxDataBufferSize = max_physics_rollback + 1;
    DM_Vector2[] position_buffer = [];

    public override void _Ready()
    {
        // Fill overlaps and position buffer with 
        System.Array.Resize(ref overlaps, MaxDataBufferSize);
        System.Array.Resize(ref position_buffer, MaxDataBufferSize);
        for (int i = 0; i < MaxDataBufferSize; i++)
        {
            position_buffer[i] = new DM_Vector2();
            overlaps[i] = new Dictionary<DP_Shape, bool>();
        }
    }

    // Overlap Detection
    public bool check_overlap(DP_Shape other)
    {
        switch (ShapeData)
        {
            case DP_Circle:
                detect_circle_overlap(other);
                break;
            default:
                break;
        }
        return false;
    }

    public bool detect_circle_overlap(DP_Shape other)
    {
        switch (other.ShapeData)
        {
            case DP_Circle:
                break;
            case DP_Rectangle:
                break;
        }
        return false;
    }




    public void PopulateCurrentFrame()
    {
        // overlaps[GetCurrentBufferPosition()] =
        positions[GetCurrentBufferPosition()] = position;
    }


    // Helper for getting key of position and overlap
    public static ulong GetCurrentBufferPosition() { return GetBufferPositionAt(Godot.Engine.GetPhysicsFrames()); }
    public static ulong GetBufferPositionAt(ulong frame){return frame % (ulong)MaxDataBufferSize;}
    

}

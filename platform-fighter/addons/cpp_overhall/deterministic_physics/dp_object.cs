using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[Tool]
[GlobalClass]
public partial class dp_object : Node
{
    // Rollback Data
    Dictionary<String, Object>[] RollbackData;

    // Collision Data
    [Export]public bool is_active = true;
    private dp_shape _shape;
    [Export]public dp_shape Shape
    {
        get => _shape;
        set
        {
            // When you set a new shape, automatically set the shape's object reference to this.
            _shape = value;
            if (_shape != null)
            {
                _shape.PhysicsObject = this;
            }
        }
    }
    [Export]public int layer_collision = 0;
    [Export]public int mask_collision = 0;
    [Export]public bool is_trigger = false;
    [Export]public bool is_static = false;
    [Export]public Color color = new Color((float)0.188, (float)0.569, (float)0.341, (float)0.773);

    // Trigger overlap handling
    Dictionary<dp_object, bool>[] overlaps = [];


    
    public void PopulateCurrentFrame()
    {
        RollbackData[GetCurrentBufferPosition()] = Shape.ExtractData();
    }
    
    
    // Helper for getting key of position and overlap
    public static ulong GetCurrentBufferPosition() { return GetBufferPositionAt(Godot.Engine.GetPhysicsFrames()); }
    public static ulong GetBufferPositionAt(ulong frame) { return frame % (ulong)MaxDataBufferSize; }
    
        // Position buffer
    static int max_physics_rollback = 50;
    static int MaxDataBufferSize = max_physics_rollback + 1;
    DM_Vector2[] position_buffer = [];


    public override void _Ready()
    {
        // Fill overlaps and position buffer with 
        overlaps = new Dictionary<dp_object, bool>[MaxDataBufferSize];
        RollbackData = new Dictionary<String, Object>[MaxDataBufferSize];
        for (int i = 0; i < MaxDataBufferSize; i++)
        {
            RollbackData[i] = new Dictionary<string, object>();
            overlaps[i] = new Dictionary<dp_object, bool>();
        }
    }

    // Overlap Detection
    public bool check_overlap(dp_object other)
    {
        switch (Shape)
        {
            case dp_circle:
                detect_circle_overlap(other);
                break;
            default:
                break;
        }
        return false;
    }

    public bool detect_circle_overlap(dp_object other)
    {
        switch (other.Shape)
        {
            case dp_circle:
                break;
            case dp_rectangle:
                break;
        }
        return false;
    }
}

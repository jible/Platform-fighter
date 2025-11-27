using Godot;
using System;

public partial class dm_vector_editor_wrapper : GodotObject
{
    [Export] public dm64_editor_wrapper x;
    [Export] public dm64_editor_wrapper y;

     // Extractors:
    public Vector2 ToStandardVector()
    {
        // NOT SURE IF THIS SHOULD OUTPUT TO LONGS OR FLOATS. 
        // TODO: TEST CASES FOR CONVERTING TO VECTOR AND NEEDING DETERMINSM
        
        return new Vector2(x.ToFloat(), y.ToFloat());
    }
}

using Godot;
using System;
using System.Collections.Generic;

// This is a tool cause it prevents errors in the editor when it is exported
[Tool]
[GlobalClass]
public partial class dp_shape : Resource
{
    // World Space
    public DM_Vector2 Position = new DM_Vector2();
    [Export]
    public Vector2 editor_position
    {
        get { return Position.ToStandardVector(); }
        set { Position = new DM_Vector2(value); }
    }

    // Owner
    public dp_object PhysicsObject;
    // Extend this method in extension classes and add to this array if you need to add more properties.
    public virtual Dictionary<String, object> ExtractData()
    {
        return new Dictionary<String, object> { { "position", Position.copy() } };
    }


    public virtual void LoadData(Dictionary<String, object> Data)
    {
        Position = ((DM_Vector2)Data["position"]).copy();
    }
}

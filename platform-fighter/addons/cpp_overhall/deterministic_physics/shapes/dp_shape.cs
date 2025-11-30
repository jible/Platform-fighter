using Godot;
using System;
using System.Collections.Generic;

// This is a tool cause it prevents errors in the editor when it is exported
[Tool]
public partial class dp_shape : Resource
{
    // World Space
    [Export] public DM_Vector2 Position = new DM_Vector2();

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

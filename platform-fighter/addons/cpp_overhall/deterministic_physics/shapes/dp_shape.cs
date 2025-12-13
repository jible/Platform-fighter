using Godot;
using System;
using System.Collections.Generic;

// This is a tool cause it prevents errors in the editor when it is exported
[Tool]
[GlobalClass]
public abstract partial class dp_shape : Resource
{
    // Owner
    public dp_object PhysicsObject;
    // Extend this method in extension classes and add to this array if you need to add more properties.
    public abstract Dictionary<String, object> ExtractData();

    public abstract DM64 GetMaxSize();

    public abstract void LoadData(Dictionary<String, object> Data);
}

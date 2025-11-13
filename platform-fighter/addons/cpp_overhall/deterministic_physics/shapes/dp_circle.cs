using Godot;
using System;

[Tool]
[GlobalClass]
public partial class dp_circle : dp_shape
{
    [Export]public float editor_radius
    {
        get { return radius.ToFloat(); }
        set { radius = new DM64(value); }
    }
    public DM64 radius = new DM64();
    
    
}


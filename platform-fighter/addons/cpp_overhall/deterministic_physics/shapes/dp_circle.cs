using Godot;
using System;

[Tool]
[GlobalClass]
public partial class dp_circle : dp_shape
{
    [Export]public dm64_editor_wrapper editor_radius;
    public dm64 radius = new dm64(50);
    
    
}


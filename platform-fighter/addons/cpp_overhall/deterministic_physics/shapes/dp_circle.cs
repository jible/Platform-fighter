using Godot;
using System;

[Tool]
[GlobalClass]
public partial class dp_circle : dp_shape
{
    [Export] public float EditorRadius = 50;
    public DM64 Radius = new(50);
    
    
}
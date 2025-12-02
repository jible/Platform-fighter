using Godot;
using System;

[GlobalClass]
[Tool]
public partial class dp_rectangle : dp_shape
{
    [Export] public Vector2 EditorSize = new(50,50);
    public DM_Vector2 Size = new(50,50);
}

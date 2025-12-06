using Godot;
using System;
using System.Numerics;

public partial class ShapeRenderTest : Node3D
{
    [Export] public dp_object moveable;
    public override void _Process(double delta)
    {
        var inputDir = Input.GetVector("debug_left", "debug_right", "debug_down", "debug_up");
        moveable.Shape.Position += new DM_Vector2((inputDir/ 7));
    }

}

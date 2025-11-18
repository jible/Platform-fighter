using Godot;
using System;

public partial class ShapeRendererTesting : Node2D
{
    
    [Export] dp_object shape;
    public override void _PhysicsProcess(double delta)
    {
        if (shape == null) return;
        if (Input.IsActionPressed("debug_up")){
            shape.Shape.Position.y -= 10;
        }
        if (Input.IsActionPressed("debug_down")){
            shape.Shape.Position.y += 10;
        }
        if (Input.IsActionPressed("debug_left")){
            shape.Shape.Position.x -= 10;
        }
        if (Input.IsActionPressed("debug_right")){
            shape.Shape.Position.x += 10;
        }
    }
}

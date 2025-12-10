using Godot;
using System;

[GlobalClass]
[Tool]
public partial class dp_shape_follower : Node3D
{
    [Export] dp_object Target;
    [Export] float ZIndex = 0f;
    public override void _Process(double delta)
    {
        if (Target == null) return;
        dp_shape shape = Target.Shape;
        if (shape == null) return;
        if (Engine.IsEditorHint()) Position = VectorUp(shape.EditorPosition, ZIndex);
        else Position = VectorUp(shape.Position.ToStandardVector(), ZIndex);
    }

    private Vector3 VectorUp(Vector2 v, float z)
    {
        return new (v.X, v.Y, z);
    }

}

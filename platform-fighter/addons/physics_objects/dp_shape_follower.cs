using Godot;
using System;

[GlobalClass]
[Tool]
public partial class dp_shape_follower : Node3D
{
	[Export] dp_object Target;
	[Export] float ZIndex = 0f;
	public void UpdatePosition()
	{
		if (Target == null) return;
		dp_shape shape = Target.Shape;
		if (shape == null) return;
		if (Engine.IsEditorHint()) Position = VectorUp(Target.EditorGlobalPosition, ZIndex);
		else Position = VectorUp(Target.GlobalPosition.ToStandardVector(), ZIndex);
	}

	private Vector3 VectorUp(Vector2 v, float z)
	{
		return new (v.X, v.Y, z);
	}

    public override void _Ready()
    {
        dp_shape_renderer_3d.GlobalInstance.RegisterFollower(this);
    }

}

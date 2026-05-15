using Godot;
using System;

[GlobalClass]
[Tool]
public partial class DP_ShapeFollower : Node3D
{
	DP_Object Target;
	[Export] float ZIndex = 0f;
	public void UpdatePosition()
	{
		if (Target == null)
		{
			Node Parent = GetParent();
			if ( Parent is DP_Object casted)Target = casted;
			else return;
		}
		DP_Shape shape = Target.Shape;
		if (shape == null) return;
		Position = VectorUp(Target.GlobalPosition.ToStandardVector(), ZIndex);
	}

	private Vector3 VectorUp(Vector2 v, float z)
	{
		return new (v.X, v.Y, z);
	}

	public override void _Ready()
	{
		DP_ShapeRenderer3D renderer = DP_ShapeRenderer3D.GlobalInstance;
		if ( renderer == null) { return;}
		renderer.RegisterFollower(this);
	}

}

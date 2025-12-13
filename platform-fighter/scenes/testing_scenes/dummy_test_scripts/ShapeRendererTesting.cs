using Godot;
using System;

public partial class ShapeRendererTesting : Node2D
{
	
	[Export] dp_object target;
	public override void _PhysicsProcess(double delta)
	{
		if (target == null) return;
		if (Input.IsActionPressed("debug_up")){
			target.SetPositionX(target.Position.y - 10);
		}
		if (Input.IsActionPressed("debug_down")){
			target.SetPositionX(target.Position.y + 10);
		}
		if (Input.IsActionPressed("debug_left")){
			target.SetPositionX(target.Position.x - 10);
		}
		if (Input.IsActionPressed("debug_right")){
			target.SetPositionX(target.Position.x + 10);
		}
	}
}

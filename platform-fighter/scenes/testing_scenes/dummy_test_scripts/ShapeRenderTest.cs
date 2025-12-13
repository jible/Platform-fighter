using Godot;
using System;
using System.Numerics;

public partial class ShapeRenderTest : Node3D
{
	[Export] public dp_object moveable;

	public override void _Ready()
	{
		
		moveable.ObjectEntered += overlapped;
		moveable.ObjectExited += exited;
	}

	private void overlapped(dp_object other)
	{
		GD.Print("entered");

	}
	private void exited(dp_object other)
	{
		GD.Print("exited");

	}

	public override void _Process(double delta)
	{
		var inputDir = Input.GetVector("debug_left", "debug_right", "debug_down", "debug_up");
		moveable.Position += new DM_Vector2((inputDir/ 7));
	}

 

}

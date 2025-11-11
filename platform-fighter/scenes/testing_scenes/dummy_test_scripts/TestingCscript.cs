using Godot;
using Godot.Bridge;
using System;

public partial class TestingCscript : Node2D
{
	public override void _Ready()
	{
		Fix64 a = new Fix64(99.9999f);

		Fix64 b = new Fix64(516.561f);
		GD.Print((a).Sqrt());
	}
}

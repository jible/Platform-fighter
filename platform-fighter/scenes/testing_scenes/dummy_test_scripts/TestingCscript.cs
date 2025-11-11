using Godot;
using Godot.Bridge;
using System;

public partial class TestingCscript : Node2D
{
	public override void _Ready()
	{
		Fix64 a = new Fix64(1024);

		Fix64 b = new Fix64(32);

		// GD.Print((a / b).ToFloat());
		GD.Print( a.Sqrt().ToFloat() );
		
	}
}

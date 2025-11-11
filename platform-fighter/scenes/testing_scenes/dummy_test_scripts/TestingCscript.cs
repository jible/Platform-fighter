using Godot;
using Godot.Bridge;
using System;

public partial class TestingCscript : Node2D
{
	public override void _Ready()
	{
		DM64 a = new DM64(1024);

		DM64 b = new DM64(32);

		// GD.Print((a / b).ToFloat());
		// GD.Print( a.Sqrt().ToFloat() );
		DM_Vector2 c = new DM_Vector2(a, b);
		GD.Print("Expected: ", new Vector2(1024, 32), " Received: ", c.ToStandardVector());

		GD.Print("Expected: ", new Vector2(1024, 32).Normalized(), " Received: ", c.ToStandardVector().Normalized());

		
	}
}

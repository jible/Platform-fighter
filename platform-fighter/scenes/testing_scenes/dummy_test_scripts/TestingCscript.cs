using Godot;
using Godot.Bridge;
using System;

public partial class TestingCscript : Node2D
{
	public override void _Ready()
	{
		DM_Vector2.UnitTest();
	}
}

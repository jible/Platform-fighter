using Godot;
using System;
using System.ComponentModel;

[GlobalClass]
public partial class dp_player_body : dp_object
{
	DM_Vector2 Velocity = new();
	// Shadow these properties so they aren't exported to the editor

	public new bool is_static;
	public new bool is_trigger;
	public override void _Ready()
	{
		base.is_static = false;
		base.is_trigger = false;
		base._Ready();
	}
	public void PhysicsStep()
	{
		Position += Velocity;
	}

	public bool IsGrounded()
	{
		
		return true;
	}
}

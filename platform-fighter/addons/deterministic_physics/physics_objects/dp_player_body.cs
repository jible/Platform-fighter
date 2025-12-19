using Godot;
using System;
using System.ComponentModel;

[GlobalClass]
[Tool]
public partial class dp_player_body : dp_object
{
	DM_Vector2 Velocity = new();
	// Shadow these properties so they aren't exported to the editor

	// public void PhysicsStep()
	// {
	// 	Position += Velocity;
	// }

	// public bool IsGrounded()
	// {
		
	// 	return true;
	// }
}

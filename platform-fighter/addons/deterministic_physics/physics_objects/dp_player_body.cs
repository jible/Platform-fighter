using Godot;
using System;
using System.ComponentModel;

[GlobalClass]
[Tool]
public partial class dp_player_body : dp_object, ITickable
{
	public DM_Vector2 Velocity = new();
	[Export] InputHandler inputHandler;
	[Export] float EditorAcceleration = new();
	DM64 Acceleration = new(.0025f);

	[Export] float EditorMaxVelocity = new();
	DM64 MaxVelocity = new();

	public void Tick()
	{
		DM_Vector2 dirVector = inputHandler.PollForStickState(0);
		GD.Print(dirVector.ToStandardVector());
		Velocity += Acceleration * dirVector;
		Position += Velocity;
	} 

	// public bool IsGrounded()
	// {
		
	// 	return true;
	// }
}

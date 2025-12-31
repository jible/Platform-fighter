using Godot;
using System;
using System.ComponentModel;

[GlobalClass]
[Tool]
public partial class dp_player_body : dp_object
{
	public DM_Vector2 Velocity = new();
	
    [Export] float EditorAcceleration = new();
    DM64 Acceleration;

    [Export] float EditorMaxVelocity = new();
    DM64 MaxVelocity = new();

    public void Tick()
    {
        Velocity += Acceleration * new DM_Vector2(1,1);
		Position += Velocity;
    } 

	// public bool IsGrounded()
	// {
		
	// 	return true;
	// }
}

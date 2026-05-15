using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.Arm;
using System.Xml;

[GlobalClass]
[Tool]
public partial class dp_player_body : dp_object, ITickable, ISerializable
{
	public DM_Vector Velocity = new();
	[Export] InputHandler inputHandler;
	[Export] public string EditorAcceleration = "0.0025";
	public DM64 Acceleration = new();


	public DM64 Gravity = new();

	
	public DM64 MaxVelocity = new();

    public ISaveHandler MakeSaveHandler()
    {
        return new SaveHandler<dp_player_body, SerializedState> (this);
    }

    public void Tick()
	{
		DM_Vector dirVector = inputHandler.PollForStickState(0);
		dirVector.y = dirVector.y * -1;
		Velocity += Acceleration * dirVector;
		Velocity.y -= Gravity;
		Position = Position + Velocity;

	}

    public class SerializedState : ISerializedState<dp_player_body>
	{
		public DM_Vector Velocity;
		public DM_Vector Position;

        public void Load(dp_player_body Owner)
        {
            Owner.Velocity = Velocity;
			Owner.Position = Position;
        }

        public void Save(dp_player_body Owner)
        {
            Position = Owner.Position;
			Velocity = Owner.Velocity;
        }
		public int GetHash()
		{
			return TickManager.DeterministicCombineHashes(Velocity.GetHashCode(), Position.GetHashCode());
		}
    }

	// public bool IsGrounded()
	// {
		
	// 	return true;
	// }
}

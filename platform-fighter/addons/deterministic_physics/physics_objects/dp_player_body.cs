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
	public DM_Vector2 Velocity = new();
	[Export] InputHandler inputHandler;
	[Export] string EditorAcceleration = "0";
	DM64 Acceleration = new(.0025f);

	[Export] Vector2I EditorMaxVelocity = new();
	
	DM64 MaxVelocity = new();

    public ISaveHandler MakeSaveHandler()
    {
        return new SaveHandler<dp_player_body, SerializedState> (this);
    }

    public void Tick()
	{
		DM_Vector2 dirVector = inputHandler.PollForStickState(0);
		dirVector.y =dirVector.y * -1;
		Velocity += Acceleration * dirVector;
		Position = Position + Velocity;
	}

    public class SerializedState : ISerializedState<dp_player_body>
	{
		public DM_Vector2 Velocity;
		public DM_Vector2 Position;

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

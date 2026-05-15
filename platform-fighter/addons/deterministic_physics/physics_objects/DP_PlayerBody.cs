using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.Arm;
using System.Xml;

[GlobalClass]
[Tool]
public partial class DP_PlayerBody : DP_Object, ICanBeTicked, ISerializable
{
	public DM_Vector Velocity = new();
	[Export] InputHandler inputHandler;
	[Export] public DM64 Acceleration = new();
	
	[Export] public DM64 Gravity = new();

	
	public DM64 MaxVelocity = new();

    public ISaveHandler MakeSaveHandler()
    {
        return new SaveHandler<DP_PlayerBody, SerializedState> (this);
    }

    public void Tick()
	{
		DM_Vector dirVector = inputHandler.PollForStickState(0);
		dirVector.y = dirVector.y * -1;
		Velocity += Acceleration * dirVector;
		Velocity.y -= Gravity;
		Position = Position + Velocity;
	}

    public class SerializedState : ISerializedState<DP_PlayerBody>
	{
		public DM_Vector Velocity = new();
		public DM_Vector Position = new();

        public void Load(DP_PlayerBody Owner)
        {
            Owner.Velocity = Velocity;
			Owner.Position = Position;
        }

        public void Save(DP_PlayerBody Owner)
        {
            Position = Owner.Position;
			Velocity = Owner.Velocity;
        }
		public int GetHash()
		{
			return TickManager.DeterministicCombineHashes(Velocity.GetHashCode(), Position.GetHashCode());
		}
    }

}

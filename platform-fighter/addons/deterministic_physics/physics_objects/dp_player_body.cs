using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.Arm;

[GlobalClass]
[Tool]
public partial class dp_player_body : dp_object, ITickable, ISerializable
{
	public DM_Vector2 Velocity = new();
	[Export] InputHandler inputHandler;
	[Export] float EditorAcceleration = new();
	DM64 Acceleration = new(.0025f);

	[Export] float EditorMaxVelocity = new();
	DM64 MaxVelocity = new();

	private ISaveHandler _saveHandler;
    public ISaveHandler saveHandler { get { return _saveHandler;} set {_saveHandler = value;} }

    public override void _Ready()
    {
    	_saveHandler = new SaveHandler<dp_player_body, SerializedState>(NetworkManager.MAX_ROLLBACK_FRAMES, this);
    }


    public void Tick()
	{
		DM_Vector2 dirVector = inputHandler.PollForStickState(0);
		dirVector.y =dirVector.y * -1;
		Velocity += Acceleration * dirVector;
		Position = Position + Velocity;
	}

    private class SerializedState : ISerializedState<dp_player_body>
	{
		public DM_Vector2 Velocity;
		public DM_Vector2 Position;

        public void Load(dp_player_body e)
        {
            throw new NotImplementedException();
        }

        public void Save(dp_player_body e)
        {
            throw new NotImplementedException();
        }
    }

	// public bool IsGrounded()
	// {
		
	// 	return true;
	// }
}

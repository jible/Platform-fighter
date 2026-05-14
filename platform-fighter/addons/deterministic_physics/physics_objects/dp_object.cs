using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text.RegularExpressions;

[Tool]
[GlobalClass]
public partial class dp_object : Node
{
	// World Space
	[Export] public Vector2I EditorPosition= new();
	[Export]public DM_Vector2 Position;

	public DM_Vector2 GlobalPosition;

	public void ComputeGlobalRuntimePosition()
	{
		
		dp_object Parent = GetParent() as dp_object;
		GlobalPosition = (Parent != null)?
		Parent.GlobalPosition + Position:
		Position;

		foreach ( Node Child in GetChildren())
		{
			if (Child is dp_object CastedChild)
			{
				CastedChild.ComputeGlobalRuntimePosition();
			}
		}
	}

	// Rollback Data
	PhysicsObjectData?[] RollbackData;
	public static dp_physics_server GlobalPhysicsServer;

	

	// Collision Data
	[Export]public bool is_active = true;
	private dp_shape _shape;
	[Export]public dp_shape Shape
	{
		get => _shape;
		set
		{
			// When you set a new shape, automatically set the shape's object reference to this.
			_shape = value;
			if (_shape != null)
			{
				_shape.PhysicsObject = this;
			}
		}
	}
	// This is where an object seeks objects to collide/ overlap with
	[Export]public int layer_collision = 0;
	// This is where an object exists for other objects to seek
	[Export]public int mask_collision = 0;
	[Export]public bool is_trigger = false;
	[Export]public bool is_static = false;
	[Export]public Color color = new Color((float)0.188, (float)0.569, (float)0.341, (float)0.773);

	// Trigger overlap handling
	Dictionary<dp_object, bool>[] overlaps = [];
	
	// Helper for getting key of position and overlap
	public static TickManager tickManager;

	public static ulong GetCurrentBufferPosition() { return (ulong) tickManager.GetStateKey(tickManager.GetCurrentTick()) ; }
	public static ulong GetPrevBufferPosition() { return (ulong) tickManager.GetStateKey(tickManager.GetCurrentTick() - 1) ; }

	public static ulong GetBufferPositionAt(ulong frame) { return (ulong) tickManager.GetStateKey((int)frame) ; }
	
		// Position buffer
	static int MaxDataBufferSize = NetworkManager.MAX_ROLLBACK_FRAMES;
	DM_Vector2[] position_buffer = [];

	[Signal] public delegate void ObjectEnteredEventHandler(dp_object other);
	[Signal] public delegate void ObjectExitedEventHandler(dp_object other);
	[Signal] public delegate void ObjectCollidedEventHandler(dp_object other);

	public override void _Ready()
	{
		// Fill overlaps and position buffer with 
		if (GlobalPhysicsServer == null)
		{
			GlobalPhysicsServer = dp_physics_server.GlobalInstance;
			if (GlobalPhysicsServer == null)
			{
				GD.Print("No Physics Server");
				return;
			}
		}
		GlobalPhysicsServer.RegisterObj(this);
		if (Engine.IsEditorHint()) {return;}
		overlaps = new Dictionary<dp_object, bool>[MaxDataBufferSize];
		RollbackData = new PhysicsObjectData?[MaxDataBufferSize];
		for (int i = 0; i < MaxDataBufferSize; i++)
		{
			RollbackData[i] = null;
			overlaps[i] = [];
		}
	}

	// This needs to be called every frame before starting to collect overlap data.
	public void CleanseBuffers()
	{
		RollbackData[GetCurrentBufferPosition()] = null;
		overlaps[GetCurrentBufferPosition()].Clear();

	}
	// Overlap Detection
	public bool CheckOverlap(dp_object other)
	{
		bool is_overlapping = false;

		switch (Shape)
		{
			case dp_circle:
				is_overlapping = detect_circle_overlap(other);
				break;
			case dp_rectangle:
				is_overlapping = detect_rect_overlap(other);
				break;
			default:
				return false;
		}
		if (is_overlapping && !overlaps[GetPrevBufferPosition()].ContainsKey(other))
		{
			EmitSignal(SignalName.ObjectEntered, other);
		}
		if (!is_overlapping && overlaps[GetPrevBufferPosition()].ContainsKey(other))
		{
			EmitSignal(SignalName.ObjectExited, other);
		}
		if (is_overlapping){ overlaps[GetCurrentBufferPosition()][other] = true;}

		return is_overlapping;
	}

	public bool detect_circle_overlap(dp_object other)
	{
		switch (other.Shape)
		{
			case dp_circle otherCircle when Shape is dp_circle thisCircle:
				DM64 max_distance = thisCircle.Radius + otherCircle.Radius;
				DM64 distance = (GlobalPosition - other.GlobalPosition).GetMagnitude(); 
				return distance < max_distance;
			case dp_rectangle:
				GD.Print("circle rect overlap not programmed");
				break;
		}
		return false;
	}

	public bool detect_rect_overlap(dp_object other)
	{
		switch (other.Shape)
		{
			case dp_circle:
				GD.Print("circle rect overlap not programmed");
				break;
			case dp_rectangle otherRectangle when Shape is dp_rectangle thisRectangle:
				DM64 AL = GlobalPosition.x;
				DM64 AR = GlobalPosition.x + thisRectangle.Size.x;
				DM64 AB = GlobalPosition.y;
				DM64 AT = GlobalPosition.y + thisRectangle.Size.y;  

				DM64 BL = other.GlobalPosition.x;
				DM64 BR = other.GlobalPosition.x + otherRectangle.Size.x;
				DM64 BB = other.GlobalPosition.y;
				DM64 BT = other.GlobalPosition.y + otherRectangle.Size.y; 

				return (
					(AL < BR ) &&
					(AR > BL) &&
					(AB < BT ) &&
					(AT > BB)
				);
		}
		return false;
	}

	// Collision Detection and handling.
	public void CheckCollision(dp_object other)
	{
		bool collided = false;
		switch (other.Shape)
		{
			case dp_circle:
				break;
			case dp_rectangle:
				collided = HandleRectCollision(other);
				break;
		}
		if (collided)
		{
			EmitSignal(SignalName.ObjectCollided, other);
			
		}
		return;
	}

	public bool HandleRectCollision(dp_object other)
	{
		switch (other.Shape)
		{
			case dp_circle:
				break;
			case dp_rectangle otherRectangle  when Shape is dp_rectangle thisRectangle:

				PhysicsObjectData? NullablePreviousFrameData = RollbackData[ GetPrevBufferPosition()];


				if (NullablePreviousFrameData == null )
				{
					return HandleStaticRectRectCollision(thisRectangle, other, otherRectangle);
				}

				PhysicsObjectData PreviousFrameData = (PhysicsObjectData)NullablePreviousFrameData;
				DM_Vector2 PrevPos = PreviousFrameData.Position; 
				
				if (PrevPos == GlobalPosition)
				{
					return HandleStaticRectRectCollision(thisRectangle, other, otherRectangle);
				}

				DM_Vector2 vel = GlobalPosition - PrevPos;
				if (vel.x == new DM64(0) && vel.y == new DM64(0)){ 
					// GD.Print("Still need to handle case with no velocity");
					return false;
				}
				DM_Vector2 ProjectedPosition = GlobalPosition.copy();

				DM_Vector2 ThisHalfSize = thisRectangle.Size / 2;
				DM_Vector2 OtherHalfSize = otherRectangle.Size / 2;

				DM_Vector2 other_expanded_min = other.GlobalPosition - OtherHalfSize - ThisHalfSize;
				DM_Vector2 other_expanded_max = other.GlobalPosition + OtherHalfSize + ThisHalfSize;

				
				DM64 enterX;
				if ((vel.x == 0) &&  
				(GlobalPosition.x <other_expanded_min.x || 
				GlobalPosition.x > other_expanded_max.x)) return false;

				if (vel.x == 0){enterX = new DM64(0);
				} else
				{enterX = (other_expanded_min.x - PrevPos.x)/vel.x;}

				DM64 exitX;
				if (vel.x == 0){ exitX = new DM64(1);} else
				{exitX = (other_expanded_max.x - PrevPos.x)/vel.x;}
				if (vel.x < 0){ (enterX, exitX) = (exitX, enterX); }
				
				DM64 enterY;
				if ((vel.y == 0) && 
				(GlobalPosition.y <other_expanded_min.y || 
				GlobalPosition.y > other_expanded_max.y)) return false;

				if (vel.y == 0){ enterY = new DM64(0);} else
				{enterY = (other_expanded_min.y - PrevPos.y)/vel.y;}
				DM64 exitY;
				if (vel.y == 0){ exitY = new DM64(1);} else
				{exitY = (other_expanded_max.y - PrevPos.y)/vel.y;}
				if (vel.y < 0){ (enterY, exitY) = (exitY, enterY); }

				bool EnteredOnX = enterX > enterY;
				DM64 enter = EnteredOnX? enterX: enterY;
				DM64 exit = !EnteredOnX? exitX: exitY;

				if (enter > exit ||enter > 1 || enter < 0) {return false;}
				GlobalPosition = PrevPos + (vel * enter);
				DM_Vector2 normal = EnteredOnX? new DM_Vector2(vel.x.Sign() * -1, new( 0)) : new DM_Vector2( new( 0), vel.y.Sign() * -1);
				GlobalPosition += normal * dp_physics_server.GlobalInstance.Epsilon;

				// Sliding
				if (EnteredOnX)
				{
					GlobalPosition = new DM_Vector2(GlobalPosition.x, ProjectedPosition.y);
				} else
				{
					GlobalPosition = new DM_Vector2(ProjectedPosition.x, GlobalPosition.y);
				}

				return true;
		}
		return false;
	}

	public bool HandleStaticRectRectCollision(dp_rectangle CastedShape, dp_object Other, dp_rectangle OtherCastedShape)
	{
		bool overlapping = detect_rect_overlap(Other);

		if (!overlapping) return false;

		// Gonna get a little creative with this for now! Instead of finding the nearest edge,
		//  im gonna take the minecraft approach and always push things up!

		DM64 newY = Other.Position.y +(OtherCastedShape.Size.y/2) + (CastedShape.Size.y/2) + dp_physics_server.GlobalInstance.Epsilon; 
		Position.y = newY;
		return true;
	}

	public void PopulateCurrentFrame()
	{
		PhysicsObjectData Data = new(GlobalPosition);
		RollbackData[GetCurrentBufferPosition()] = Data;
	}

	public struct PhysicsObjectData
	{
		public DM_Vector2 Position = new();
		// public dp_shape.ShapeData ShapeData;
		public PhysicsObjectData(DM_Vector2 _position)
		{
			Position = _position;
		}
	}
}

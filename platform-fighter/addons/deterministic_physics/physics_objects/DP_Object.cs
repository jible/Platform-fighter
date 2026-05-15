using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text.RegularExpressions;

[Tool]
[GlobalClass]
public partial class DP_Object : Node
{
	// World Space
	[Export]public DM_Vector Position = new();

	public DM_Vector GlobalPosition 
	{
		get
		{
			var parent = GetParent();
			var ParentPosition = new DM_Vector(0,0);
			if (parent != null && (parent is DP_Object castedParent))
			{
				ParentPosition = castedParent.GlobalPosition;
			}
			return Position + ParentPosition;
		}
		set
		{
			var parent = GetParent() as DP_Object;
			var ParentPosition = parent?.GlobalPosition ?? new DM_Vector(0,0);
			Position = value - ParentPosition;
		}
	}

	

	// Rollback Data
	PhysicsObjectData?[] RollbackData;
	public static DP_PhysicsServer GlobalPhysicsServer;

	

	// Collision Data
	[Export]public bool is_active = true;
	private DP_Shape _shape;
	[Export]public DP_Shape Shape
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
	Dictionary<DP_Object, bool>[] overlaps = [];
	
	// Helper for getting key of position and overlap
	public static TickManager tickManager;

	public static ulong GetCurrentBufferPosition() { return (ulong) tickManager.GetStateKey(tickManager.GetCurrentTick()) ; }
	public static ulong GetPrevBufferPosition() { return (ulong) tickManager.GetStateKey(tickManager.GetCurrentTick() - 1) ; }

	public static ulong GetBufferPositionAt(ulong frame) { return (ulong) tickManager.GetStateKey((int)frame) ; }
	
		// Position buffer
	static int MaxDataBufferSize = NetworkManager.MAX_ROLLBACK_FRAMES;
	DM_Vector[] position_buffer = [];

	[Signal] public delegate void ObjectEnteredEventHandler(DP_Object other);
	[Signal] public delegate void ObjectExitedEventHandler(DP_Object other);
	[Signal] public delegate void ObjectCollidedEventHandler(DP_Object other);

	public override void _Ready()
	{
		// Fill overlaps and position buffer with 
		if (GlobalPhysicsServer == null)
		{
			GlobalPhysicsServer = DP_PhysicsServer.GlobalInstance;
			if (GlobalPhysicsServer == null)
			{
				GD.Print("No Physics Server");
				return;
			}
		}
		GlobalPhysicsServer.RegisterObj(this);
		if (Engine.IsEditorHint()) {return;}
		overlaps = new Dictionary<DP_Object, bool>[MaxDataBufferSize];
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
	public bool CheckOverlap(DP_Object other)
	{
		bool is_overlapping = false;

		switch (Shape)
		{
			case DP_Circle:
				is_overlapping = detect_circle_overlap(other);
				break;
			case DP_Rectangle:
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

	public bool detect_circle_overlap(DP_Object other)
	{
		switch (other.Shape)
		{
			case DP_Circle otherCircle when Shape is DP_Circle thisCircle:
				DM64 max_distance = thisCircle.Radius + otherCircle.Radius;
				DM64 distance = (GlobalPosition - other.GlobalPosition).GetMagnitude(); 
				return distance < max_distance;
			case DP_Rectangle:
				GD.Print("circle rect overlap not programmed");
				break;
		}
		return false;
	}

	public bool detect_rect_overlap(DP_Object other)
	{
		switch (other.Shape)
		{
			case DP_Circle:
				GD.Print("circle rect overlap not programmed");
				break;
			case DP_Rectangle otherRectangle when Shape is DP_Rectangle thisRectangle:
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
	public void CheckCollision(DP_Object other)
	{
		bool collided = false;
		switch (other.Shape)
		{
			case DP_Circle:
				break;
			case DP_Rectangle:
				collided = HandleRectCollision(other);
				break;
		}
		if (collided)
		{
			EmitSignal(SignalName.ObjectCollided, other);
			
		}
		return;
	}

	public bool HandleRectCollision(DP_Object other)
	{
		switch (other.Shape)
		{
			case DP_Circle:
				break;
			case DP_Rectangle otherRectangle  when Shape is DP_Rectangle thisRectangle:

				PhysicsObjectData? NullablePreviousFrameData = RollbackData[ GetPrevBufferPosition()];


				if (NullablePreviousFrameData == null )
				{
					return HandleStaticRectRectCollision(thisRectangle, other, otherRectangle);
				}

				PhysicsObjectData PreviousFrameData = (PhysicsObjectData)NullablePreviousFrameData;
				DM_Vector PrevPos = PreviousFrameData.Position; 
				
				if (PrevPos == GlobalPosition)
				{
					return HandleStaticRectRectCollision(thisRectangle, other, otherRectangle);
				}

				DM_Vector vel = GlobalPosition - PrevPos;
				if (vel.x == new DM64(0) && vel.y == new DM64(0)){ 
					// GD.Print("Still need to handle case with no velocity");
					return false;
				}
				DM_Vector ProjectedPosition = GlobalPosition.copy();

				DM_Vector ThisHalfSize = thisRectangle.Size / 2;
				DM_Vector OtherHalfSize = otherRectangle.Size / 2;

				DM_Vector other_expanded_min = other.GlobalPosition - OtherHalfSize - ThisHalfSize;
				DM_Vector other_expanded_max = other.GlobalPosition + OtherHalfSize + ThisHalfSize;

				
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
				DM_Vector normal = EnteredOnX? new DM_Vector(vel.x.Sign() * -1, new( 0)) : new DM_Vector( new( 0), vel.y.Sign() * -1);
				GlobalPosition += normal * DP_PhysicsServer.GlobalInstance.Epsilon;

				// Sliding
				if (EnteredOnX)
				{
					GlobalPosition = new DM_Vector(GlobalPosition.x, ProjectedPosition.y);
				} else
				{
					GlobalPosition = new DM_Vector(ProjectedPosition.x, GlobalPosition.y);
				}

				return true;
		}
		return false;
	}

	public bool HandleStaticRectRectCollision(DP_Rectangle CastedShape, DP_Object Other, DP_Rectangle OtherCastedShape)
	{
		bool overlapping = detect_rect_overlap(Other);

		if (!overlapping) return false;

		// Gonna get a little creative with this for now! Instead of finding the nearest edge,
		//  im gonna take the minecraft approach and always push things up!

		DM64 newY = Other.Position.y +(OtherCastedShape.Size.y/2) + (CastedShape.Size.y/2) + DP_PhysicsServer.GlobalInstance.Epsilon; 
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
		public DM_Vector Position = new();
		// public dp_shape.ShapeData ShapeData;
		public PhysicsObjectData(DM_Vector _position)
		{
			Position = _position;
		}
	}
}

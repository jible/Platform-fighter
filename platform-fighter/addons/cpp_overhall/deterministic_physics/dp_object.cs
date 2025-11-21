using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[Tool]
[GlobalClass]
public partial class dp_object : Node
{
	// Rollback Data
	Dictionary<String, Object>[] RollbackData;

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
	public static ulong GetCurrentBufferPosition() { return GetBufferPositionAt(Godot.Engine.GetPhysicsFrames()); }
	public static ulong GetPrevBufferPosition() { return GetBufferPositionAt(Godot.Engine.GetPhysicsFrames() - 1); }

	public static ulong GetBufferPositionAt(ulong frame) { return frame % (ulong)MaxDataBufferSize; }
	
		// Position buffer
	static int max_physics_rollback = 50;
	static int MaxDataBufferSize = max_physics_rollback + 1;
	DM_Vector2[] position_buffer = [];


	[Signal] public delegate void ObjectEnteredEventHandler(dp_object other);
	[Signal] public delegate void ObjectExitedEventHandler(dp_object other);
	[Signal] public delegate void ObjectCollidedEventHandler(dp_object other);

	public override void _Ready()
	{
		// Fill overlaps and position buffer with 
		if (Engine.IsEditorHint()) {return;}
		overlaps = new Dictionary<dp_object, bool>[MaxDataBufferSize];
		RollbackData = new Dictionary<String, Object>[MaxDataBufferSize];
		for (int i = 0; i < MaxDataBufferSize; i++)
		{
			RollbackData[i] = [];
			overlaps[i] = [];
		}
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
			default:
				detect_rect_overlap(other);
				break;
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
				DM64 max_distance = thisCircle.radius + otherCircle.radius;
				DM64 distance = (Shape.Position - other.Shape.Position).GetMagnitude(); 
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
				DM64 AL = thisRectangle.Position.x;
				DM64 AR = thisRectangle.Position.x + thisRectangle.size.x;
				DM64 AB = thisRectangle.Position.y;
				DM64 AT = thisRectangle.Position.y + thisRectangle.size.y;  

				DM64 BL = otherRectangle.Position.x;
				DM64 BR = otherRectangle.Position.x + otherRectangle.size.x;
				DM64 BB = otherRectangle.Position.y;
				DM64 BT = otherRectangle.Position.y + thisRectangle.size.y; 

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
				Object PrevPosFromDict;
				Dictionary<String, Object> FrameData = GetFrameData(Godot.Engine.GetPhysicsFrames() - 1);

				if (!FrameData.TryGetValue("position", out PrevPosFromDict))
				{
					return false;
				}
				DM_Vector2 PrevPos = (DM_Vector2)PrevPosFromDict; 
				DM_Vector2 vel = Shape.Position - PrevPos;
				if (vel.x == new DM64(0) && vel.y == new DM64(0)){ 
					// GD.Print("Still need to handle case with no velocity");

					return false;
				}


				DM_Vector2 ThisHalfSize = thisRectangle.size / 2;
				DM_Vector2 OtherHalfSize = otherRectangle.size / 2;

				DM_Vector2 other_expanded_min = otherRectangle.Position - OtherHalfSize - ThisHalfSize;
				DM_Vector2 other_expanded_max = otherRectangle.Position + OtherHalfSize + ThisHalfSize;

			 

				
				DM64 enterX;
				if ((vel.x == 0) &&  
				(thisRectangle.Position.x <other_expanded_min.x || 
				thisRectangle.Position.x > other_expanded_max.x)) return false;

				if (vel.x == 0){enterX = new DM64(0);
				} else
				{enterX = (other_expanded_min.x - PrevPos.x)/vel.x;}

				DM64 exitX;
				if (vel.x == 0){ exitX = new DM64(1);} else
				{exitX = (other_expanded_max.x - PrevPos.x)/vel.x;}
				if (vel.x < 0){ (enterX, exitX) = (exitX, enterX); }
				
				DM64 enterY;
				if ((vel.y == 0) && 
				(thisRectangle.Position.y <other_expanded_min.y || 
				thisRectangle.Position.y > other_expanded_max.y)) return false;

				if (vel.y == 0){ enterY = new DM64(0);} else
				{enterY = (other_expanded_min.y - PrevPos.y)/vel.y;}
				DM64 exitY;
				if (vel.y == 0){ exitY = new DM64(1);} else
				{exitY = (other_expanded_max.y - PrevPos.y)/vel.y;}
				if (vel.y < 0){ (enterY, exitY) = (exitY, enterY); }
				
				// Todo:  Case for sliding



				// Case for returning object to exactly where it entered:
				DM64 enter = enterX > enterY? enterX: enterY;
				DM64 exit = exitX < exitY? exitX: exitY;

				if (enter > exit ||enter > 1 || enter < 0) {return false;}
				thisRectangle.Position = PrevPos + (vel * enter);
				return true;
		}
		return false;
	}
	
	public Dictionary<String, Object> GetFrameData(ulong frame)
	{
		return RollbackData[GetBufferPositionAt(frame)];
		
	}

	public void PopulateCurrentFrame()
	{
		Dictionary<String, Object> Data = Shape.ExtractData();
		RollbackData[GetCurrentBufferPosition()] = Data;
	}
}

using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

[GlobalClass]
[Tool]
public partial class dp_physics_server : Node
{
	public List<dp_object> AllObjects = new List<dp_object>();
	[Export] public Node SearchRoot;

	// Play with these for optimizing!!
	[Export] public int EditorHashGridSize = 30;
	public DM64 HashGridSize = new(30);
	[Export] public int EditorBigObjectMin = 100;
	public DM64 BigObjectMin = new (100);

	public override void _Ready()
	{
		dp_object.GlobalPhysicsServer = this;
		AllObjects = new List<dp_object>();
		if (SearchRoot != null)
		{
			GetAllShapes(SearchRoot);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint()) { return; }
		PhysicsTick();
	}

	public void PhysicsTick()
	{
		if (Engine.IsEditorHint()) { return; }

		// Spacial Hashing
		Dictionary<Vector2I, List<dp_object>> grid = [];
		Dictionary<dp_object, List<Vector2I>> ObjectCells =[];

		HashObjects(grid, ObjectCells);

		// Itterate through all objects
		// Use Spacial Hashing to decide what objects to check
		foreach (dp_object ObjA in AllObjects)
		{
			HashSet<dp_object> interacted = [];
			HandleObjectProcess(ObjA, grid, ObjectCells, interacted);
		}
	}

	// Itterates through all physics objects and populates the grid with all 
	// tile positions they overlap 
	public void HashObjects(
		Dictionary<Vector2I, List<dp_object>> grid, 
		Dictionary<dp_object, List<Vector2I>> ObjectCells)
	{
		foreach (dp_object Obj in AllObjects)
		{
			if (!Obj.is_active ||Obj.Shape == null) continue;
			ObjectCells[Obj] = GetCoveredCells(Obj);
			foreach (var cell in ObjectCells[Obj])
			{
				if (!grid.TryGetValue(cell, out var list))
				{grid[cell] = [];}
				grid[cell].Add(Obj);
			}
		}
	}
	// For a given object, itterate through all other physics objects and checks if they potentially overlap on
	// any hashed tiles. if they do, it check 
	public void HandleObjectProcess( 
		dp_object obj,
		Dictionary<Vector2I, List<dp_object>> grid, 
		Dictionary<dp_object, List<Vector2I>> ObjectCells,
		HashSet<dp_object> interacted)
	{
		obj.CleanseBuffers();
			if (!obj.is_active || obj.Shape ==null){return;} 
			Vector2I GridPos = GetGridPos(obj);

			List<dp_object> cellData;
			if (!grid.TryGetValue(GridPos, out cellData))
			{cellData = [];}
			foreach (var cell in ObjectCells[obj])
			{
				if (grid.TryGetValue(cell, out var list))
				{
					foreach (var other in list)
					{
						if (interacted.Contains(other))continue;
						interacted.Add(other);
						HandleIndividualInteraction(obj, other);
					}
				}
			}
			obj.PopulateCurrentFrame();
	}

	private Vector2I GetGridPos(dp_object obj)
	{
		DM_Vector2 DM_GridPos = obj.Shape.Position / HashGridSize;
		return new(DM_GridPos.x.Round().to_int(), DM_GridPos.y.Round().to_int());
	}

	private List<Vector2I> GetCoveredCells(dp_object obj)
	{
		List<Vector2I> o = [];
		dp_shape shape = obj.Shape;
		if (shape == null)
		{
			return o;
		}

		DM64 maxSize = shape.GetMaxSize().Round();
		DM64 CellsCovered = (maxSize/ HashGridSize).Ceil();
		int Covered = CellsCovered.to_int();

		Vector2I gridPos = GetGridPos(obj);

		for (int i = -Covered; i <= Covered; i+= 1)
		{
			for (int j = -Covered; j <= Covered; j+= 1)
			{
				o.Add(gridPos + new Vector2I(i,j));
			}
		}

		return o;
	}

	public void HandleIndividualInteraction(dp_object ObjA, dp_object ObjB)
	{
		if (ObjA == ObjB) return;
		if (ObjA.is_static) return;
		if (!ObjB.is_active){return;} 
		if ((ObjA.mask_collision & ObjB.layer_collision) == 0){return;}
		if (!ObjB.is_active){return;} 
		if (ObjA.is_trigger)
		{
			// Search for overlaps
			ObjA.CheckOverlap(ObjB);
			return;
		}
		if (ObjB.is_trigger){ return;}
		
		// Final case where Obj A and B are collision objects
		ObjA.CheckCollision(ObjB);    
	}

	public void RegisterObj( dp_object target)
	{
		if (AllObjects.Contains(target)) return;
		AllObjects.Add(target);
	}


	public void GetAllShapes(Node Parent)
	{
		if (Parent == null) { return; }
		if (Parent is dp_object) { AllObjects.Add((dp_object)Parent); }
		foreach (var ChildNode in Parent.GetChildren())
		{
			GetAllShapes(ChildNode);
		}
	}
	private void _on_node_added(Node node)
	{
		if (node is dp_object CastedNode)
		{
			AllObjects.Add(CastedNode);
		}
	}

	private void _on_node_removed(Node node)
	{
		if (node is dp_object)
		{
			AllObjects.Remove((dp_object)node);

		}
	}
}

using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.AccessControl;

[GlobalClass]
[Tool]
public partial class DP_PhysicsServer : Node
{
	public List<DP_Object> AllEntities = new List<DP_Object>();
	public string EditorEpsilon = "0.0001";
	public DM64 Epsilon = new(0);


	// Play with these for optimizing!!
	[Export] public int EditorHashGridSize = 30;
	public DM64 HashGridSize = new(30);

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			Epsilon = new DM64(EditorEpsilon);
		}
		GlobalInstance = this;
		configure();
		GetTree().SceneChanged += () => configure();
	}

	public Node GetRoot()
	{
		Node root= Engine.IsEditorHint() 
		? GetTree().EditedSceneRoot:
		GetTree().CurrentScene;

		return root;
	}
	
	public static DP_PhysicsServer GlobalInstance;
	public void configure(Node SceneRoot = null)
	{
		if (SceneRoot == null)
		{
			SceneRoot = GetRoot();
		}

		AllEntities.Clear();
		if (SceneRoot != null)
		{
			GetAllEntities(SceneRoot);
		}
	}

	public void PhysicsTick()
	{
		if (Engine.IsEditorHint()) { return; }
		// Spacial Hashing
		Dictionary<Vector2I, List<DP_Object>> Grid = [];
		Dictionary<DP_Object, List<Vector2I>> ObjectCells =[];

		HashObjects(Grid, ObjectCells);

		// Itterate through all objects
		// Use Spacial Hashing to decide what objects to check
		foreach (DP_Object Entity in AllEntities)
		{
			HashSet<DP_Object> interacted = [];
			HandleObjectProcess(Entity, Grid, ObjectCells, interacted);
		}
	}

	// Itterates through all physics objects and populates the grid with all 
	// tile positions they overlap 
	public void HashObjects(
		Dictionary<Vector2I, List<DP_Object>> Grid, 
		Dictionary<DP_Object, List<Vector2I>> ObjectCells)
	{
		foreach (DP_Object Entity in AllEntities)
		{
			if (!Entity.is_active || Entity.Shape == null) continue;
			ObjectCells[Entity] = GetCoveredCells(Entity);
			foreach (var Cell in ObjectCells[Entity])
			{
				if (!Grid.TryGetValue(Cell, out var list))
				{Grid[Cell] = [];}
				Grid[Cell].Add(Entity);
			}
		}
	}

	// For a given object, itterate through all other physics objects and checks if they potentially overlap on
	// any hashed tiles. if they do, it check 
	public void HandleObjectProcess( 
		DP_Object EntityA,
		Dictionary<Vector2I, List<DP_Object>> Grid, 
		Dictionary<DP_Object, List<Vector2I>> EntityCells,
		HashSet<DP_Object> interacted)
	{
		EntityA.CleanseBuffers();
		if (!EntityA.is_active || EntityA.Shape ==null){return;} 

		
		foreach (var cell in EntityCells[EntityA])
		{
			if (Grid.TryGetValue(cell, out var list))
			{
				foreach (var EntityB in list)
				{
					if (interacted.Contains(EntityB))continue;
					interacted.Add(EntityB);
					HandleIndividualInteraction(EntityA, EntityB);
				}
			}
		}
		EntityA.PopulateCurrentFrame();
	}

	private Vector2I GetGridPos(DP_Object Entity)
	{
		DM_Vector DM_GridPos = Entity.GlobalPosition / HashGridSize;
		return new(DM_GridPos.x.Round().to_int(), DM_GridPos.y.Round().to_int());
	}

	private List<Vector2I> GetCoveredCells(DP_Object Entity)
	{
		List<Vector2I> Output = [];
		DP_Shape shape = Entity.Shape;
		if (shape == null)
		{
			return Output;
		}

		DM64 maxSize = shape.GetMaxSize().Round();
		DM64 CellsCovered = (maxSize/ HashGridSize).Ceil();
		int Covered = CellsCovered.to_int();

		Vector2I gridPos = GetGridPos(Entity);

		for (int i = -Covered; i <= Covered; i+= 1)
		{
			for (int j = -Covered; j <= Covered; j+= 1)
			{
				Output.Add(gridPos + new Vector2I(i,j));
			}
		}

		return Output;
	}

	public void HandleIndividualInteraction(DP_Object EntityA, DP_Object EntityB)
	{
		if (EntityA == EntityB) return;
		if (EntityA.is_static) return;
		if (!EntityB.is_active) return;
		if ((EntityA.mask_collision & EntityB.layer_collision) == 0){return;}
		if (!EntityB.is_active) return; 
		if (EntityA.is_trigger)
		{
			EntityA.CheckOverlap(EntityB);
			return;
		}
		if (EntityB.is_trigger){ return;}
		
		EntityA.CheckCollision(EntityB);    
	}

	public void RegisterObj( DP_Object target)
	{
		if (!AllEntities.Contains(target))
		{
			AllEntities.Add(target);
		} 
	}


	public void GetAllEntities(Node Parent)
	{
		if (Parent == null) { return; }
		if (Parent is DP_Object CastedParent) {
			RegisterObj(CastedParent);
		}
		foreach (var ChildNode in Parent.GetChildren())
		{
			GetAllEntities(ChildNode);
		}
	}
	
}

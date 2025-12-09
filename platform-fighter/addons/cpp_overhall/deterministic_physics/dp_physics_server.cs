using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

[GlobalClass]
[Tool]
public partial class dp_physics_server : Node
{
	public List<dp_object> AllObjects = new List<dp_object>();
	[Export] public Node SearchRoot;
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
		HandleInteractions();
	}

	public void RegisterObj( dp_object target)
    {
        if (AllObjects.Contains(target)) return;
		AllObjects.Add(target);
    }

	public void HandleInteractions()
	{
		if (Engine.IsEditorHint()) { return; }
		foreach (dp_object ObjA in AllObjects)
		{
			ObjA.CleanseBuffers();
			if (!ObjA.is_active){continue;} 
			if (ObjA.Shape ==null){continue;}
			foreach (dp_object ObjB in AllObjects)
			{
				if (ObjA == ObjB) continue;
				if (ObjA.is_static) continue;
				if (!ObjB.is_active){continue;} 
				if ((ObjA.mask_collision & ObjB.layer_collision) == 0){continue;}
				if (!ObjB.is_active){continue;} 
				if (ObjA.is_trigger)
				{
					// Search for overlaps
					ObjA.CheckOverlap(ObjB);
					continue;
				}
				if (ObjB.is_trigger){ continue;}
				
				// Final case where Obj A and B are collision objects
				ObjA.CheckCollision(ObjB);    
			}   
			ObjA.PopulateCurrentFrame();
		}
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

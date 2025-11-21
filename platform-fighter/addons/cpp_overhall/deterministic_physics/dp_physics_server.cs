using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

[GlobalClass]
[Tool]
public partial class dp_physics_server : Node
{
    public List<dp_object> AllShapes = new List<dp_object>();
    [Export] Node SearchRoot;
    //  

    public override void _Ready()
    {
        var tree = GetTree();

        tree.NodeAdded += _on_node_added;
        tree.NodeRemoved += _on_node_removed;
        AllShapes = new List<dp_object>();
        if (SearchRoot != null)
        {
            GetAllShapes(SearchRoot);
        }
    }




    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) { return; }
        HandleInteractions();
    }

    public void HandleInteractions()
    {
        foreach (dp_object ObjA in AllShapes)
        {
            if (!ObjA.is_active){continue;} 

            foreach (dp_object ObjB in AllShapes)
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
        if (Parent is dp_object) { AllShapes.Add((dp_object)Parent); }
        foreach (var ChildNode in Parent.GetChildren())
        {
            GetAllShapes(ChildNode);
        }
    }
    private void _on_node_added(Node node)
    {
        if (node is dp_object CastedNode)
        {
            AllShapes.Add(CastedNode);
        }
    }

    private void _on_node_removed(Node node)
    {
        if (node is dp_object)
        {
            AllShapes.Remove((dp_object)node);

        }
    }
}

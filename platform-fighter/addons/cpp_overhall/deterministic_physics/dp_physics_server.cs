using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
[Tool]
public partial class dp_physics_server : Node
{
    public List<dp_object> AllShapes = new List<dp_object>();
    [Export]Node SearchRoot;
    //Testing! agaom
    //  

    // This is how you connect signals!
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

    // public override void _ExitTree()
    // {
    //     var tree = GetTree();
    //     if (tree != null)
    //     {
    //         tree.NodeAdded -= _on_node_added;
    //         tree.NodeRemoved -= _on_node_removed;
    //     }
    // }



    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) { return; }
        HandleCollision();
    }

    public void HandleCollision()
    {
        return;
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

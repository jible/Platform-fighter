using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
[Tool]
public partial class dp_physics_server : Node
{
    public List<dp_object> AllShapes = [];
    [Export]Node SearchRoot;

    // This is how you connect signals!
    public override void _Ready()
    {
        GetTree().NodeAdded += _on_node_added;
        GetTree().NodeRemoved += _on_node_removed;
        AllShapes = [];
        if (SearchRoot == null){return;}
        GetAllShapes(SearchRoot);
    }


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
        if (Parent is dp_object) { AllShapes.Append(Parent); }
        foreach (var ChildNode in Parent.GetChildren())
        {
            GetAllShapes(ChildNode);
        }
    }
    public void _on_node_added(Node NewNode)
    {
        if (NewNode is dp_object)
        {
            AllShapes.Append(NewNode);
        }
    }

    public void _on_node_removed(Node node)
    {
        if (node is dp_object)
        {
            AllShapes.Remove((dp_object)node);

        }
    }
}

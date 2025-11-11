using Godot;
using System;

public partial class DP_PhysicsServer : Node
{
    public DP_Shape[] all_shapes = [];
    [Export]
    Node search_root;

    // This is how you connect signals!
    public override void _Ready()
    {
        GetTree().NodeAdded += (Node node) =>_on_node_added(node);
    }

    public void _on_node_added(Node node)
    {
        return;
    }
}

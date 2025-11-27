using Godot;
using System;
using System.ComponentModel;

public partial class dm_math_server : Node
{
     public override void _Ready()
    {
        Node Root = GetTree().Root;
        propogate_update_runtime_math_vars(Root);
    }

    public void propogate_update_runtime_math_vars(Node parent)
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> prop_list = parent.GetPropertyList(); 
    }
}

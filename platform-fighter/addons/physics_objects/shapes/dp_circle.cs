using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class dp_circle : dp_shape
{
    [Export] public float EditorRadius = 50;
    public DM64 Radius = new(50);
    
    public override DM64 GetMaxSize()
    {
        
        return Radius;
    }

    public override Dictionary<String, object> ExtractData()
    {
        return new Dictionary<String, object> {{"radius", Radius.copy()}};
    }


    public override void LoadData(Dictionary<String, object> Data)
    {
        Radius = ((DM64)Data["radius"]).copy();
    }

}
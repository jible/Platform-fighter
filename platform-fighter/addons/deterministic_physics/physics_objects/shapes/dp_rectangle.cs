using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]
public partial class dp_rectangle : dp_shape
{
    [Export] public Vector2 EditorSize = new(50,50);
    public DM_Vector2 Size = new(50,50);

    public override DM64 GetMaxSize()
    {
        return DM64.Max(Size.x, Size.y) / 2;
    }

    public override Dictionary<String, object> ExtractData()
    {
        return new Dictionary<String, object> {{"size", Size.copy()}};
    }


    public override void LoadData(Dictionary<String, object> Data)
    {
        Size = ((DM_Vector2)Data["size"]).copy();
    }

}

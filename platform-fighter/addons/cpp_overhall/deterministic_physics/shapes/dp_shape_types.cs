using Godot;
using System;

public partial class DP_ShapeType : RefCounted
{}
public partial class DP_Circle : DP_ShapeType
{
    [Export]
    public DM64 radius = new DM64();
}

public partial class DP_Rectangle : DP_ShapeType
{
    [Export]
    public DM_Vector2 size = new DM_Vector2();
}
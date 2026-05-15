using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]
public partial class DP_Rectangle : DP_Shape
{
    [Export] 
    public DM_Vector Size = new(50,50);

    public override DM64 GetMaxSize()
    {
        return DM64.Max(Size.x, Size.y) / 2;
    }
    
}

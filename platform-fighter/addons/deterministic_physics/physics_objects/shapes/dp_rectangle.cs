using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]
public partial class dp_rectangle : dp_shape
{
    [Export] public Vector2I EditorSize = new(50,50);
    public DM_Vector2 Size = new(50,50);

    public override DM64 GetMaxSize()
    {
        return DM64.Max(Size.x, Size.y) / 2;
    }

  



    
}

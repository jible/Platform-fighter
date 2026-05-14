using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class dp_circle : dp_shape
{
    [Export] 
    public DM64 Radius = new(50);
    
    public override DM64 GetMaxSize()
    {
        
        return Radius;
    }


}
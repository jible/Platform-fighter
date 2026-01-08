using Godot;
using System;

public partial class RollbackManager : Node
{
    public RollbackManager GloablInstance;
    public static int MAX_ROLLBACK_FRAMES = 50;
    public override void _Ready()
    {
        GloablInstance = this;
    }
}

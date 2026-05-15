using Godot;
using System;

[GlobalClass]
public partial class Idle : State
{
    public override void EnterState()
    {
        GD.Print("entered idle");
    }

    public override bool GeneralCondition()
    {
        return false;
        // There is no condition for idle. States should decide when they enter idle, 
        // based on when their animation ends. or when the conditions for that state is no longer met.
    }
    
    public override void LeaveState()
    {
    }

    public override void OnAnimationFinished()
    {
    }

    public override void Tick()
    {
    }

}

using Godot;
using System;

[GlobalClass]
public partial class Run : State
{
    public override void EnterState()
    {
    }

    public override bool GeneralCondition()
    {
        DM64 LeftStickXAxisMagnitude = inputHandler.PollForStickState((int)ControllerState.StickTypes.LEFT).x.Abs();
        return  LeftStickXAxisMagnitude >= StickState.MAX_STICK_AXIS_VALUE - 1;
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

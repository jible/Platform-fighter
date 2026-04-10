using Godot;
using System;

[GlobalClass]
public partial class ArielJump : State
{
    public override void EnterState()
    {
        throw new NotImplementedException();
    }

    public override bool GeneralCondition()
    {
        return inputHandler.PollForInput(ControllerState.ButtonTypes.JUMP);

    }


    public override void LeaveState()
    {
        throw new NotImplementedException();
    }

    public override void OnAnimationFinished()
    {
        throw new NotImplementedException();
    }

    public override void Tick()
    {
        throw new NotImplementedException();
    }

}

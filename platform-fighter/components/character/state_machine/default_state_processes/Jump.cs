using Godot;
using System;

[GlobalClass]
public partial class Jump : State
{
    public override void EnterState()
    {
    }

    public override bool GeneralCondition()
    {
        GD.Print("checking conditions");

        return inputHandler.PollForInput(ControllerState.ButtonTypes.JUMP);

    }
    
    public override void LeaveState()
    {
    }

    public override void OnAnimationFinished()
    {
        stateMachine.ChangeState(CharacterStateMachine.States.IDLE);

    }

    public override void Tick()
    {
    }

}

// The states every character starts with. Stateless: anything a state needs to remember goes on the Character
public class IdleState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Idle;
    public override bool ApplyGravity => false;

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.InputHandler.JustPressed(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.Jump);
        }
        else if (machine.Character.InputHandler.LeftStick() != DMVector.zero)
        {
            machine.ChangeState(CharacterStateId.Walk);
        }
    }
}

public class WalkState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Walk;

    public override bool ApplyGravity => false;

    public override void Tick(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        if (character.InputHandler.JustPressed(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.Jump);
        }
        else if (character.InputHandler.LeftStick() == DMVector.zero)
        {
            machine.ChangeState(CharacterStateId.Idle);
        } else if (character.velocity.Value.x.Sign() != character.InputHandler.LeftStick().x.Sign())
        {
            machine.ChangeState(CharacterStateId.Turn); // If they click a different direction, get a kick

        }

    }
}

public class JumpState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Jump;
    public override bool ApplyGravity => true;

    public override void EnterState(CharacterStateMachine machine)
    {
        machine.Character.Movement.Jump();
    }

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Idle);
        }
        // Placeholder until there's a jump animation to end on
        else if (machine.Character.velocity.Value.y < 0)
        {
            machine.ChangeState(CharacterStateId.Fall);
        }
    }
}

public class FallState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Fall;
    public override bool ApplyGravity => true;

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Idle);
        }
    }
}

public class TurnState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Turn;
    public override bool ApplyGravity => false;


    public override void EnterState(CharacterStateMachine machine)
    {
        // give the kick
        machine.Character.Movement.GroundedTurnAroundKick();
    }
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        // Give the turn around kick
        machine.ChangeState(CharacterStateId.Walk);
    }

    // When the anim ends
}

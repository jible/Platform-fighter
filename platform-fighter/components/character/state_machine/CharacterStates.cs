// The states every character starts with. Stateless: anything a state needs to remember goes on the Character
public class IdleState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Idle;
    public override bool ApplyGravity => false;
    public override bool Cancellable => true;

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.InputHandler.JustPressed(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.JumpSquat);
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
    public override bool Cancellable => true;

    public override void Tick(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        if (character.InputHandler.JustPressed(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.JumpSquat);
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

// Not cancellable, so an attack pressed during the crouch doesn't come out as a tilt
public class JumpSquatState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.JumpSquat;
    public override bool ApplyGravity => false;
    public override bool Cancellable => false;

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(CharacterStateId.Jump);
    }
}

public class JumpState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Jump;
    public override bool ApplyGravity => true;
    public override bool Cancellable => true;

    public override void EnterState(CharacterStateMachine machine)
    {
        machine.Character.Movement.Jump();
    }

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Land);
        }
    }

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(CharacterStateId.Fall);
    }
}

// Ends on landing, so its data should loop
public class FallState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Fall;
    public override bool ApplyGravity => true;
    public override bool Cancellable => true;

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Land);
        }
    }
}

public class LandState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Land;
    public override bool ApplyGravity => false;
    public override bool Cancellable => false;

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(CharacterStateId.Idle);
    }
}

public class TurnState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Turn;
    public override bool ApplyGravity => false;
    public override bool Cancellable => true;


    // Faces the way the stick points, and gives the kick. A stick with no sideways push keeps the old facing
    public override void EnterState(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        DM64 stickX = character.InputHandler.LeftStick().x;
        if (stickX > 0) character.direction = Character.Direction.Right;
        else if (stickX < 0) character.direction = Character.Direction.Left;
        character.Movement.GroundedTurnAroundKick();
    }
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        // Give the turn around kick
        machine.ChangeState(CharacterStateId.Walk);
    }

    // When the anim ends
}

// Plays the attack the AttackHandler started. Shared by every attack, so what each one does is in its data
public class AttackState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Attack;
    // Gravity does nothing on the ground, so aerials and ground attacks can share this
    public override bool ApplyGravity => true;
    public override bool Cancellable => false;

    public override StateDefinition GetDefinition(Character character) => character.Attacks.Current;

    // An attack set to loop never gets here, so it plays until something else changes the state
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(machine.Character.Movement.IsGrounded ? CharacterStateId.Idle : CharacterStateId.Fall);
    }
}

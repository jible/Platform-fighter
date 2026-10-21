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
            var inputDirection = machine.Character.InputHandler.LeftStick().x < 0 ? Character.Direction.Left :Character.Direction.Right;
            machine.Character.direction = inputDirection;
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
        var inputDirection = character.InputHandler.LeftStick().x.Sign() < 0? Character.Direction.Left: Character.Direction.Right;
        if (character.InputHandler.JustPressed(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.JumpSquat);
        }
        else if (character.InputHandler.LeftStick() == DMVector.zero)
        {
            machine.ChangeState(CharacterStateId.Idle);
        } else if (character.direction != inputDirection)
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
        Character character = machine.Character;
        if (character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Land);
        }
        // An air jump. The Jump state's enter does the jump and counts it
        else if (character.InputHandler.JustPressed(ControllerState.ButtonTypes.JUMP) && character.Movement.CanJump)
        {
            machine.ChangeState(CharacterStateId.Jump);
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
        character.Movement.GroundedTurnAroundKick();
    }
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        // Give the turn around kick
        Character character = machine.Character;
        DM64 stickX = character.InputHandler.LeftStick().x;
        if (stickX > 0) character.direction = Character.Direction.Right;
        else if (stickX < 0) character.direction = Character.Direction.Left;
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

    public override void Tick(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        if (character.Attacks.Current.CancelOnLand && character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Land);
        }
    }

    // An attack set to loop never gets here, so it plays until something else changes the state
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(machine.Character.Movement.IsGrounded ? CharacterStateId.Idle : CharacterStateId.Fall);
    }
}

// Entered from anything when health runs out. Not cancellable, and hits do nothing, so it always plays out
public class DyingState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Dying;
    public override bool ApplyGravity => true;
    public override bool Cancellable => false;

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(CharacterStateId.Off);
    }
}

// Out of play. Its length is how long until the respawn, and with no lives left it never leaves
public class OffState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Off;
    public override bool ApplyGravity => false;
    public override bool Cancellable => false;

    public override void EnterState(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        character.TurnOff();
        character.Life.LoseLife();
        if (!character.Life.HasLivesLeft) character.NotifyOutOfLives();
    }

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        if (character.Life.HasLivesLeft) character.Respawn();
    }
}

// The moment after a hit, before the knockback. A new hit restarts it.
// When it ends, the character is launched and turns to face whatever hit them
public class HurtStartState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.HurtStart;
    public override bool ApplyGravity => true;
    public override bool Cancellable => false;

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        DMVector knockback = character.LastHit.Knockback;
        // Placeholder knockback, the hit's vector as is
        character.Movement.SetUncappedVelocity(knockback);
        // Pushed right means the hit came from the right. No sideways push keeps the old facing
        if (knockback.x > 0) character.direction = Character.Direction.Left;
        else if (knockback.x < 0) character.direction = Character.Direction.Right;
        machine.ChangeState(CharacterStateId.Hurt);
    }
}

// Flying from the knockback. Ends on landing, or once the speed along the knockback's main axis drops low enough
public class HurtState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Hurt;
    public override bool ApplyGravity => true;
    public override bool Cancellable => false;

    // Speed along the knockback's main axis that counts as done. Placeholder, to be tuned
    private static readonly DM64 EndSpeed = new DM64(1) / 2;

    public override void Tick(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        if (character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Idle);
            return;
        }

        // The axis the knockback pushed hardest along, x on a tie
        DMVector knockback = character.LastHit.Knockback;
        DMVector velocity = character.velocity.Value;
        DM64 speed = knockback.y.Abs() > knockback.x.Abs() ? velocity.y.Abs() : velocity.x.Abs();
        if (speed < EndSpeed)
        {
            machine.ChangeState(CharacterStateId.Fall);
        }
    }
}

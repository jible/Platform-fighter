// The states every character starts with. Stateless: anything a state needs to remember goes on the Character
public class IdleState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Idle;
    public override bool ApplyGravity => false;
    public override bool Cancellable => true;

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.InputHandler.TryConsume(ControllerState.ButtonTypes.JUMP))
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
        if (character.InputHandler.TryConsume(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.JumpSquat);
        }
        else if (character.InputHandler.LeftStick() == DMVector.zero)
        {
            machine.ChangeState(CharacterStateId.Stop);
        } else if (character.direction != inputDirection)
        {
            machine.ChangeState(CharacterStateId.Turn); // If they click a different direction, get a kick

        }

    }
}

// Ends when its animation does, so its length is how long the slide lasts before Idle's normal friction takes over
public class StopState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Stop;
    public override bool ApplyGravity => false;
    public override bool Cancellable => true;

    // Pushing against the slide turns, like Walk. With no slide left, against the facing does
    public override void Tick(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        DM64 stickX = character.InputHandler.LeftStick().x;
        if (character.InputHandler.TryConsume(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.JumpSquat);
        }
        else if (stickX != 0)
        {
            DM64 moving = character.velocity.Value.x.Sign();
            if (moving == 0) moving = character.direction == Character.Direction.Right ? new DM64(1) : new DM64(-1);
            machine.ChangeState(stickX.Sign() == moving ? CharacterStateId.Walk : CharacterStateId.Turn);
        }
    }

    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        machine.ChangeState(CharacterStateId.Idle);
    }
}

// Not cancellable, so an attack pressed during the crouch doesn't come out as a tilt
public class JumpSquatState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.JumpSquat;
    public override bool ApplyGravity => false;
    public override bool Cancellable => false;

    // Still holding jump is a full jump, otherwise a short hop, so the squat's length is the window to release it
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        bool held = machine.Character.InputHandler.IsDown(ControllerState.ButtonTypes.JUMP);
        machine.ChangeState(held ? CharacterStateId.Jump : CharacterStateId.ShortHop);
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

// A jump at short hop height. Only entered from the ground, so it never uses an air jump
public class ShortHopState : JumpState
{
    public override CharacterStateId Id => CharacterStateId.ShortHop;

    public override void EnterState(CharacterStateMachine machine)
    {
        machine.Character.Movement.ShortHop();
    }

    // Plays the Jump state's data until it has its own
    public override StateDefinition GetDefinition(Character character) =>
        character.Definition.GetState(Id) ?? character.Definition.GetState(CharacterStateId.Jump);
}

// Ends on landing, so its last phase should restart
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
        // CanJump first, so a press that can't jump stays buffered
        else if (character.Movement.CanJump && character.InputHandler.TryConsume(ControllerState.ButtonTypes.JUMP))
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


    // Gives the kick. Facing stays the old way until the turn ends, so a jump or attack out of it comes out backwards
    public override void EnterState(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        character.Movement.GroundedTurnAroundKick();
    }

    public override void Tick(CharacterStateMachine machine)
    {
        if (machine.Character.InputHandler.TryConsume(ControllerState.ButtonTypes.JUMP))
        {
            machine.ChangeState(CharacterStateId.JumpSquat);
        }
    }

    // Faces the way the stick points and walks. A stick with no sideways push keeps the old facing and slides to a stop
    public override void OnAnimFinished(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        DM64 stickX = character.InputHandler.LeftStick().x;
        if (stickX == 0)
        {
            machine.ChangeState(CharacterStateId.Stop);
            return;
        }
        character.direction = stickX > 0 ? Character.Direction.Right : Character.Direction.Left;
        machine.ChangeState(CharacterStateId.Walk);
    }
}

// Plays the attack the AttackHandler started. Shared by every attack, so what each one does is in its data
public class AttackState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Attack;
    // Gravity does nothing on the ground, so aerials and ground attacks can share this
    public override bool ApplyGravity => true;
    public override bool Cancellable => false;
    // So a down attack doesn't drop the attacker off the platform they're on
    public override bool CanDropThroughPlatforms => false;

    public override StateDefinition GetDefinition(Character character) => character.Attacks.Current;

    public override void Tick(CharacterStateMachine machine)
    {
        Character character = machine.Character;
        if (character.Attacks.Current.CancelOnLand && character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Land);
        }
    }

    // An attack whose last phase loops or goes back never gets here, so it plays until something else changes the state
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

// Just got hit. The first phase is the moment before the knockback, and a new hit restarts it.
// When the first phase ends for the first time, the character is launched and turns to face whatever hit them.
// From then on it ends on landing, or once the hit's hitstun has passed. CharacterMovement swaps in knockback
// dissipation and DI for the usual movement while in this state
public class HurtState : CharacterState
{
    public override CharacterStateId Id => CharacterStateId.Hurt;
    public override bool ApplyGravity => true;
    public override bool Cancellable => false;

    private static int FirstPhaseLength(CharacterStateMachine machine) =>
        machine.CurrentDefinition?.Phases[0].Length ?? machine.Character.Data.DefaultStateLength;

    // Still in the first play of the first phase. Every tick in the state has been in that phase until it ends or
    // restarts, so the two counts only match until then. The length check covers a first phase that ends the state
    // and holds. Stateless, so it rolls back with the machine
    private static bool BeforeLaunch(CharacterStateMachine machine)
    {
        return machine.CurrentPhase == 0 && machine.TicksInPhase == machine.TicksInState && machine.TicksInPhase < FirstPhaseLength(machine);
    }

    // Leftover knockback from an earlier hit would otherwise keep dissipating out of velocity it's no longer part of
    public override void EnterState(CharacterStateMachine machine) => machine.Character.Movement.ClearKnockback();

    // The rest of the launch stays in velocity, for the usual movement to take over
    public override void ExitState(CharacterStateMachine machine) => machine.Character.Movement.ClearKnockback();

    public override void OnPhaseFinished(CharacterStateMachine machine, int phase)
    {
        // Only the first time it ends, which is when it's been the only phase played
        if (phase != 0 || machine.TicksInPhase != machine.TicksInState) return;
        Character character = machine.Character;
        DMVector knockback = character.LastHit.Knockback;
        character.Movement.Launch(knockback);
        // Pushed right means the hit came from the right. No sideways push keeps the old facing
        if (knockback.x > 0) character.direction = Character.Direction.Left;
        else if (knockback.x < 0) character.direction = Character.Direction.Right;
    }

    public override void Tick(CharacterStateMachine machine)
    {
        // Not launched yet, so there's no knockback to end
        if (BeforeLaunch(machine)) return;
        Character character = machine.Character;
        if (character.Movement.IsGrounded)
        {
            machine.ChangeState(CharacterStateId.Idle);
            return;
        }

        // The launch happened as TicksInState reached the first phase's length
        if (machine.TicksInState - FirstPhaseLength(machine) >= character.LastHit.Hitstun)
        {
            machine.ChangeState(CharacterStateId.Fall);
        }
    }
}

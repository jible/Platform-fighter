// A character's movement: walking, jumping, drag and gravity, all as changes to the character's velocity.
// Plain C#, made and owned by the character. States call into it, and the character ticks it after its state machine,
// so a jump impulse from a state lands before gravity and clamping
public class CharacterMovement
{
    private readonly Character character;
    // Set by a downward collision during the physics step, read by states on the next tick
    // Probed by the physics server at the end of each step, so it holds steady while standing still
    public bool IsGrounded => character.OnGround;
    private readonly SerializableProperty<int> usedAirJumps = new();
    // What's left of the last launch, which is part of velocity. Tracked apart from the rest so it can dissipate on its own
    // without fighting gravity. Only used in the Hurt state, which clears it on entering and leaving
    private readonly SerializableProperty<DMVector> knockback = new();
    private CharacterData Data => character.Data;
    // Horizontal max depends on whether we're grounded, vertical max is the fall speed cap
    private DMVector MaxVelocity => new(IsGrounded ? Data.Ground.MaxVelocity : Data.Air.MaxVelocity, Data.Air.MaxFallVelocity);
    // Stick x in the range [-1, 1]
    private DM64 InputX => character.InputHandler.LeftStick().x / StickState.MAX_STICK_AXIS_VALUE;
    // Stick in the range [-1, 1] on each axis
    private DMVector InputDirection => character.InputHandler.LeftStick() / new DM64(StickState.MAX_STICK_AXIS_VALUE);
    // Turns a per second rate (acceleration, friction) into how much of it applies this tick
    private static DM64 PerTick(DM64 perSecond) => perSecond / TickManager.TicksPerSecond;

    // Knockback lost per second, times the character's weight. Placeholder, to be tuned
    private static readonly DM64 KnockbackDissipation = new DM64(15);
    // How hard the stick steers knockback, per second. Well under air acceleration, so it nudges rather than steers.
    // The same for every character. Placeholder, to be tuned
    private static readonly DM64 DIAcceleration = new DM64(3);

    public CharacterMovement(Character character)
    {
        this.character = character;
        character.Colliding += onCollide;
    }

    public void onCollide(PhysicsObject colA, PhysicsObject otherObj, DMVector side)
    {
        if (side == DMVector.Down)
        {
            character.velocity.Value.y = new(0);
            usedAirJumps.Value = 0;
        }
    }

    public bool CanJump => IsGrounded || usedAirJumps.Value < Data.Jump.AirJumps;

    public void Jump()
    {
        ref DMVector velocity = ref character.velocity.Value;
        if (IsGrounded)
        {
            velocity.y = Data.Jump.Velocity;
        }
        else if (usedAirJumps.Value < Data.Jump.AirJumps)
        {
            velocity.y = Data.Jump.Velocity;
            usedAirJumps.Value++;
            AddCappedImpulse(new DMVector(Data.Jump.AirHorizontalImpulse * InputX.Sign(), new DM64(0)));
        }
    }

    // Ground only, so there's no air jump to count
    public void ShortHop()
    {
        character.velocity.Value.y = Data.Jump.ShortHopVelocity;
    }

    public void StandardMovement()
    {
        DM64 input = InputX;
        if (input == 0) return;

        ref DMVector velocity = ref character.velocity.Value;
        DM64 acceleration = IsGrounded ? Data.Ground.Acceleration : Data.Air.Acceleration;
        DM64 max = MaxVelocity.x;
        if (velocity.x.Abs() < max)
        {
            velocity.x = (velocity.x + PerTick(acceleration) * input).Clamped(-max, max);
        }
    }

    public void GroundedTurnAroundKick()
    {
        ref DMVector velocity = ref character.velocity.Value;
        DM64 acceleration = IsGrounded ? Data.Ground.Acceleration : Data.Air.Acceleration;

        DM64 input = InputX;
        DM64 turnaround = Data.Ground.TurnaroundMultiplier;
        velocity.x += PerTick(acceleration) * input.Sign() * turnaround;
    }

    // Only slows down with no input, or when something like knockback pushed past the max
    public void StandardDrag()
    {
        if (InputX == 0 || character.velocity.Value.x.Abs() > MaxVelocity.x)
        {
            ApplyDrag();
        }
    }

    public void ApplyDrag() => ApplyDrag(IsGrounded ? GroundFriction : Data.Air.Friction);

    // Grounded attacks and landing slide, so they use SlideFriction in place of the usual
    private DM64 GroundFriction =>
        character.StateMachine.IsInState(CharacterStateId.Attack) || character.StateMachine.IsInState(CharacterStateId.Land)
            ? Data.Ground.SlideFriction
            : Data.Ground.Friction;

    public void ApplyDrag(DM64 friction)
    {
        character.velocity.Value.x -= character.velocity.Value.x * PerTick(friction);
    }

    // The fall cap only limits the character's own falling, not what's left of a launch, so a spike isn't cut short.
    // Knockback is zero outside the Hurt state, so this is the plain cap there
    public void StandardGravity()
    {
        if (IsGrounded) return;
        ref DMVector velocity = ref character.velocity.Value;
        velocity.y = DM64.Max(velocity.y - PerTick(Data.Air.FallAcceleration), knockback.Value.y - Data.Air.MaxFallVelocity);
    }

    // Replaces velocity with the launch, so whatever the character was doing is gone
    public void Launch(DMVector launch)
    {
        character.velocity.Value = launch;
        knockback.Value = launch;
    }

    public void ClearKnockback() => knockback.Value = DMVector.zero;

    // Shrinks what's left of the launch by a fixed amount each tick, heavier characters faster, and never past zero
    public void ApplyKnockbackDissipation()
    {
        DMVector current = knockback.Value;
        DM64 magnitude = current.GetMagnitude();
        if (magnitude == 0) return;
        DM64 remaining = DM64.Max(magnitude - PerTick(KnockbackDissipation * Data.Combat.Weight), new DM64(0));
        SetKnockback(current * (remaining / magnitude));
    }

    // Directional influence: the stick pushes the launch a little in any direction. Part of the knockback, so it
    // dissipates with it, and pushing against the launch makes it end sooner
    public void ApplyDI()
    {
        DMVector input = InputDirection;
        if (input == DMVector.zero) return;
        SetKnockback(knockback.Value + input * PerTick(DIAcceleration));
    }

    // Velocity changes by the same amount, so the character's own motion within it is untouched
    private void SetKnockback(DMVector value)
    {
        character.velocity.Value += value - knockback.Value;
        knockback.Value = value;
    }

    // Won't push an axis past its max, but won't slow down an axis that's already over it either
    public void AddCappedImpulse(DMVector impulse)
    {
        ref DMVector velocity = ref character.velocity.Value;
        DMVector max = MaxVelocity;
        velocity.x = CappedAdd(velocity.x, impulse.x, max.x);
        velocity.y = CappedAdd(velocity.y, impulse.y, max.y);
    }

    private static DM64 CappedAdd(DM64 current, DM64 impulse, DM64 max)
    {
        if (impulse == 0) return current;
        bool sameDirection = impulse.Sign() == current.Sign();
        if (sameDirection && current.Abs() >= max) return current;

        DM64 result = current + impulse;
        // Slowing down without flipping direction is always allowed
        if (!sameDirection && result.Sign() == current.Sign()) return result;
        return result.Clamped(-max, max);
    }

    public void AddUncappedImpulse(DMVector impulse)
    {
        character.velocity.Value += impulse;
    }

    public void SetUncappedVelocity(DMVector velocity)
    {
        character.velocity.Value = velocity;
    }

    public void Tick()
    {
        // Hurt has its own processes in place of stick movement and drag. Later states may pick theirs with flags
        if (character.StateMachine.IsInState(CharacterStateId.Hurt))
        {
            ApplyKnockbackDissipation();
            ApplyDI();
        }
        // Any stick input leaves Stop before this runs, so there's only the slide
        else if (character.StateMachine.IsInState(CharacterStateId.Stop))
        {
            ApplyDrag(Data.Ground.StopFriction);
        }
        // Grounded attacks take no stick movement, they only slide. Aerials keep the usual movement
        else if (character.StateMachine.IsInState(CharacterStateId.Attack) && IsGrounded)
        {
            ApplyDrag(Data.Ground.SlideFriction);
        }
        else
        {
            StandardMovement();
            StandardDrag();
        }

        // Apply gravity
        StandardGravity();
    }
}

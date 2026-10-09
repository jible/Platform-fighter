// A character's movement: walking, jumping, drag and gravity, all as changes to the character's velocity.
// Plain C#, made and owned by the character. States call into it, and the character ticks it after its state machine,
// so a jump impulse from a state lands before gravity and clamping
public class CharacterMovement
{
    private readonly Character character;
    // Set by a downward collision during the physics step, read by states on the next tick
    private readonly SerializableProperty<bool> grounded = new();
    public bool IsGrounded => grounded.Value;
    private readonly SerializableProperty<int> usedAirJumps = new();
    private CharacterData Data => character.Data;
    // Horizontal max depends on whether we're grounded, vertical max is the fall speed cap
    private DMVector MaxVelocity => new(IsGrounded ? Data.Ground.MaxVelocity : Data.Air.MaxVelocity, Data.Air.MaxFallVelocity);
    // Stick x in the range [-1, 1]
    private DM64 InputX => character.InputHandler.LeftStick().x / StickState.MAX_STICK_AXIS_VALUE;
    // Turns a per second rate (acceleration, friction) into how much of it applies this tick
    private static DM64 PerTick(DM64 perSecond) => perSecond / TickManager.TicksPerSecond;

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
            grounded.Value = true;
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

    public void ApplyDrag()
    {
        DM64 friction = IsGrounded ? Data.Ground.Friction : Data.Air.Friction;
        character.velocity.Value.x -= character.velocity.Value.x * PerTick(friction);
    }

    public void StandardGravity()
    {
        if (IsGrounded) return;
        ref DMVector velocity = ref character.velocity.Value;
        velocity.y = DM64.Max(velocity.y - PerTick(Data.Air.FallAcceleration), -Data.Air.MaxFallVelocity);
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
        StandardMovement();
        StandardDrag();

        // Apply gravity
        StandardGravity();

        // Cleared last so everything above sees the previous physics step's result.
        // The physics step after this sets it again if we're still standing on something
        grounded.Value = false;

    }
}

using Godot;
using System;

// Draws a character. Display only: it is told what to show after the ticks run, and never
// keeps its own clock, so a rollback is fixed up by the next Show with no extra work.
// One implementation per VisualKind, each a node the character adds as its child
public interface ICharacterView
{
    // animation is a path relative to the character's folder, see CharacterFiles.AnimationName.
    // grid is how the state cuts its sheet, and sprite the one to show, numbered left to right then top to bottom.
    // A state with no data passes a null grid and its ticks as sprite, and the view steps through the sheet on its own
    void Show(string animation, SpriteGrid? grid, int sprite, Character.Direction facing);
    // Distinct images the animation steps through, like a sprite sheet's frames, so an editor can step one at a time.
    // 0 when the animation has none, or doesn't step, as with a model
    int FrameCount(string animation);
}

// Owns a character's state machine and holds every reference its states need.
// Built in code, with no scene: whatever spawns it calls Configure, then adds it under the world root.
// It is its own physics body, so it must be a root of the simulation. Its only child node is the view
public partial class Character : PhysicsObject, ITickable, IEntity
{
    public enum Direction
    {
        Left,Right
    }

    public byte EntityId { get; set; }
    // Not named Input, which would hide Godot's Input class inside Character
    public InputHandler InputHandler { get; private set; }
    public int PlayerIndex { get; private set; }
    public CharacterId CharacterId { get; private set; }
    public CharacterData Data { get; private set; }
    // Hitboxes and the animation each state plays, from the character's files
    public CharacterDefinition Definition { get; private set; }
    // Null when the character's VisualKind isn't supported
    public ICharacterView View { get; private set; }
    public CharacterStateMachine StateMachine { get; private set; }
    public CharacterMovement Movement { get; private set; }
    // Starts attacks from input, and remembers which one the Attack state is playing
    public AttackHandler Attacks { get; private set; }
    public LifeData Life { get; private set; }

    // Fired by the Off state when the character loses their last life. Fires inside the tick, and again when a
    // rollback resimulates it, so listeners must only change rollback state
    public event Action<Character> OutOfLives;

    // The most recent hit taken, for the hurt states to read, since states hold no data
    private readonly SerializableProperty<ReceivedHit> lastHit = new();
    public ReceivedHit LastHit => lastHit.Value;

    // Where the character spawned, and respawns. Set once when configured, so not rollback state
    private DMVector spawnPosition;

    // Set when the character is configured
    private readonly SerializableProperty<Direction> _direction = new();
    public Direction direction // Each state directly handles manipulating direction
    {
        get => _direction.Value;
        set => _direction.Value = value;
    }

    // Who this character's current state has already hit, one per hit group index of the current state.
    // Sized by the definition when configured. Written by the HitboxManager and cleared when a state is entered
    public HitGroup[] HitGroups { get; private set; }

    // Called by whatever spawns the character, before it's added under the world root.
    // spawnPosition is in world space, since a character is always a root
    public void Configure(CharacterId characterId, int playerIndex, InputManager inputManager, DMVector spawnPosition, int lives, Direction direction = Direction.Left)
    {
        CharacterId = characterId;
        Data = CharacterFiles.LoadData(characterId);
        Definition = CharacterFiles.LoadDefinition(characterId);
        // Rollback state, so made here, before the world is built, and never resized
        HitGroups = HitGroup.CreateSet(Definition.MaxHitGroups);
        PlayerIndex = playerIndex;

        // The physics server reads these when the world is built
        shape = new Square { size = Data.Body.Size };
        objectType = ObjectType.CollisionObject;
        Layer = CollisionLayers.Characters;
        Mask = CollisionLayers.Stage;
        SimTransform.LocalPosition = spawnPosition;
        this.spawnPosition = spawnPosition;

        View = CharacterViews.Create(characterId, Data);
        if (View is Node viewNode) AddChild(viewNode);
        InputHandler = new InputHandler(playerIndex, inputManager);
        Movement = new CharacterMovement(this);
        Attacks = new AttackHandler(this, InputHandler);
        Life = new LifeData(lives);
        Life.HealthDepleted += () => StateMachine.ChangeState(CharacterStateId.Dying);
        // Last, since entering the first state can use everything above
        StateMachine = new CharacterStateMachine(this, CreateStates(), CharacterStateId.Idle);
        this.direction = direction;
    }

    // One of each state. A character with its own version of a state can swap it in here
    private static CharacterState[] CreateStates() => new CharacterState[]
    {
        new IdleState(),
        new WalkState(),
        new JumpSquatState(),
        new JumpState(),
        new FallState(),
        new LandState(),
        new TurnState(),
        new AttackState(),
        new HurtStartState(),
        new HurtState(),
        new DyingState(),
        new OffState(),
    };

    public void Tick()
    {
        StateMachine.Tick();
        // An off character is out of play, so nothing moves them until they respawn
        if (!StateMachine.IsInState(CharacterStateId.Off)) Movement.Tick();
    }

    // Called by the HitboxManager at the end of a tick, after every hit for the tick has been found.
    // Runs again when a rollback resimulates the tick, so anything done here must be rolled back state
    public void ReceiveHit(Character attacker, HitDefinition hit)
    {
        // Already dying or off, so the hit does nothing
        if (!Life.IsAlive) return;

        Life.ChangeHealth(-hit.Damage);
        // Placeholder until there's a health display
        GD.Print($"{Name} was hit by {attacker.Name} for {hit.Damage}, {Life.Health} health left");
        // The hit was fatal, and the character is already Dying
        if (!Life.IsAlive) return;

        // Knockback is authored as if the attacker faces right
        lastHit.Value = new ReceivedHit { Knockback = hit.KnockbackFacing(attacker.direction) };
        // Reentered, so a hit during HurtStart starts it over with the new knockback
        StateMachine.ChangeState(CharacterStateId.HurtStart, reenter: true);
    }

    // Called by the Off state when the character leaves play
    public void TurnOff()
    {
        velocity.Value = DMVector.zero;
        // The physics server skips inactive bodies, so an off character can't land on anything
        isActive = false;
    }

    // Called by the Off state when there's a life left to play. Back where they first spawned, at full health
    public void Respawn()
    {
        SimTransform.LocalPosition = spawnPosition;
        velocity.Value = DMVector.zero;
        isActive = true;
        Life.RestoreHealth();
        StateMachine.ChangeState(CharacterStateId.Fall);
    }

    // Called by the Off state when the last life is lost
    public void NotifyOutOfLives() => OutOfLives?.Invoke(this);

    // After the play manager ticks, so it draws the final state of this frame, including any rollback
    protected override void OnProcess(double delta)
    {
        if (View == null) return;

        CharacterStateId stateId = StateMachine.CurrentStateId;
        // Set every frame, since a rollback can move the character into or out of the Off state
        if (View is Node3D viewNode) viewNode.Visible = stateId != CharacterStateId.Off;
        StateDefinition state = StateMachine.CurrentDefinition;
        if (state != null)
        {
            View.Show(state.AnimationName, state.Grid, StateMachine.CurrentSprite, direction);
        }
        else
        {
            // No authored data yet, so play the sheet named after the state
            View.Show(CharacterFiles.AnimationName(stateId.ToString()), null, (int)StateMachine.TicksInState, direction);
        }
    }
}

// What the character keeps of the last hit it took. Unmanaged, so it fits in a SerializableProperty
public struct ReceivedHit
{
    // Already mirrored for the attacker's facing
    public DMVector Knockback;
}

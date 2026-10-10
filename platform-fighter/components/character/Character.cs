using Godot;

// Draws a character. Display only: it is told what to show after the ticks run, and never
// keeps its own clock, so a rollback is fixed up by the next Show with no extra work.
// One implementation per VisualKind, each a node the character adds as its child
public interface ICharacterView
{
    // frame is a tick within the animation, already looped or held by the state.
    // length is the state's length in ticks, or 0 when the state has no authored data
    void Show(string animation, int frame, int length, Character.Direction facing);
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
    public CharacterData Data { get; private set; }
    // Hitboxes and the animation each state plays, from the character's files
    public CharacterDefinition Definition { get; private set; }
    // Null when the character's VisualKind isn't supported
    public ICharacterView View { get; private set; }
    public CharacterStateMachine StateMachine { get; private set; }
    public CharacterMovement Movement { get; private set; }

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
    public void Configure(CharacterId characterId, int playerIndex, InputManager inputManager, DMVector spawnPosition, Direction direction = Direction.Left)
    {
        Data = Roster.Get(characterId);
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

        View = CharacterViews.Create(characterId);
        if (View is Node viewNode) AddChild(viewNode);
        InputHandler = new InputHandler(playerIndex, inputManager);
        Movement = new CharacterMovement(this);
        // Last, since entering the first state can use everything above
        StateMachine = new CharacterStateMachine(this, CreateStates(), CharacterStateId.Idle);
        this.direction = direction;
    }

    // One of each state. A character with its own version of a state can swap it in here
    private static CharacterState[] CreateStates() => new CharacterState[]
    {
        new IdleState(),
        new WalkState(),
        new JumpState(),
        new FallState(),
        new TurnState(),
    };

    public void Tick()
    {
        StateMachine.Tick();
        Movement.Tick();
    }

    // Called by the HitboxManager at the end of a tick, after every hit for the tick has been found.
    // Runs again when a rollback resimulates the tick, so anything done here must be rolled back state
    public void ReceiveHit(Character attacker, HitDefinition hit)
    {
        // Placeholder until there's damage and a hitstun state to apply it to
        GD.Print($"{Name} was hit by {attacker.Name} for {hit.Damage}");
    }

    // After the play manager ticks, so it draws the final state of this frame, including any rollback
    protected override void OnProcess(double delta)
    {
        if (View == null) return;

        CharacterStateId stateId = StateMachine.CurrentStateId;
        StateDefinition state = Definition.GetState(stateId);
        if (state != null)
        {
            View.Show(state.AnimationName, state.FrameAt(StateMachine.TicksInState), state.Length, direction);
        }
        else
        {
            // No authored data yet, so play the sheet named after the state
            View.Show(stateId.ToString(), (int)StateMachine.TicksInState, 0, direction);
        }
    }
}

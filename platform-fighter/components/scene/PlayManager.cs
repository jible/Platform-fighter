using Godot;

// Sets up a match and runs it: fills the holders under the world root, then builds the deterministic world
// from them and drives it every frame. The world only simulates what it's given; deciding what's in the match is this class's job
public partial class PlayManager : Node3D
{
    [Export] public InputManager InputManager;
    // Everything under this is collected into the deterministic world
    [Export] public Node3D WorldRoot;
    [Export] public StageHolder StageHolder;
    [Export] public CharacterHolder CharacterHolder;
    [Export] public StageId Stage = StageId.Test;

    // For testing without the menus: when no players were set up before this scene, one keyboard player is added per entry.
    // Player 1 gets the Keyboard 1 tag and player 2 Keyboard 2, so two people can share a keyboard
    [Export] public Godot.Collections.Array<CharacterId> DebugPlayers = new() { CharacterId.Platty };

    // Null until _Ready, so nothing ticks before every object exists
    private DeterministicWorld world;

    public override void _Ready()
    {
        // Before the deterministic nodes, so a frame's ticks run before they draw it
        ProcessPriority = -1;

        if (!HasPlayers()) AddDebugPlayers();

        // Clears rollback state left from before this scene. Rollback state registers when it's constructed,
        // so this has to come before anything below makes a character or stage block
        SerializableDataManager.Reset();
        StageHolder.LoadStage(Stage);
        CharacterHolder.SpawnCharacters(InputManager, StageHolder);

        world = new DeterministicWorld(WorldRoot, InputManager);
        InputManager.tickManager = world.TickManager;
    }

    public override void _Process(double delta)
    {
        world?.Update(delta);
    }

    public override void _ExitTree()
    {
        world?.Dispose();
        world = null;
    }

    private static bool HasPlayers()
    {
        foreach (var player in PlayerManager.GlobalInstance.AllPlayers)
        {
            if (player != null) return true;
        }
        return false;
    }

    private void AddDebugPlayers()
    {
        for (int playerNumber = 0; playerNumber < DebugPlayers.Count && playerNumber < PlayerManager.MaxPlayerCount; playerNumber++)
        {
            var profile = new PlayerProfile { Character = DebugPlayers[playerNumber] };
            // Any device number but -1 reads as local. Keyboards ignore it, since every keyboard is one device
            profile.Configure(playerNumber, 0, PlayerProfile.ControllerTypes.KEYBOARD);
            if (playerNumber == 1) profile.SetTag(TagManager.Keyboard2Id);
            PlayerManager.GlobalInstance.AllPlayers[playerNumber] = profile;
        }
    }
}

using Godot;
using System.Linq;

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
    // Every player starts with this many
    [Export] public int Lives = 3;

    // For testing without the menus: when no players were set up before this scene, one keyboard player is added per entry.
    // Player 1 gets the Keyboard 1 tag and player 2 Keyboard 2, so two people can share a keyboard
    [Export] public Godot.Collections.Array<CharacterId> DebugPlayers = new() { CharacterId.Platty };

    // Null until _Ready, so nothing ticks before every object exists
    private DeterministicWorld world;

    // Null until the match ends. Once it's set, the world stops ticking
    public MatchResults Results { get; private set; }
    public event System.Action<MatchResults> MatchEnded;

    public override void _Ready()
    {
        // Before the deterministic nodes, so a frame's ticks run before they draw it
        ProcessPriority = -1;

        if (!HasPlayers()) AddDebugPlayers();

        // Clears rollback state left from before this scene. Rollback state registers when it's constructed,
        // so this has to come before anything below makes a character or stage block
        SerializableDataManager.Reset();
        StageHolder.LoadStage(Stage);
        CharacterHolder.SpawnCharacters(InputManager, StageHolder, Lives);
        foreach (var character in CharacterHolder.Characters)
        {
            if (character != null) character.OutOfLives += OnOutOfLives;
        }

        world = new DeterministicWorld(WorldRoot, InputManager);
        InputManager.tickManager = world.TickManager;

        // Game systems the world doesn't know about. Hits resolve in player number order, wherever the characters sit in the tree
        var hitboxManager = new HitboxManager(world.All<Character>().OrderBy(character => character.PlayerIndex).ToArray());
        world.AddSystem(hitboxManager);
        world.AddRenderer(new RuntimeBoxRenderer(hitboxManager, world.BoxDrawer));
    }

    public override void _Process(double delta)
    {
        if (Results != null) return;

        world?.Update(delta);
        // Checked after the update, so a rollback that undid the last elimination doesn't end the match
        if (world != null && PlayersStillIn() == 0) EndMatch();
    }

    // Runs inside the tick, and again when a rollback resimulates it, so it only writes rollback state.
    // The player is placed behind everyone still in, so the first out of four places 4th.
    // Players out on the same tick are placed in player number order, the lowest number placing worst.
    // Once one player is left, they win
    private void OnOutOfLives(Character character)
    {
        // Already placed as the winner, by someone else going out earlier this tick
        if (character.Life.Placement != 0) return;
        character.Life.Placement = PlayersStillIn();

        if (PlayersStillIn() != 1) return;
        foreach (var other in CharacterHolder.Characters)
        {
            if (other != null && other.Life.Placement == 0) other.Life.Placement = 1;
        }
    }

    private int PlayersStillIn()
    {
        int count = 0;
        foreach (var character in CharacterHolder.Characters)
        {
            if (character != null && character.Life.Placement == 0) count++;
        }
        return count;
    }

    private void EndMatch()
    {
        Results = new MatchResults();
        for (int playerNumber = 0; playerNumber < CharacterHolder.Characters.Length; playerNumber++)
        {
            Character character = CharacterHolder.Characters[playerNumber];
            if (character == null) continue;
            Results.Players[playerNumber] = new PlayerMatchResult
            {
                Placement = character.Life.Placement,
                Character = character.CharacterId,
            };
            GD.Print($"Player {playerNumber + 1} ({character.CharacterId}) placed {character.Life.Placement}");
        }
        MatchEnded?.Invoke(Results);
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

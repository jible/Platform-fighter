using Godot;

// Spawns a character for every player and holds them. Sits under the world root, so the world collects the characters,
// but the world never spawns or knows about players itself
public partial class CharacterHolder : Node3D
{
    // Indexed by player number, null for empty slots
    public readonly Character[] Characters = new Character[PlayerManager.MaxPlayerCount];

    // Called by the play manager before the world is built.
    // Spawns in player number order, so the tick order is the same on every machine
    public void SpawnCharacters(InputManager inputManager, StageHolder stage, int lives)
    {
        for (int playerNumber = 0; playerNumber < PlayerManager.MaxPlayerCount; playerNumber++)
        {
            PlayerProfile profile = PlayerManager.GlobalInstance.AllPlayers[playerNumber];
            if (profile == null) continue;

            DMVector spawn = stage.SpawnPoint(playerNumber);
            var character = new Character { Name = $"Player {playerNumber + 1} ({profile.Character})" };
            // Face the middle of the stage
            var facing = spawn.x > 0 ? Character.Direction.Left : Character.Direction.Right;
            character.Configure(profile.Character, playerNumber, inputManager, spawn, lives, facing);
            AddChild(character);

            Characters[playerNumber] = character;
            profile.Instance = character;
        }
    }
}

// Kills any character outside the stage's perimeter. Checked after physics, against each character's final position for the tick.
// Holds no state between ticks, and killing only changes the character's rollback state, so it rolls back with them.
// A world system, added by the PlayManager
public class PerimeterEnforcer : IWorldSystem
{
    private readonly StagePerimeter perimeter;
    // In player number order
    private readonly Character[] characters;

    public PerimeterEnforcer(StagePerimeter perimeter, Character[] characters)
    {
        this.perimeter = perimeter;
        this.characters = characters;
    }

    public void Tick()
    {
        foreach (var character in characters)
        {
            // Already dying or off, so they stay where they left until they respawn
            if (!character.Life.IsAlive) continue;
            DMVector position = character.SimTransform.GlobalPosition;
            if (!perimeter.Contains(position) && !IsSafeOffTop(character, position)) character.Life.Kill();
        }
    }

    // Off the top only kills a launched character, so jumping up there on their own just falls back down
    private bool IsSafeOffTop(Character character, DMVector position) =>
        position.y > perimeter.Max.y
        && position.x >= perimeter.Min.x && position.x <= perimeter.Max.x
        && !character.StateMachine.IsInState(CharacterStateId.Hurt);
}

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
            if (!perimeter.Contains(character.SimTransform.GlobalPosition)) character.Life.Kill();
        }
    }
}

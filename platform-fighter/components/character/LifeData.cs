using System;

// A character's health and lives for the match. Plain C#, made and owned by the character.
// All rollback state, so it must only change inside a tick, and anything listening to HealthDepleted
// must only change rollback state too, since the event fires again when a rollback resimulates the tick
public class LifeData
{
    public const int MaxHealth = 100;

    private readonly SerializableProperty<int> health = new();
    private readonly SerializableProperty<int> lives = new();
    // Where the character finished, 1 for the winner. 0 while they're still in the match.
    // Set by the PlayManager, since it takes knowing how many other players are still in
    private readonly SerializableProperty<int> placement = new();

    public int Health => health.Value;
    // Includes the life being played, so it's 0 once the last one is lost
    public int Lives => lives.Value;
    public bool IsAlive => health.Value > 0;
    public bool HasLivesLeft => lives.Value > 0;
    public int Placement
    {
        get => placement.Value;
        set => placement.Value = value;
    }

    // Fires once, on the change that takes health to 0 or below
    public event Action HealthDepleted;

    public LifeData(int startingLives)
    {
        health.Value = MaxHealth;
        lives.Value = startingLives;
    }

    // Negative to lower health, positive to raise it. Does nothing once health is depleted, so a dead
    // character can't be hit again or healed back. Never goes above MaxHealth
    public void ChangeHealth(int amount)
    {
        if (!IsAlive) return;

        health.Value = Math.Min(health.Value + amount, MaxHealth);
        if (!IsAlive) HealthDepleted?.Invoke();
    }

    // Takes health straight to 0, for deaths that aren't from damage, like leaving the stage's perimeter
    public void Kill() => ChangeHealth(-health.Value);

    public void LoseLife()
    {
        if (lives.Value > 0) lives.Value--;
    }

    // Back to full, for respawning
    public void RestoreHealth()
    {
        health.Value = MaxHealth;
    }
}

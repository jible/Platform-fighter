// Runtime side of a hit group: which entities it has hit since it was last cleared.
// Definitions are shared by every instance of a character, so this memory lives on the character instead,
// one per group index, and is reused by whichever state the character is in
public class HitGroup
{
    // One bit per entity in a ulong
    public const int MaxEntities = 64;

    // Bit n is set once entity n has been hit
    private readonly SerializableProperty<ulong> hitEntities = new();

    public bool HasHit(IEntity entity) => (hitEntities.Value & Bit(entity)) != 0;
    public void MarkHit(IEntity entity) => hitEntities.Value |= Bit(entity);
    public void Clear() => hitEntities.Value = 0;

    private static ulong Bit(IEntity entity) => 1UL << entity.EntityId;

    public static HitGroup[] CreateSet(int count)
    {
        var groups = new HitGroup[count];
        for (int i = 0; i < count; i++)
        {
            groups[i] = new HitGroup();
        }
        return groups;
    }
}

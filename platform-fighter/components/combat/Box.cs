// An active hit or hurt box on the current tick: where it is, and who it belongs to.
// Built fresh by the HitboxManager from the owner's frame data every tick, so it holds nothing between ticks
public readonly struct Box
{
    public readonly BoxType Type;
    public readonly Character Owner;
    public readonly DMVector Center;
    public readonly DMVector Size;
    // Hitboxes only, null on hurtboxes.
    // The owner's runtime memory for this box's group, shared by every box in the group
    public readonly HitGroup Group;
    // What a hit from this box deals
    public readonly HitDefinition Hit;

    private Box(BoxType type, Character owner, DMVector center, DMVector size, HitGroup group, HitDefinition hit)
    {
        Type = type;
        Owner = owner;
        Center = center;
        Size = size;
        Group = group;
        Hit = hit;
    }

    public static Box Hitbox(Character owner, DMVector center, DMVector size, HitGroup group, HitDefinition hit)
        => new(BoxType.Hitbox, owner, center, size, group, hit);

    public static Box Hurtbox(Character owner, DMVector center, DMVector size)
        => new(BoxType.Hurtbox, owner, center, size, null, null);

    public bool Overlaps(Box other) => Overlap.Boxes(Center, Size, other.Center, other.Size);
}

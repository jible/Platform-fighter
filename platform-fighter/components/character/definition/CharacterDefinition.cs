using System.Collections.Generic;

// Runtime form of a character, built once from its files by CharacterDefinitionBuilder.
// Everything is expanded to one value per frame, so a tick only ever indexes arrays.
// Read only after building, so one definition can be shared by every instance of the character.

public sealed class CharacterDefinition
{
    public readonly StateDefinition[] States;
    // Most hit groups any one state uses, counting hitboxes that hit on their own.
    // Each character holds this many HitGroups, reused by whichever state it's in
    public readonly int MaxHitGroups;
    private readonly Dictionary<string, StateDefinition> statesByName = new();

    public CharacterDefinition(StateDefinition[] states)
    {
        States = states;
        foreach (var state in states)
        {
            statesByName[state.Name] = state;
            foreach (var box in state.Boxes)
            {
                if (box.HitGroupIndex + 1 > MaxHitGroups) MaxHitGroups = box.HitGroupIndex + 1;
            }
        }
    }

    // Null when the state has no authored data
    public StateDefinition GetState(string name)
    {
        statesByName.TryGetValue(name, out StateDefinition state);
        return state;
    }

    // Looked up by name, matching how the file stores it. Cache the result if this gets hot
    public StateDefinition GetState(CharacterStateId id) => GetState(id.ToString());
}

public sealed class StateDefinition
{
    public readonly string Name;
    public readonly string Animation;
    public readonly int Length;
    public readonly bool Loop;
    public readonly BoxDefinition[] Boxes;

    public StateDefinition(string name, string animation, int length, bool loop, BoxDefinition[] boxes)
    {
        Name = name;
        Animation = animation;
        Length = length;
        Loop = loop;
        Boxes = boxes;
    }

    // The sheet or clip to play. A state with no animation set plays the one named after it
    public string AnimationName => string.IsNullOrEmpty(Animation) ? Name : Animation;

    // Maps ticks spent in the state to a frame of its data
    public int FrameAt(uint ticksInState)
    {
        if (Loop) return (int)(ticksInState % (uint)Length);
        return ticksInState >= Length ? Length - 1 : (int)ticksInState;
    }
}

// What a hitbox deals. Each hitbox has its own, so a sweet spot and a sour spot in one hit group can differ
public sealed class HitDefinition
{
    public readonly int Damage;
    public readonly int Hitstun;
    public readonly DMVector Knockback;

    public HitDefinition(int damage, int hitstun, DMVector knockback)
    {
        Damage = damage;
        Hitstun = hitstun;
        Knockback = knockback;
    }

    public DMVector KnockbackFacing(Character.Direction facing) => BoxDefinition.Mirror(Knockback, facing);
}

public sealed class BoxDefinition
{
    public readonly int Id;
    public readonly BoxType Type;
    // Index of the box's hit group within its state. Boxes in a group share one hit per target.
    // Every hitbox has one, since a hitbox without an authored group gets its own. -1 on other boxes
    public readonly int HitGroupIndex;
    // Hitboxes only, null otherwise
    public readonly HitDefinition Hit;

    // All indexed by frame and as long as the state
    private readonly DMVector[] offsets;
    private readonly DMVector[] sizes;
    private readonly bool[] active;
    // Bone position per frame, null when the box follows the root
    private readonly DMVector[] bonePositions;

    public BoxDefinition(int id, BoxType type, int hitGroupIndex, HitDefinition hit, DMVector[] offsets, DMVector[] sizes, bool[] active, DMVector[] bonePositions)
    {
        Id = id;
        Type = type;
        HitGroupIndex = hitGroupIndex;
        Hit = hit;
        this.offsets = offsets;
        this.sizes = sizes;
        this.active = active;
        this.bonePositions = bonePositions;
    }

    public bool IsActive(int frame) => active[frame];

    public DMVector SizeAt(int frame) => sizes[frame];

    // Center of the box relative to the character root, facing right
    public DMVector CenterAt(int frame)
    {
        DMVector center = offsets[frame];
        if (bonePositions != null)
        {
            // The bake can be shorter than the state if the animation changed, hold its last frame
            center += bonePositions[frame < bonePositions.Length ? frame : bonePositions.Length - 1];
        }
        return center;
    }

    public DMVector CenterAt(int frame, Character.Direction facing) => Mirror(CenterAt(frame), facing);

    // Data is authored facing right
    public static DMVector Mirror(DMVector v, Character.Direction facing)
    {
        return facing == Character.Direction.Right ? v : new DMVector(-v.x, v.y);
    }
}

using System;
using System.Collections.Generic;

// Runtime form of a character, built once from its files by CharacterDefinitionBuilder.
// Everything is expanded to one value per frame, so a tick only ever indexes arrays.
// Read only after building, so one definition can be shared by every instance of the character.

public sealed class CharacterDefinition
{
    public readonly StateDefinition[] States;
    // Sorted by name, and each one's Index is its place here, so every machine numbers them the same
    public readonly AttackDefinition[] Attacks;
    // Most hit groups any one state or attack uses, counting hitboxes that hit on their own.
    // Each character holds this many HitGroups, reused by whichever state it's in
    public readonly int MaxHitGroups;
    private readonly Dictionary<string, StateDefinition> statesByName = new();
    private readonly Dictionary<AttackInput, AttackDefinition> attacksByInput = new();

    // attacks must be sorted by name with unique inputs, as the builder makes them
    public CharacterDefinition(StateDefinition[] states, AttackDefinition[] attacks)
    {
        States = states;
        Attacks = attacks;
        foreach (var state in states)
        {
            statesByName[state.Name] = state;
            MaxHitGroups = Math.Max(MaxHitGroups, HitGroupsUsed(state));
        }
        foreach (var attack in attacks)
        {
            attacksByInput[attack.Input] = attack;
            MaxHitGroups = Math.Max(MaxHitGroups, HitGroupsUsed(attack));
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

    // A state or attack by name, for the editor, which lists both together. Null when neither has data
    public StateDefinition GetStateOrAttack(string name)
    {
        StateDefinition state = GetState(name);
        if (state != null) return state;
        foreach (var attack in Attacks)
        {
            if (attack.Name == name) return attack;
        }
        return null;
    }

    // Null when the character has no attack for the input
    public AttackDefinition FindAttack(AttackInput input)
    {
        attacksByInput.TryGetValue(input, out AttackDefinition attack);
        return attack;
    }

    private static int HitGroupsUsed(StateDefinition state)
    {
        int used = 0;
        foreach (var box in state.Boxes)
        {
            used = Math.Max(used, box.HitGroupIndex + 1);
        }
        return used;
    }
}

// Not sealed, since an attack is a state with more to it
public class StateDefinition
{
    public readonly string Name;
    public readonly string Animation;
    public readonly int Length;
    public readonly bool Loop;
    public readonly BoxDefinition[] Boxes;
    // Null when the state has no sprite sequence, so the view spreads the sheet evenly instead
    public readonly SpriteTrack Sprite;

    public StateDefinition(string name, string animation, int length, bool loop, BoxDefinition[] boxes, SpriteTrack sprite)
    {
        Name = name;
        Animation = animation;
        Length = length;
        Loop = loop;
        Boxes = boxes;
        Sprite = sprite;
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

// A move. Played through the shared Attack state, which hands this to everything that reads the current state
public sealed class AttackDefinition : StateDefinition
{
    // Place in CharacterDefinition.Attacks, which is what rollback stores
    public readonly int Index;
    public readonly AttackInput Input;
    public readonly MovementType Movement;
    // Ends in the Land state on touching the ground
    public readonly bool CancelOnLand;

    public AttackDefinition(int index, AttackInput input, MovementType movement, bool cancelOnLand, StateDefinition state)
        : base(state.Name, state.Animation, state.Length, state.Loop, state.Boxes, state.Sprite)
    {
        Index = index;
        Input = input;
        Movement = movement;
        CancelOnLand = cancelOnLand;
    }
}

// The frames of the state's sheet in the order they play, spread evenly over however long the animation plays.
// That's usually the state's length, but a state like hitstun can stretch it over a time decided at runtime
public sealed class SpriteTrack
{
    // The sheet's grid, for Sprite3D's Hframes and Vframes
    public readonly int Columns;
    public readonly int Rows;
    // Never empty. Always within the grid
    private readonly int[] sequence;

    public SpriteTrack(int columns, int rows, int[] sequence)
    {
        Columns = columns;
        Rows = rows;
        this.sequence = sequence;
    }

    public int Count => sequence.Length;

    // frame is within 0..duration - 1
    public int SpriteAt(int frame, int duration) => sequence[SpriteSequenceMath.EntryAt(frame, sequence.Length, duration)];
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

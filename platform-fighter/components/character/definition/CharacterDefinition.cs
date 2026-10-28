using System;
using System.Collections.Generic;

// Runtime form of a character, built once from its files by CharacterDefinitionBuilder.
// Everything is expanded to plain arrays, so a tick only ever indexes them.
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
            MaxHitGroups = Math.Max(MaxHitGroups, state.HitGroupCount);
        }
        foreach (var attack in attacks)
        {
            attacksByInput[attack.Input] = attack;
            MaxHitGroups = Math.Max(MaxHitGroups, attack.HitGroupCount);
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
}

// Not sealed, since an attack is a state with more to it
public class StateDefinition
{
    public readonly string Name;
    public readonly SpriteGrid Grid;
    // In file order. Never empty, and the first always plays first
    public readonly PhaseDefinition[] Phases;
    // Hit groups its hitboxes use, counting hitboxes that hit on their own
    public readonly int HitGroupCount;
    // Indexed by sprite. Never empty, and an entry with no boxes is an empty array
    private readonly BoxDefinition[][] frames;

    public StateDefinition(string name, SpriteGrid grid, PhaseDefinition[] phases, BoxDefinition[][] frames, int hitGroupCount)
    {
        Name = name;
        Grid = grid;
        Phases = phases;
        this.frames = frames;
        HitGroupCount = hitGroupCount;
    }

    protected StateDefinition(StateDefinition state)
        : this(state.Name, state.Grid, state.Phases, state.frames, state.HitGroupCount) { }

    // The sheet to play, found by the state's name
    public string AnimationName => CharacterFiles.AnimationName(Name);

    // Sprite showing on a tick of a phase. Past the phase's end it holds the last sprite
    public int SpriteAt(int phase, uint ticksInPhase) => Phases[Math.Min(phase, Phases.Length - 1)].SpriteAt(ticksInPhase);

    public BoxDefinition[] BoxesAt(int sprite) => frames[Math.Clamp(sprite, 0, frames.Length - 1)];
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
        : base(state)
    {
        Index = index;
        Input = input;
        Movement = movement;
        CancelOnLand = cancelOnLand;
    }
}

// A run of the state's sprites, spread evenly over its own length
public sealed class PhaseDefinition
{
    // Index of the phase to play when this one runs out, or EndsState
    public const int EndsState = -1;

    public readonly int Start;
    public readonly int SpriteCount;
    // Ticks, at least SpriteCount unless the file was edited by hand
    public readonly int Length;
    public readonly int Next;

    public PhaseDefinition(int start, int spriteCount, int length, int next)
    {
        Start = start;
        SpriteCount = spriteCount;
        Length = length;
        Next = next;
    }

    public int SpriteAt(uint ticksInPhase)
    {
        int tick = ticksInPhase >= Length ? Length - 1 : (int)ticksInPhase;
        return Start + SpriteSequenceMath.EntryAt(tick, SpriteCount, Length);
    }
}

// How a sheet is cut into sprites, for Sprite3D's Hframes and Vframes
public readonly struct SpriteGrid
{
    public readonly int Columns;
    public readonly int Rows;

    public SpriteGrid(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }

    public int Count => Columns * Rows;
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

// One box on one sprite, in world units
public sealed class BoxDefinition
{
    // Shared with copies of the box on other sprites, so the editor can follow it
    public readonly int Id;
    public readonly BoxType Type;
    // Index of the box's hit group within its state. Boxes in a group share one hit per target. -1 on non hitboxes
    public readonly int HitGroupIndex;
    // Hitboxes only, null otherwise
    public readonly HitDefinition Hit;
    // Relative to the character root, facing right
    public readonly DMVector Center;
    public readonly DMVector Size;

    public BoxDefinition(int id, BoxType type, int hitGroupIndex, HitDefinition hit, DMVector center, DMVector size)
    {
        Id = id;
        Type = type;
        HitGroupIndex = hitGroupIndex;
        Hit = hit;
        Center = center;
        Size = size;
    }

    public DMVector CenterFacing(Character.Direction facing) => Mirror(Center, facing);

    // Data is authored facing right
    public static DMVector Mirror(DMVector v, Character.Direction facing)
    {
        return facing == Character.Direction.Right ? v : new DMVector(-v.x, v.y);
    }
}

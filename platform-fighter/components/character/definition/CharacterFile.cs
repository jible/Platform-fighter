using System;
using System.Collections.Generic;

// The hand authored half of a character's JSON. Written by the hitbox editor, read once at load and
// built into a CharacterDefinition. Nothing reads these classes during a tick.
// Read and written with Newtonsoft through CharacterFileJson, which saves enums by name and DM64 as decimals.
// Positions are authored facing right and mirrored at runtime.

[Serializable]
public class CharacterFile
{
    // Bump when the layout changes, so old files can be upgraded on load
    public int Version = 1;
    // Keyed by the CharacterStateId name, not its number, so reordering the enum does not remap data
    public Dictionary<string, StateFile> States = new();
}

[Serializable]
public class StateFile
{
    // Sprite animation path or model clip name. Also picks which baked bone tracks the boxes follow
    public string Animation = "";
    // Ticks in one play through of the state
    public int Length = 1;
    public bool Loop;
    public List<HitGroupFile> HitGroups = new();
    public List<BoxFile> Boxes = new();
    // Handed to the next box created in this state, so ids stay stable when boxes are renamed or deleted
    public int NextBoxId;
    public int NextHitGroupId;
}

// Boxes in one group share a single hit, so a multi box attack only connects once per target.
// What the hit deals belongs to each box, and the best overlapping box in the group lands
[Serializable]
public class HitGroupFile
{
    public int Id;
    public string Name = "";
}

// Saved by name, so renaming a value breaks existing files
public enum BoxType
{
    // Can be hit
    Hurtbox,
    // Deals hits
    Hitbox,
    // Body collision that keeps characters apart
    Pushbox,
}

[Serializable]
public class BoxFile
{
    // Unique within its state only
    public int Id;
    // Editor display only
    public string Name = "";
    public BoxType Type;
    // Bone the offsets are relative to. Empty means the character root
    public string Bone = "";
    // Hit group id, or NoHitGroup. Only hitboxes use it
    public int HitGroup = NoHitGroup;
    // What the box deals. Only hitboxes use these
    public int Damage;
    // Ticks the target is stunned for
    public int Hitstun;
    // Facing right, mirrored with the attacker
    public DMVector Knockback;
    public List<BoxKey> Keys = new();
    // Frames the box exists on. A box with no ranges is never active
    public List<FrameRange> Active = new();

    public const int NoHitGroup = -1;
}

// How a key blends into the next key
// Saved by name, so renaming a value breaks existing files
public enum KeyInterpolation
{
    // Keep this key's values until the next key
    Hold,
    // Blend linearly toward the next key
    Linear,
}

[Serializable]
public struct BoxKey
{
    public int Frame;
    // Center of the box, relative to the bone (or root)
    public DMVector Offset;
    // Full width and height
    public DMVector Size;
    public KeyInterpolation Interpolation;
}

// Inclusive on both ends. Saved as [start, end]
[Serializable]
public struct FrameRange
{
    public int Start;
    public int End;

    public FrameRange(int start, int end)
    {
        Start = start;
        End = end;
    }
}

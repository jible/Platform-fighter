using System;
using System.Collections.Generic;

// The hand authored half of a character's JSON. Written by the hitbox editor, read once at load and
// built into a CharacterDefinition. Nothing reads these classes during a tick.
// Read and written with Newtonsoft through CharacterFileJson, which saves enums by name and DM64 as decimals.
// Positions are authored facing right and mirrored at runtime.
// Box offsets and sizes are in the character's box units: sprite pixels (y up, 0,0 at the sprite's center)
// for sprite characters, world units for models. The builder converts them, see VisualData.BoxUnit.

[Serializable]
public class CharacterFile
{
    // Bump when the layout changes, so old files can be upgraded on load
    public int Version = 1;
    // Keyed by the CharacterStateId name, not its number, so reordering the enum does not remap data
    public Dictionary<string, StateFile> States = new();
    // Keyed by attack name, which also names the sheet when Animation is empty, like a state.
    // Names are shared with states, so an attack can't be named after a CharacterStateId
    public Dictionary<string, AttackFile> Attacks = new();
}

[Serializable]
public class StateFile
{
    // Sprite animation path or model clip name. Also picks which baked bone tracks the boxes follow
    public string Animation = "";
    // How a sprite sheet is cut up and played. Null spreads a single row sheet evenly over the state
    public SpriteFile Sprite;
    // Ticks in one play through of the state
    public int Length = 1;
    public bool Loop;
    public List<HitGroupFile> HitGroups = new();
    public List<BoxFile> Boxes = new();
    // Handed to the next box created in this state, so ids stay stable when boxes are renamed or deleted
    public int NextBoxId;
    public int NextHitGroupId;
}

// A move, played by the shared Attack state. Everything a state has, plus what starts it
[Serializable]
public class AttackFile : StateFile
{
    public AttackInput Input;
    // Not read by CharacterMovement yet
    public MovementType Movement = MovementType.IgnoreInput;
}

// The input that starts an attack. Each input starts at most one of a character's attacks
[Serializable]
public struct AttackInput : IEquatable<AttackInput>
{
    public AttackButton Button;
    // Relative to facing, so the data never deals with sides
    public StickDirection Direction;
    public AttackContext Context;

    public AttackInput(AttackButton button, StickDirection direction, AttackContext context)
    {
        Button = button;
        Direction = direction;
        Context = context;
    }

    public bool Equals(AttackInput other) => Button == other.Button && Direction == other.Direction && Context == other.Context;
    public override bool Equals(object obj) => obj is AttackInput other && Equals(other);
    public override int GetHashCode() => ((int)Button * 8 + (int)Direction) * 8 + (int)Context;
    public override string ToString() => $"{Context} {Direction} {Button}";
}

// Saved by name, so renaming a value breaks existing files
public enum AttackButton
{
    // Light, see ControllerState.ButtonTypes.LIGHT
    A,
    // Special. Wins when both are pressed on the same tick
    B,
}

// Saved by name, so renaming a value breaks existing files
public enum StickDirection
{
    Neutral,
    Up,
    Down,
    Forward,
    Back,
}

// Saved by name, so renaming a value breaks existing files
public enum AttackContext
{
    Ground,
    Air,
}

// How a state moves the character. Saved by name, so renaming a value breaks existing files
public enum MovementType
{
    // Stick input, drag and gravity as normal
    Standard,
    // Velocity held at zero
    Freeze,
    // Drag and gravity, but the stick does nothing
    IgnoreInput,
    // Directional influence: the stick nudges knockback
    DI,
}

// The state's sheet (StateFile.Animation) as a grid of frames, numbered left to right then top to bottom from 0.
// Sequence is the frames in the order they play, each shown for the same time, so a repeated frame holds longer
[Serializable]
public class SpriteFile
{
    public int Columns = 1;
    public int Rows = 1;
    // Sequence entries per second. Rates that don't divide TicksPerSecond show some entries a tick longer
    public int FrameRate = 10;
    public List<int> Sequence = new();
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

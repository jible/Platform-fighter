using System;
using System.Collections.Generic;

// The hand authored half of a character's JSON. Written by the hitbox editor, read once at load and
// built into a CharacterDefinition. Nothing reads these classes during a tick.
// Read and written with Newtonsoft through CharacterFileJson, which saves enums by name and DM64 as decimals.
// Positions are authored facing right and mirrored at runtime.
// Box offsets and sizes are in the character's box units: sprite pixels (y up, 0,0 at the sprite's center)
// for sprite characters, world units for models. The builder converts them, see VisualData.BoxUnit.
// Everything is per sprite of the state's sheet. Models have no sprites yet, so they'll need frames of their own

[Serializable]
public class CharacterFile
{
    // Bump when the layout changes, so old files can be upgraded on load.
    // 3 split states into phases and gave each sprite its own boxes
    public int Version = CurrentVersion;
    public const int CurrentVersion = 3;
    // Keyed by the CharacterStateId name, not its number, so reordering the enum does not remap data
    public Dictionary<string, StateFile> States = new();
    // Keyed by attack name, which also names the sheet, like a state.
    // Names are shared with states, so an attack can't be named after a CharacterStateId
    public Dictionary<string, AttackFile> Attacks = new();
    // Override the character's BodyData.Size and VisualData.Offset, so the editor can tune them.
    // Null keeps the value from code, see CharacterFiles.LoadData
    public DMVector? BodySize;
    public DMVector? VisualOffset;
    // Override the character's movement data, so the property editor can tune it. Null keeps the section from code.
    // A section replaces the code's whole section, so a field missing from it takes the class default, not the code's value.
    // The property editor always saves every field, so that only matters for hand edited files
    public GroundMovementData Ground;
    public AirMovementData Air;
    public JumpData Jump;
    public CombatData Combat;
}

// A state's sheet is found by its name, see CharacterFiles.AnimationName, so it has no path of its own
[Serializable]
public class StateFile
{
    // How the sheet is cut up. Null until the grid is set, which plays a single row sheet on its own
    public SpriteFile Sprite;
    // In play order, covering every sprite of the sheet in order with no gaps. The first always plays first
    public List<PhaseFile> Phases = new();
    // One per sprite of the sheet, holding that sprite's boxes. Boxes never carry over from one sprite to the next
    public List<FrameFile> Frames = new();
    // Shared by every frame and phase, so boxes on different frames can share one hit
    public List<HitGroupFile> HitGroups = new();
    // Handed to the next box, group or phase created in this state, so ids stay stable when others are deleted
    public int NextBoxId;
    public int NextHitGroupId;
    public int NextPhaseId;
    // Leaves for the Fall state on any tick it's played off the ground, like a grounded attack carried off a ledge
    public bool CancelInAir;
}

// A run of the sheet's sprites, played over its own length.
// Its sprites are spread evenly over Length, so tuning how a phase feels never touches the art
[Serializable]
public class PhaseFile
{
    // Unique within its state only. What OnEnd targets point at, so renaming or moving a phase breaks nothing
    public int Id;
    // Editor display only
    public string Name = "";
    // Sprites it plays, inclusive on both ends
    public int Start;
    public int End;
    // Ticks it plays for. The editor keeps it at least the sprite count, so every sprite shows for a tick
    public int Length = 1;
    public PhaseEnd OnEnd = PhaseEnd.Next;
    // Phase id OnEnd goes to, for PhaseEnd.Phase only
    public int Target = NoPhase;

    public const int NoPhase = -1;

    public int SpriteCount => End - Start + 1;
}

// What a phase does when its length runs out. Saved by name, so renaming a value breaks existing files
public enum PhaseEnd
{
    // The next phase, or the state ends after the last one
    Next,
    // Plays itself again, which is how a state loops
    Restart,
    First,
    // The phase whose id is PhaseFile.Target
    Phase,
}

// Everything on one sprite of the sheet
[Serializable]
public class FrameFile
{
    public List<BoxFile> Boxes = new();
}

// A move, played by the shared Attack state. Everything a state has, plus what starts it
[Serializable]
public class AttackFile : StateFile
{
    public AttackInput Input;
    // Not read by CharacterMovement yet
    public MovementType Movement = MovementType.IgnoreInput;
    // Ends the attack in the Land state on touching the ground, for aerials. Anything special on landing,
    // like a landing hitbox, is up to whatever plays after it
    public bool CancelOnLand;
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

// The state's sheet as a grid of sprites, numbered left to right then top to bottom from 0
[Serializable]
public class SpriteFile
{
    public int Columns = 1;
    public int Rows = 1;
    // The sheet's size in pixels when the grid was last set, so the editor can tell when sprites were added or
    // removed and ask where, before the frame data drifts onto the wrong sprites. 0 when unknown
    public int SheetWidth;
    public int SheetHeight;

    public int Count => Math.Max(1, Columns) * Math.Max(1, Rows);
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

// One box on one sprite. Copying it to the next sprite keeps its id and name, so the editor can follow it
[Serializable]
public class BoxFile
{
    // Unique within its state only, shared by copies of the box on other sprites
    public int Id;
    // Editor display only
    public string Name = "";
    public BoxType Type;
    // Hit group id. Every hitbox has one, other boxes leave it at NoHitGroup
    public int HitGroup = NoHitGroup;
    // What the box deals. Only hitboxes use these
    public int Damage;
    // Facing right, mirrored with the attacker
    public DMVector Knockback;
    // How much more knockback the hit deals as the target's health runs out. 0 never grows, 1 doubles at no health.
    // Defaults to 1, so boxes saved before this existed load with it
    public DM64 KnockbackScaling = new DM64(1);
    // Center of the box, relative to the character root
    public DMVector Offset;
    // Full width and height
    public DMVector Size;

    public const int NoHitGroup = -1;
}

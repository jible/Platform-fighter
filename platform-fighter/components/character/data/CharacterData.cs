using System;

// Every character shares this shape, only the values differ.
// Defaults live on the fields, so a character only lists what it changes.
// The Roster's copy is the code's values. A character's file can override some of them, so play and the
// editor use the copy from CharacterFiles.LoadData instead
// Velocities are in units per second, accelerations in units per second per second.
// Only the physics server converts velocity to units per tick.
[Serializable]
public class CharacterData
{
    public GroundMovementData Ground = new();
    public AirMovementData Air = new();
    public JumpData Jump = new();
    public CombatData Combat = new();
    public BodyData Body = new();
    public VisualData Visuals = new();
    // Ticks a state lasts when it has no authored data, so states that end on their animation still end
    public int DefaultStateLength = 30;
}

// The character's own physics body, which collides with the stage
[Serializable]
public class BodyData
{
    // Full width and height in world units, centered on the character's position. Overridden by CharacterFile.BodySize
    public DMVector Size = new DMVector(2, 2);
}

public enum VisualKind
{
    // One sheet per animation, cut into a grid by the state's sprite sequence, see SpriteFile
    Sprite,
    // Not supported yet
    Model,
}

// How the character is drawn. Mostly display only, except PixelSize, which also scales a sprite character's boxes.
// The art lives with the character's other files, see CharacterFiles
[Serializable]
public class VisualData
{
    public VisualKind Kind = VisualKind.Sprite;
    // Played for any animation the character has no art for yet. Null shows nothing new
    public string DefaultAnimation;
    // Width of one frame in a sheet played without a sprite sequence. 0 means square frames, as wide as the sheet is tall
    public int SpriteFrameWidth = 0;
    // World units per pixel of a sprite sheet. A sprite character's boxes are authored in the same pixels,
    // so this also sizes them in play and must be a DM64. Changing it rescales art and boxes together
    public DM64 PixelSize = new DM64(1) / 100;
    // Where the art sits relative to the character's position, in box units like a box offset, so it lines up with
    // the boxes and body the same way in play and in the editor. Authored facing right and mirrored with the character.
    // Overridden by CharacterFile.VisualOffset
    public DMVector Offset = DMVector.zero;

    // World units per unit of an authored box offset or size: pixels for sprites, world units for models
    public DM64 BoxUnit => Kind == VisualKind.Sprite ? PixelSize : new DM64(1);
}

[Serializable]
public class GroundMovementData
{
    public DM64 Acceleration = new DM64(30);
    public DM64 MaxVelocity = new DM64(2);
    // Held for the whole Dash state, before Walk takes over at MaxVelocity
    public DM64 DashVelocity = new DM64(24);
    // How slippery the character is on the ground. Defaults are Platty's.
    // Each tick loses velocity * friction / 60, so from a given speed the slide covers about speed / friction units.
    // Must stay under 60, or the velocity flips
    public DM64 Friction = new DM64(6);
    // Used in place of Friction in the Stop state, after letting go of the stick while walking
    public DM64 StopFriction = new DM64(12);
    // Used in place of Friction during grounded attacks, crouching and landing. Meant to sit between Friction and StopFriction
    public DM64 SlideFriction = new DM64(8);
    // Acceleration is multiplied by this when pushing against the current velocity
    public DM64 TurnaroundMultiplier = new DM64(5);
}

[Serializable]
public class AirMovementData
{
    public DM64 Acceleration = new DM64(20);
    public DM64 MaxVelocity = new DM64(2);
    // Gravity
    public DM64 FallAcceleration = new DM64(10);
    public DM64 MaxFallVelocity = new DM64(2);
    // Works like the ground's, see GroundMovementData.Friction
    public DM64 Friction = new DM64(2);
    public DM64 TurnaroundMultiplier = new DM64(5);
    // Ticks the Land state lasts. Also the landing lag of any attack sprite that doesn't set its own
    public int LandingLag = 3;
}

// How the character takes hits
[Serializable]
public class CombatData
{
    // Scales how fast knockback dissipates, so heavier characters fly less far. Doesn't change hitstun
    public DM64 Weight = new DM64(1);
}

[Serializable]
public class JumpData
{
    public DM64 Velocity = new DM64(50);
    // Used instead of Velocity when jump is released before the jump squat ends
    public DM64 ShortHopVelocity = new DM64(30);
    public int AirJumps = 2;
    // Horizontal push in the stick's direction on an air jump, capped by the air max velocity
    public DM64 AirHorizontalImpulse = new DM64(2);
}

using System;

// Every character shares this shape, only the values differ.
// Defaults live on the fields, so a character only lists what it changes.
// Velocities are in units per second, accelerations in units per second per second.
// Only the physics server converts velocity to units per tick.
[Serializable]
public class CharacterData
{
    public GroundMovementData Ground = new();
    public AirMovementData Air = new();
    public JumpData Jump = new();
    public BodyData Body = new();
    public VisualData Visuals = new();
    // Ticks a state lasts when it has no authored data, so states that end on their animation still end
    public int DefaultStateLength = 30;
}

// The character's own physics body, which collides with the stage
[Serializable]
public class BodyData
{
    // Full width and height, centered on the character's position
    public DMVector Size = new DMVector(2, 2);
}

public enum VisualKind
{
    // One sheet per animation, a single row of frames in order
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
    // Width of one frame in a sprite sheet. 0 means square frames, as wide as the sheet is tall
    public int SpriteFrameWidth = 0;
    // World units per pixel of a sprite sheet. A sprite character's boxes are authored in the same pixels,
    // so this also sizes them in play and must be a DM64. Changing it rescales art and boxes together
    public DM64 PixelSize = new DM64(1) / 100;

    // World units per unit of an authored box offset or size: pixels for sprites, world units for models
    public DM64 BoxUnit => Kind == VisualKind.Sprite ? PixelSize : new DM64(1);
}

[Serializable]
public class GroundMovementData
{
    public DM64 Acceleration = new DM64(30);
    public DM64 MaxVelocity = new DM64(2);
    // Fraction of horizontal velocity lost per second, applied a tick's share at a time
    public DM64 Friction = new DM64(6);
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
    // Fraction of horizontal velocity lost per second, applied a tick's share at a time
    public DM64 Friction = new DM64(2);
    public DM64 TurnaroundMultiplier = new DM64(5);
}

[Serializable]
public class JumpData
{
    public DM64 Velocity = new DM64(50);
    public int AirJumps = 2;
    // Horizontal push in the stick's direction on an air jump, capped by the air max velocity
    public DM64 AirHorizontalImpulse = new DM64(2);
}

// Placeholder platformer character
public static partial class Roster
{
    public static readonly CharacterData Platty = new()
    {
        Ground = { MaxVelocity = new DM64(3) },
        Jump = { Velocity = new DM64(15) / 2, ShortHopVelocity = new DM64(9) / 2 },
        // 49 pixel frames drawn as tall as the 2 unit body
        Visuals = { DefaultAnimation = "BallGamePlayerSpriteSheetV1", PixelSize = new DM64(2) / 49 },
    };
}

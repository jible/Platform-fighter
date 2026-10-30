using System;
using System.Collections.Generic;

// Small value for passing a stage choice around (stage select, match setup, netplay)
public enum StageId
{
    Test,
}

// Every stage shares this shape, only the values differ. Built into nodes by the StageHolder.
// Also the layout of a stage's file, see StageFiles
public class StageData
{
    // Solid blocks characters stand on and collide with
    public List<StageBlock> Blocks = new();
    // In player number order. Empty uses the StageHolder's defaults
    public List<DMVector> SpawnPoints = new();
    // Characters outside this die. Should sit just past what the camera shows
    public StagePerimeter Perimeter;
}

// The bottom left and top right corners of a box in world space
public struct StagePerimeter
{
    public DMVector Min;
    public DMVector Max;

    public StagePerimeter(DMVector min, DMVector max)
    {
        Min = min;
        Max = max;
    }

    // On the edge counts as inside
    public bool Contains(DMVector point) => point.x >= Min.x && point.x <= Max.x && point.y >= Min.y && point.y <= Max.y;
}

// A class rather than a struct, so the stage editor can change one in place
public class StageBlock
{
    // For the stage editor. The game never reads it
    public string Name = "";
    public DMVector Center;
    // Full width and height
    public DMVector Size;
    // Only stops characters landing on it from above, and holding down falls through
    public bool Platform;

    // For the JSON reader
    public StageBlock() { }

    public StageBlock(string name, DMVector center, DMVector size, bool platform = false)
    {
        Name = name;
        Center = center;
        Size = size;
        Platform = platform;
    }
}

public static class Stages
{
    // The stage's file when it has one, otherwise its built in data. A copy either way, so editing it changes nothing else
    public static StageData Get(StageId id) => StageFiles.ReadStage(id) ?? CharacterFileJson.Copy(BuiltIn(id));

    // What a stage is before its file is first saved. A switch instead of a prebuilt table, like the Roster
    public static StageData BuiltIn(StageId id)
    {
        return id switch
        {
            StageId.Test => Test,
            _ => throw new ArgumentOutOfRangeException(nameof(id), $"No data registered for {id}"),
        };
    }

    // A floor with a block and a platform above it to jump on
    public static readonly StageData Test = new()
    {
        Blocks =
        {
            new StageBlock("Floor", new DMVector(0, 0), new DMVector(18, 1)),
            new StageBlock("Block", new DMVector(5, 3), new DMVector(4, 1)),
            new StageBlock("Platform", new DMVector(-5, 3), new DMVector(4, 1), platform: true),
        },
        SpawnPoints =
        {
            new DMVector(-3, 2),
            new DMVector(3, 2),
            new DMVector(-6, 2),
            new DMVector(6, 2),
        },
        // Just past the play scene's camera view. The view is widest at the bottom, so the sides clear that
        Perimeter = new StagePerimeter(new DMVector(-32, -17), new DMVector(32, 16)),
    };
}

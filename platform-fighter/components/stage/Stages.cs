using System;
using System.Collections.Generic;

// Small value for passing a stage choice around (stage select, match setup, netplay)
public enum StageId
{
    Test,
}

// Every stage shares this shape, only the values differ. Built into nodes by the StageHolder
public class StageData
{
    // Solid blocks characters stand on and collide with
    public List<StageBlock> Blocks = new();
    // In player number order. Empty uses the StageHolder's defaults
    public List<DMVector> SpawnPoints = new();
}

public struct StageBlock
{
    public DMVector Center;
    // Full width and height
    public DMVector Size;

    public StageBlock(DMVector center, DMVector size)
    {
        Center = center;
        Size = size;
    }
}

public static class Stages
{
    // A switch instead of a prebuilt table, like the Roster
    public static StageData Get(StageId id)
    {
        return id switch
        {
            StageId.Test => Test,
            _ => throw new ArgumentOutOfRangeException(nameof(id), $"No data registered for {id}"),
        };
    }

    // A floor with one block above it to jump on
    public static readonly StageData Test = new()
    {
        Blocks =
        {
            new StageBlock(new DMVector(0, 0), new DMVector(18, 1)),
            new StageBlock(new DMVector(5, 3), new DMVector(4, 1)),
        },
        SpawnPoints =
        {
            new DMVector(-3, 2),
            new DMVector(3, 2),
            new DMVector(-6, 2),
            new DMVector(6, 2),
        },
    };
}

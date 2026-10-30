using Godot;

// Builds the stage for the play manager and holds it. Sits under the world root, so the world collects the stage's blocks,
// but the world never builds or knows about the stage itself
public partial class StageHolder : Node3D
{
    // How deep the blocks are drawn. Display only
    private const float BlockDepth = 2;

    // For stages that don't store their own, in player number order
    private static readonly DMVector[] DefaultSpawnPoints =
    {
        new DMVector(-3, 2),
        new DMVector(3, 2),
        new DMVector(-6, 2),
        new DMVector(6, 2),
    };

    private StageData data;

    // Null until the stage is loaded
    public StageData Data => data;

    // Called by the play manager before the world is built
    public void LoadStage(StageId stageId)
    {
        data = Stages.Get(stageId);
        for (int i = 0; i < data.Blocks.Count; i++)
        {
            StageBlock block = data.Blocks[i];
            var blockObject = new PhysicsObject
            {
                Name = $"Block {i}",
                shape = new Square { size = block.Size },
                objectType = PhysicsObject.ObjectType.CollisionObject,
                isStatic = true,
                Layer = block.Platform ? CollisionLayers.Platforms : CollisionLayers.Stage,
                oneWay = block.Platform,
                // The mesh already shows it
                renderShape = false,
            };
            blockObject.SimTransform.LocalPosition = block.Center;
            blockObject.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = block.Size.ToVector3(BlockDepth) } });
            AddChild(blockObject);
        }
    }

    // Where a player starts. Past the stage's own spawn points, the defaults are used
    public DMVector SpawnPoint(int playerNumber)
    {
        if (data != null && playerNumber < data.SpawnPoints.Count) return data.SpawnPoints[playerNumber];
        return DefaultSpawnPoints[playerNumber % DefaultSpawnPoints.Length];
    }
}

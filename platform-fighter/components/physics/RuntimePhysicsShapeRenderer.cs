// Draws the shapes of the physics objects the world collected. Owned by the DeterministicWorld,
// which calls Draw every frame. Display only
public class RuntimePhysicsShapeRenderer : IWorldRenderer
{
    public bool Visible = true;
    public float RenderZAxis = 0;
    public float RenderThickness = .5f;

    private readonly PhysicsObject[] objects;
    private readonly DebugBoxDrawer drawer;

    public RuntimePhysicsShapeRenderer(PhysicsObject[] objects, DebugBoxDrawer drawer)
    {
        this.objects = objects;
        this.drawer = drawer;
    }

    public void Draw()
    {
        if (!Visible) return;
        foreach (var obj in objects)
        {
            if (obj.shape == null || !obj.renderShape) continue;

            // Circles aren't drawn until the physics supports them
            if (obj.shape is Square square)
            {
                drawer.Draw(
                    obj.SimTransform.GlobalPosition.ToVector3(RenderZAxis),
                    square.size.ToVector3(RenderThickness),
                    obj.renderColor);
            }
        }
    }
}

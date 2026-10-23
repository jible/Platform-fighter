// Draws the shapes of the physics objects the world collected. Owned by the DeterministicWorld,
// which calls Draw every frame. Display only
public class RuntimePhysicsShapeRenderer : IWorldRenderer
{
    public bool Visible = true;
    // Where the front face sits. Just behind the character sprites at z = 0, so the sprites draw over the shapes
    public float RenderZAxis = -.01f;
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
                    // Centred half the thickness further back, so no part of the box is in front of RenderZAxis
                    obj.SimTransform.GlobalPosition.ToVector3(RenderZAxis - RenderThickness / 2),
                    square.size.ToVector3(RenderThickness),
                    obj.renderColor);
            }
        }
    }
}

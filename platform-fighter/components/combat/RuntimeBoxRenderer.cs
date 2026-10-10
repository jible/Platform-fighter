using Godot;
using System.Collections.Generic;

// Draws the hit and hurt boxes the HitboxManager gathered on the last tick.
// A world renderer, added by the PlayManager. The world calls Draw every frame, then shows what was drawn
public class RuntimeBoxRenderer : IWorldRenderer
{
    public bool Visible = true;
    // In front of the physics shapes, which sit around z = 0. Godot's camera looks down -z, so in front is +z
    public float RenderZAxis = .3f;
    public float RenderThickness = .1f;
    public Color HitboxColor = new(1f, .2f, .2f, .5f);
    public Color HurtboxColor = new(.2f, .5f, 1f, .4f);

    private readonly HitboxManager hitboxManager;
    private readonly DebugBoxDrawer drawer;

    public RuntimeBoxRenderer(HitboxManager hitboxManager, DebugBoxDrawer drawer)
    {
        this.hitboxManager = hitboxManager;
        this.drawer = drawer;
    }

    public void Draw()
    {
        if (!Visible) return;
        // Hurtboxes first, so overlapping hitboxes draw over them
        DrawAll(hitboxManager.Hurtboxes, HurtboxColor);
        DrawAll(hitboxManager.Hitboxes, HitboxColor);
    }

    private void DrawAll(IReadOnlyList<Box> boxes, Color color)
    {
        for (int i = 0; i < boxes.Count; i++)
        {
            drawer.Draw(boxes[i].Center.ToVector3(RenderZAxis), boxes[i].Size.ToVector3(RenderThickness), color);
        }
    }
}

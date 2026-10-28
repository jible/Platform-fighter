using Godot;
using System;

// Draws see through boxes for the debug renderers. Boxes are drawn every frame and only last until the next Submit,
// so callers never create or track anything.
// Everything is one MultiMesh made through the RenderingServer, so it adds no nodes to the scene tree:
// the root is only used to find the 3D world to draw into. Boxes draw in the order they were drawn this frame.
// Owned by the DeterministicWorld, which submits every frame and disposes it when the match ends
public class DebugBoxDrawer : IDisposable
{
    // 12 floats of transform, then 4 of color
    private const int FloatsPerBox = 16;

    // Kept so the mesh and material aren't freed while the RenderingServer still uses them
    private readonly BoxMesh cube = new() { Size = Vector3.One };
    private readonly StandardMaterial3D material = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        VertexColorUseAsAlbedo = true,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        // All boxes are one instance, so Godot depth sorts them against sprites by the whole MultiMesh's center,
        // which moves as anything in the scene moves. Below the sprites' 0, so they always draw over the boxes
        RenderPriority = -1,
    };
    private readonly Rid multimesh;
    private readonly Rid instance;

    // Boxes drawn since the last Submit
    private float[] buffer = Array.Empty<float>();
    private int capacity;
    private int count;

    public DebugBoxDrawer(Node root)
    {
        multimesh = RenderingServer.MultimeshCreate();
        RenderingServer.MultimeshSetMesh(multimesh, cube.GetRid());
        Allocate(16);
        instance = RenderingServer.InstanceCreate2(multimesh, root.GetViewport().FindWorld3D().Scenario);
        RenderingServer.InstanceGeometrySetMaterialOverride(instance, material.GetRid());
    }

    public void Draw(Vector3 center, Vector3 size, Color color)
    {
        if (count == capacity) Grow();

        // Scale only, so the basis is a diagonal
        int i = count * FloatsPerBox;
        buffer[i + 0] = size.X; buffer[i + 1] = 0;      buffer[i + 2] = 0;       buffer[i + 3] = center.X;
        buffer[i + 4] = 0;      buffer[i + 5] = size.Y; buffer[i + 6] = 0;       buffer[i + 7] = center.Y;
        buffer[i + 8] = 0;      buffer[i + 9] = 0;      buffer[i + 10] = size.Z; buffer[i + 11] = center.Z;
        buffer[i + 12] = color.R; buffer[i + 13] = color.G; buffer[i + 14] = color.B; buffer[i + 15] = color.A;
        count++;
    }

    // Shows everything drawn since the last Submit, then starts the next frame empty
    public void Submit()
    {
        RenderingServer.MultimeshSetBuffer(multimesh, buffer);
        RenderingServer.MultimeshSetVisibleInstances(multimesh, count);
        count = 0;
    }

    public void Dispose()
    {
        RenderingServer.FreeRid(instance);
        RenderingServer.FreeRid(multimesh);
    }

    // Reallocating clears the MultiMesh, so capacity only grows, and doubles to keep it rare
    private void Grow()
    {
        float[] old = buffer;
        Allocate(capacity * 2);
        Array.Copy(old, buffer, old.Length);
    }

    private void Allocate(int boxes)
    {
        capacity = boxes;
        buffer = new float[capacity * FloatsPerBox];
        RenderingServer.MultimeshAllocateData(multimesh, capacity, RenderingServer.MultimeshTransformFormat.Transform3D, colorFormat: true);
    }
}

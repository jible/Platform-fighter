using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class dp_shape_renderer_3d : Node3D
{
    
    [Export] public dp_physics_server PhysicsServer;
    [Export] public bool RenderShapesInEditor;
    [Export] public bool RenderShapesInPlay;
    public int shape_layer = 0;
    public float BuildBoardThickness = 0.1f;


    private ImmediateMesh debugMesh;
    private MeshInstance3D meshInstance;

    public override void _Ready()
    {
        meshInstance = new MeshInstance3D();
        debugMesh = new ImmediateMesh();
        meshInstance.Mesh = debugMesh;
        AddChild(meshInstance);
    }

    public override void _PhysicsProcess(double delta)
    {
        
        if ((Engine.IsEditorHint() && RenderShapesInEditor) || (!Engine.IsEditorHint() && RenderShapesInPlay))
        {
            update_shape_render();
            GD.Print("wow");
        }
    }

    public void update_shape_render()
    {
        debugMesh.ClearSurfaces();
        if (PhysicsServer == null) { return; }
        foreach (dp_object PhysicsObject in PhysicsServer.AllShapes)
        {
            
            dp_shape Shape = PhysicsObject.Shape;
            if (PhysicsObject == null || Shape == null) { continue; }
            if (Shape is dp_circle c) { 
                GD.Print("circle");
                draw_collision_circle(c); }
            else if (Shape is dp_rectangle r) { 
                GD.Print("rectangle");
                draw_collision_rectangle(r); }
        }
    }

    public void draw_collision_circle(dp_circle circle)
    {
        Vector3 ObjPosition;
        float ObjRadius;
        if (Engine.IsEditorHint())
        {
            ObjPosition = VectorUp(circle.EditorPosition);
            ObjRadius = circle.EditorRadius;
        }else
        {
            ObjPosition = VectorUp(circle.Position.ToStandardVector());
            ObjRadius = circle.Radius.ToFloat();
        }
        DrawCircle(ObjPosition, ObjRadius,circle.PhysicsObject.color);
    }
    public void DrawCircle(Vector3 center, float radius, Color color)
    {
        debugMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        debugMesh.SurfaceSetColor(color);

        int segments = 40;
        Vector3 prev = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.Tau;

            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );

            // Each segment produces one triangle: center → prev → next
            debugMesh.SurfaceAddVertex(center);
            debugMesh.SurfaceAddVertex(prev);
            debugMesh.SurfaceAddVertex(next);

            prev = next;
        }

        debugMesh.SurfaceEnd();
    }


    public void draw_collision_rectangle(dp_rectangle rectangle)
    {
        Vector2 ObjSize;
        Vector3 ObjPos;
        if (Engine.IsEditorHint()){
            ObjSize = rectangle.EditorSize;
            ObjPos = VectorUp(rectangle.EditorPosition);
        } else{
            ObjSize = rectangle.Size.ToStandardVector();
            ObjPos = VectorUp(rectangle.Position.ToStandardVector());
        }
        
        DrawRectangle(ObjPos, ObjSize, rectangle.PhysicsObject.color);
    }

    public void DrawRectangle(Vector3 center, Vector2 size, Color color)
    {
        debugMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        debugMesh.SurfaceSetColor(color);

        Vector3 a = center + new Vector3(-size.X/2, -size.Y/2, 0);
        Vector3 b = center + new Vector3( size.X/2, -size.Y/2, 0);
        Vector3 c = center + new Vector3( size.X/2,  size.Y/2, 0);
        Vector3 d = center + new Vector3(-size.X/2,  size.Y/2, 0);

        // Triangle 1
        debugMesh.SurfaceAddVertex(a);
        debugMesh.SurfaceAddVertex(b);
        debugMesh.SurfaceAddVertex(c);

        // Triangle 2
        debugMesh.SurfaceAddVertex(c);
        debugMesh.SurfaceAddVertex(d);
        debugMesh.SurfaceAddVertex(a);

        debugMesh.SurfaceEnd();
    }


    public Vector3 VectorUp(Vector2 original)
    {
        return new Vector3(original.X, original.Y, shape_layer);
    }
}

using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class dp_shape_renderer_3d : Node3D
{
    
    [Export] public dp_physics_server PhysicsServer;
    [Export] public bool RenderShapesInEditor;
    [Export] public bool RenderShapesInPlay;
    public Dictionary<dp_shape, MeshInstance3D> MeshCollection = [];
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
            if (Shape is dp_circle c) { draw_collision_circle(c); }
            else if (Shape is dp_rectangle r) { draw_collision_rectangle(r); }
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
        debugMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        debugMesh.SurfaceSetColor(color);

        int segments = 40;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.Tau;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius,
                                            Mathf.Sin(angle) * radius,
                                            0);
            debugMesh.SurfaceAddVertex(p);
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
        debugMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        debugMesh.SurfaceSetColor(color);

        Vector3 a = center + new Vector3(-size.X/2, -size.Y/2, 0);
        Vector3 b = center + new Vector3( size.X/2, -size.Y/2, 0);
        Vector3 c = center + new Vector3( size.X/2,  size.Y/2, 0);
        Vector3 d = center + new Vector3(-size.X/2,  size.Y/2, 0);

        void Edge(Vector3 p1, Vector3 p2)
        {
            debugMesh.SurfaceAddVertex(p1);
            debugMesh.SurfaceAddVertex(p2);
        }

        Edge(a, b);
        Edge(b, c);
        Edge(c, d);
        Edge(d, a);

        debugMesh.SurfaceEnd();
    }


    public Vector3 VectorUp(Vector2 original)
    {
        return new Vector3(original.X, original.Y, shape_layer);
    }
}

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
    public Dictionary<dp_object,MeshInstance3D> ObjectToMesh = [];
    public Dictionary<dp_object,object[]> PreviousData = [];


    public override void _PhysicsProcess(double delta)
    {
        if ((Engine.IsEditorHint() && RenderShapesInEditor) || (!Engine.IsEditorHint() && RenderShapesInPlay))
        {
            update_shape_render();
        }
    }

    public void update_shape_render()
    {
        var seenShapes = new HashSet<dp_object>();

        if (PhysicsServer == null) { return; }
        foreach (dp_object PhysicsObject in PhysicsServer.AllShapes)
        {
            if (PhysicsObject == null || PhysicsObject.Shape == null)
            {
                continue;
            }



            dp_shape Shape = PhysicsObject.Shape;
            seenShapes.Add(PhysicsObject);
            MeshInstance3D mesh;

            bool exists = ObjectToMesh.TryGetValue(PhysicsObject, out mesh);
            if (!exists)
            {
                mesh = new MeshInstance3D();
                AddChild(mesh);
                ObjectToMesh[PhysicsObject] = mesh;
            }

            if (Shape is dp_circle c) { 
                draw_collision_circle(PhysicsObject, c, mesh); }
            else if (Shape is dp_rectangle r) { 
                draw_collision_rectangle(PhysicsObject, r, mesh); }
        }
        var toRemove = new List<dp_object>();

        foreach (var PhysicsObject in ObjectToMesh.Keys)
        {
            if (!seenShapes.Contains(PhysicsObject))
            {
                toRemove.Add(PhysicsObject);
            }
        }
        foreach (var item in toRemove)
        {
            RemoveChild(ObjectToMesh[item]);
            ObjectToMesh[item].QueueFree();
            ObjectToMesh.Remove(item);
            PreviousData.Remove(item);
        }
    }

    public void draw_collision_circle(dp_object obj, dp_circle circle, MeshInstance3D mesh)
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
        return;
    }


    public void draw_collision_rectangle(dp_object obj, dp_rectangle rectangle, MeshInstance3D meshInstance)
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
        
        

        meshInstance.Position = ObjPos;
        
        // Rectangle Data:
        // [shape, size]
        object[] ObjPrevData = [];
        bool HasData = PreviousData.TryGetValue(obj, out ObjPrevData);

        // If it has the data and its the same, 
        if (
            HasData &&
            (string)ObjPrevData[0] == "rectangle" && 
            (Vector2)ObjPrevData[1] == ObjSize)
        {return;}
        // In this case it is different, so make a new quad mesh
        PreviousData[obj] = ["rectangle", ObjSize];
        var quad = new QuadMesh();
        quad.Size = ObjSize;
        meshInstance.Mesh = quad;
    }



    public Vector3 VectorUp(Vector2 original)
    {
        return new Vector3(original.X, original.Y, shape_layer);
    }
}

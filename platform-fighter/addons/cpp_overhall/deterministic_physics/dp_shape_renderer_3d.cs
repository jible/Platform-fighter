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
    private Dictionary<dp_object,RenderState> PreviousData = [];


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
        foreach (dp_object PhysicsObject in PhysicsServer.AllObjects)
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

    public void draw_collision_circle(dp_object obj, dp_circle circle, MeshInstance3D MeshInstance)
    {
        Vector3 ObjPosition;
        float ObjRadius;
        if (Engine.IsEditorHint())
        {
            ObjPosition = VectorUp(circle.EditorPosition, shape_layer);
            ObjRadius = circle.EditorRadius;
        }else
        {
            ObjPosition = VectorUp(circle.Position.ToStandardVector(), shape_layer);
            ObjRadius = circle.Radius.ToFloat();
        }
        

        RenderState ObjPrevData;
        bool HasData = PreviousData.TryGetValue(obj, out ObjPrevData);
        bool SameShape = HasData && ObjPrevData.ShapeType == "circle";
        bool SameColor = HasData && ObjPrevData.color == obj.color;
        bool SameRadius = HasData && ObjPrevData.Radius == ObjRadius;


        if (!SameShape || !SameRadius)
        {
            CylinderMesh mesh = new();
            mesh.TopRadius = ObjRadius;
            mesh.BottomRadius = ObjRadius;
            mesh.Height = BuildBoardThickness;
            MeshInstance.Mesh = mesh;
        }

        if (!SameColor)
        {
            MeshInstance.MaterialOverride = MakeMaterial(obj.color);
        }

        MeshInstance.Position = ObjPosition;
        PreviousData[obj] = new RenderState("circle", ObjRadius, obj.color);
        MeshInstance.RotationDegrees = new Vector3(90, 0, 0);

    }

    public void draw_collision_rectangle(dp_object obj, dp_rectangle rectangle, MeshInstance3D meshInstance)
    {
        Vector2 ObjSize;
        Vector3 ObjPos;
        if (Engine.IsEditorHint()){
            ObjSize = rectangle.EditorSize;
            ObjPos = VectorUp(rectangle.EditorPosition, shape_layer);
        } else{
            ObjSize = rectangle.Size.ToStandardVector();
            ObjPos = VectorUp(rectangle.Position.ToStandardVector(), shape_layer);
        }

        meshInstance.Position = ObjPos;
        // e
        // Rectangle Data:
        // [shape, color, size]
        RenderState ObjPrevData;
        bool HasData = PreviousData.TryGetValue(obj, out ObjPrevData);
        bool SameShape = HasData && ObjPrevData.ShapeType == "rectangle";
        bool SameColor = HasData && ObjPrevData.color == obj.color;
        bool SameSize = HasData && ObjPrevData.Size == ObjSize;

        if (!SameShape || !SameSize)
        {
            var box = new BoxMesh();
            box.Size = VectorUp(ObjSize, BuildBoardThickness);
            meshInstance.Mesh = box;
        }
        if (!SameColor)
        {
            meshInstance.MaterialOverride = MakeMaterial(obj.color);
        }
        meshInstance.RotationDegrees = new Vector3(0, 0, 0);
        PreviousData[obj] = new RenderState("rectangle", ObjSize, obj.color);
    }

    public Vector3 VectorUp(Vector2 original, float z)
    {
        return new Vector3(original.X, original.Y, z);
    }

    public StandardMaterial3D MakeMaterial(Color color)
    {
        var mat = new StandardMaterial3D();

        mat.AlbedoColor = color;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        // mat.BlendMode = BaseMaterial3D.BlendModeEnum.Mix;
        // mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;
        // mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;

        return mat;
    }

    
    class RenderState
    {
        public string ShapeType;
        public Vector2 Size;
        public float Radius;
        public Color color;

        public RenderState(string st,Vector2 s, Color c)
        {
            ShapeType = st;
            Size = s;
            color = c;
        }

        public RenderState(string st,float r, Color c)
        {
            ShapeType = st;
            Radius = r;
            color = c;
        }
    }
}



using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class dp_shape_renderer_3d : Node3D
{
    public static dp_shape_renderer_3d GlobalInstance;
    [Export] public dp_physics_server PhysicsServer;
    [Export] public bool RenderShapesInEditor = true;
    [Export] public bool RenderShapesInPlay = true;
    public int shape_layer = 0;
    public float BuildBoardThickness = 0.1f;
    public Dictionary<dp_object,MeshInstance3D> ObjectToMesh = [];
    private Dictionary<dp_object,RenderState> PreviousData = [];

    public List<dp_shape_follower> ShapeFollowers = [];

    public override void _Ready()
    {
        configure();
        GetTree().SceneChanged += () => configure();
    }

    public Node GetRoot()
    {
        Node root= Engine.IsEditorHint() 
        ? GetTree().EditedSceneRoot:
        GetTree().CurrentScene;

        return root;
    }

    public void configure(Node Root = null)
    {
        if (Root == null)
        {
            Root = GetRoot();
        }
        GlobalInstance = this;

        foreach (var mesh in ObjectToMesh.Values)
        {
            mesh.QueueFree();
        }
        ObjectToMesh.Clear();
        PreviousData.Clear();
        ShapeFollowers.Clear();
        GetAllFollowers(Root);
    } 

    public void GetAllFollowers(Node Parent)
	{
		if (Parent == null) { return; }
		if (Parent is dp_shape_follower CastedParent) {
			if (!ShapeFollowers.Contains(CastedParent)) {
				ShapeFollowers.Add(CastedParent); 
			}
		}
		foreach (var ChildNode in Parent.GetChildren())
		{
			GetAllFollowers(ChildNode);
		}
	}

    public override void _PhysicsProcess(double delta)
    {
        
        if (Engine.IsEditorHint() && RenderShapesInEditor)
        {
            update_shape_render();
        }
    }
    public void update_shape_render()
    {
        
        UpdateFollowerPositions();
        if (!Engine.IsEditorHint() && !RenderShapesInPlay) return;

        Node CurrentScene = (Engine.IsEditorHint()) ? GetTree().EditedSceneRoot: GetTree().CurrentScene;

        var seenShapes = new HashSet<dp_object>();

        if (PhysicsServer == null) { 
            PhysicsServer = dp_physics_server.GlobalInstance;
            if (PhysicsServer == null) return;
        }
        foreach (dp_object PhysicsObject in PhysicsServer.AllEntities)
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
                CurrentScene.AddChild(mesh);
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
        while (toRemove.Count > 0)
        {
            var PhysicsObj = toRemove[0];
            var mesh = ObjectToMesh[PhysicsObj];
            if (mesh != null && mesh.GetParent() == CurrentScene)
            { 
                CurrentScene.RemoveChild(mesh);
                mesh.QueueFree();
            }
            ObjectToMesh[PhysicsObj].QueueFree();
            ObjectToMesh.Remove(PhysicsObj);
            PreviousData.Remove(PhysicsObj);
            toRemove.RemoveAt(0);
        }
    }

    public void RegisterFollower(dp_shape_follower newItem)
    {
        if ( ShapeFollowers.Contains(newItem)) return;
        ShapeFollowers.Add(newItem);
    }



    public void UpdateFollowerPositions()
    {
        foreach (var Follower in ShapeFollowers)
        {
            if (Follower == null)
            {
                ShapeFollowers.Remove(Follower);
                continue;
            }
            Follower.UpdatePosition();
        }
    }

    public void draw_collision_circle(dp_object obj, dp_circle circle, MeshInstance3D MeshInstance)
    {
        Vector3 ObjPosition;
        float ObjRadius;
        if (Engine.IsEditorHint())
        {
            ObjPosition = VectorUp(obj.EditorGlobalPosition, shape_layer);
            ObjRadius = circle.EditorRadius;
        }else
        {
            ObjPosition = VectorUp(obj.GlobalPosition.ToStandardVector(), shape_layer);
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
            ObjPos = VectorUp(obj.EditorGlobalPosition, shape_layer);
        } else{
            ObjSize = rectangle.Size.ToStandardVector();
            ObjPos = VectorUp(obj.GlobalPosition.ToStandardVector(), shape_layer);
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



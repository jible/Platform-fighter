using Godot;
using System;
using System.Numerics;

public partial class StageHolder : Node
{

    [Export] String StagePath = "";
    Node Stage;

    public void Config()
    {
        InstanceStage();
    }

    public void InstanceStage()
    {
        PackedScene scene = (PackedScene)GD.Load(StagePath);
        Node3D Instance = (Node3D)scene.Instantiate(); 

        AddChild(Instance);
        Instance.Name = "stage";
        Instance.Position = Godot.Vector3.Zero;
        Stage = Instance;
    }

    public void Tick()
    {
        return;
    }
}

using Godot;
using System;

public partial class PlayManager : Node3D
{
    [Export] public TickManager tickManager;
    [Export] public CharacterHolder characterHolder;
    [Export] public StageHolder StageHolder;
    [Export] public Camera3D Camera;
    [Export] public InputManager inputManager;


    public override void _Ready()
    {
        DP_PhysicsServer.GlobalInstance.configure();
        DP_ShapeRenderer3D.GlobalInstance.configure();
        characterHolder.Config();
        StageHolder.Config();
        tickManager.PrepForRollback();
        
    }

    public void Tick()
    {
        DP_PhysicsServer.GlobalInstance.PhysicsTick();
        if (!tickManager.IsRollingBack )
        {
            DP_ShapeRenderer3D.GlobalInstance.update_shape_render();
        } 
        return;
    }
}

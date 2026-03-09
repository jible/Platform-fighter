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
        dp_physics_server.GlobalInstance.configure();
        dp_shape_renderer_3d.GlobalInstance.configure();
        characterHolder.Config();
        StageHolder.Config();
        tickManager.PrepForRollback();
        
    }

    public void Tick()
    {
        if (Input.IsActionJustPressed("debug_action")){
            inputManager.RollbackTargetFrame = tickManager.GetCurrentTick() -5;
        }
        dp_physics_server.GlobalInstance.PhysicsTick();
        if (!tickManager.IsRollingBack )
        {
            dp_shape_renderer_3d.GlobalInstance.update_shape_render();
        } 
        return;
    }
}

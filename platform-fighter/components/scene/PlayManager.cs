using Godot;
using System;

public partial class PlayManager : Node3D
{
    [Export] public TickManager tickManager;
    [Export] public CharacterHolder characterHolder;
    [Export] public StageHolder StageHolder;
    [Export] public Camera3D Camera;
    [Export] public InputManager inputManager;
    public void Tick()
    {
        characterHolder.Tick();
        return;
    }
}

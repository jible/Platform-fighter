using Godot;
using System;

public partial class ModeSelect : Control
{

    [Export] Button LocalPlayButton;
    [Export] Button ControllerCustomizationButton;

    string LocalCharacterSelectScenePath = "uid://cl3dgdjb3y1tx";
    string ControllerCustomizationScenePath = "uid://cm5nlcuvc8yuq";

    public override void _Ready()
    {
        LocalPlayButton.Pressed += () => GetTree().ChangeSceneToFile(LocalCharacterSelectScenePath);
        ControllerCustomizationButton.Pressed += () => GetTree().ChangeSceneToFile(ControllerCustomizationScenePath);
    }

}

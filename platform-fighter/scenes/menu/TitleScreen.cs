using Godot;
using System;

public partial class TitleScreen : Control
{

    string ModeSelectScenePath = "uid://cwlsejxx2roi7";

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton MouseEvent && MouseEvent.Pressed && MouseEvent.ButtonIndex == MouseButton.Left)
        {
            GetTree().ChangeSceneToFile(ModeSelectScenePath);
        }
    }

}

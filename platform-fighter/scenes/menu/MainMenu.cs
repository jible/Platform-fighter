using Godot;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

public partial class MainMenu : Control
{

    string CharSelectScenePath = "uid://cdcq8ql8pxore";
    string OnlineMenuScenePath = "uid://bxax0g1vr4tev";

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey KeyEvent && KeyEvent.Pressed)
        {
            HandleKeyPress(KeyEvent.Keycode);
        }
    }

    public void HandleKeyPress(Key key)
    {
        switch (key)
        {
            case Key.L:
                GetTree().ChangeSceneToFile(CharSelectScenePath);
                break;                
            case Key.O:
                GetTree().ChangeSceneToFile(OnlineMenuScenePath);
                break;                
            
        }
    }


}

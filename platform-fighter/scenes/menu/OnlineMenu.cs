using Godot;
using System;
using System.Collections.Generic;

public partial class OnlineMenu : Control
{

    [Export] TextEdit IpField;
    [Export] TextEdit PortField;
    string CharSelectScenePath = "uid://cdcq8ql8pxore";
    public override void _Input(InputEvent @event)
    {
        SelectInputProcess(@event);

    }

    void SelectInputProcess( InputEvent Event )
    {
        InputEventKey EventKey;
        if (Event is InputEventKey Casted && Casted.Pressed)
        {
            EventKey = Casted;
        } else return;

        int Port;
        bool isValidPort = int.TryParse(PortField.Text, out Port);

        if (!isValidPort) return;

        string Ip = IpField.Text;
        // TODO: Make a function that verifies ip

        if (EventKey.Keycode == Key.H)
        {
            NetworkManager.GlobalInstance.StartGame(Port);
            GetTree().ChangeSceneToFile(CharSelectScenePath);
        } else if (EventKey.Keycode == Key.J)
        {
            NetworkManager.GlobalInstance.JoinGame(Ip, Port);

            GetTree().ChangeSceneToFile(CharSelectScenePath);
            
        }
    }



}



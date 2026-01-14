using Godot;
using System;
using System.Collections.Generic;

public partial class OnlineMenu : Control
{
    public enum Modes
    {
        SELECT,
        HOST,
        CLIENT,
    }
    [Export] Control SelectModeNodes;
    [Export] Control HostModeNodes;
    [Export] Control ClientModeNodes;

    public Dictionary< Modes, Control> ModeToNode;

    public void _Ready()
    {
        ModeToNode = new(){
            {Modes.SELECT, SelectModeNodes},
            {Modes.CLIENT, ClientModeNodes},
            {Modes.HOST, HostModeNodes},
        };

        ChangeMode(Modes.SELECT);
    }

    Modes Mode = Modes.SELECT;
    public override void _Input(InputEvent @event)
    {
        switch (Mode)
        {
            case Modes.SELECT:
                SelectInputProcess(@event);
                break;
            case Modes.HOST:
                break;
            case Modes.CLIENT:
                break;
        }
    }

    void SelectInputProcess( InputEvent Event )
    {
        InputEventKey EventKey;
        if (Event is InputEventKey Casted && Casted.Pressed)
        {
            EventKey = Casted;
        } else return;

        if (EventKey.Keycode == Key.H)
        {
            
        } else if (EventKey.Keycode == Key.J)
        {
            
        }
    }

    void HostInputProcess ( InputEvent Event )
    {
        InputEventKey EventKey;
        if (Event is InputEventKey Casted && Casted.Pressed)
        {
            EventKey = Casted;
        } else return;

    }
    void ClientInputProcess ( InputEvent Event )
    {
        InputEventKey EventKey;
        if (Event is InputEventKey Casted && Casted.Pressed)
        {
            EventKey = Casted;
        } else return;

        
    }

    void ChangeMode(Modes _Mode)
    {
        ModeToNode[Mode].Visible  = false;
        Mode = _Mode;
        ModeToNode[Mode].Visible  = true;
        
    }

}

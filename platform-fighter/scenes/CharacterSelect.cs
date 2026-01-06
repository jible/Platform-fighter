using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterSelect : Control
{
    [Export] Label MessageHolder;
    [Export] VBoxContainer CharacterLabels;
    public Dictionary <int, Label> Labels = [];

    public override void _Ready()
    {
        MessageHolder.Text = "Press R to ready";
        PlayerManager.GlobalInstance.PlayerAdded += OnPlayerAdded;
        PlayerManager.GlobalInstance.PlayerRemoved += OnPlayerRemoved;
    }

    public override void _Input(InputEvent @event )
    {
        if (!@event.IsPressed()) return;
        if (@event.IsActionPressed("debug_ready") )
        {
            PlayerManager.GlobalInstance.AttemptAddPlayer(@event);
        } else if (@event.IsActionPressed("play_default_special"))
        {
            PlayerManager.GlobalInstance.AttemptRemovePlayer(@event);
        }
    }

    public void OnPlayerAdded(int PlayerNumber)
    {
        GD.Print("playerAdded", PlayerNumber);
        Label NewLabel = new();
        CharacterLabels.AddChild(NewLabel);
        Labels[PlayerNumber ] = NewLabel;
        NewLabel.Text = PlayerNumber.ToString();

    }

    public void OnPlayerRemoved(int PlayerNumber)
    {
        GD.Print("playerRemoved");
        Label ToRemove = Labels[PlayerNumber];
        if (ToRemove == null) return;
        Labels.Remove(PlayerNumber);
        ToRemove.QueueFree();
    }


}

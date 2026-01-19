using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

public partial class CharacterSelect : Control
{
    [Export] Label MessageHolder;
    [Export] VBoxContainer CharacterLabels;
    public Dictionary <int, Label> Labels = [];
    private string PlayScenePath = "uid://t16aulelxjm1";
    private Dictionary<NetworkManager.ConnectionType,CharSelectMenuState> ConnectionTypeToState;

    public override void _Ready()
    {
        ConnectionTypeToState= new()
        {
            {NetworkManager.ConnectionType.DISCONNECTED, new OfflineMenuState(this)},
            {NetworkManager.ConnectionType.HOST, new HostMenuState(this)},
            {NetworkManager.ConnectionType.CLIENT, new ClientMenuState(this)},
        };

        foreach (var player in PlayerManager.GlobalInstance.AllPlayers)
        {
            if (player == null) continue;
            OnPlayerAdded(player.PlayerNumber);
        }
        MessageHolder.Text = "Press R to ready";
        PlayerManager.GlobalInstance.PlayerAdded += OnPlayerAdded;
        PlayerManager.GlobalInstance.PlayerRemoved += OnPlayerRemoved;

        ConnectionTypeToState[NetworkManager.GlobalInstance.connectionType].Ready();
    }

    public override void _Input(InputEvent @event )
    {
        ConnectionTypeToState[NetworkManager.GlobalInstance.connectionType].HandleInput(@event);
    }

    public void GoToPlayScene()
    {
        GetTree().ChangeSceneToFile(PlayScenePath);
    }

    public int RequestAddPlayer()
    {
        return -1;
    }

    public void OnPlayerAdded(int PlayerNumber)
    {
        GD.Print("playerAdded", PlayerNumber);

        ConnectionTypeToState[NetworkManager.GlobalInstance.connectionType].OnPlayerAdded(PlayerNumber);

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



    

    


 


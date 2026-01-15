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

    
    public class OfflineMenuState: CharSelectMenuState
    {
        public OfflineMenuState(CharacterSelect _charSelectScene) : base(_charSelectScene)
        {
        }

        public override void HandleInput(InputEvent inputEvent)
        {
            if (!inputEvent.IsPressed()) return;
            if (inputEvent.IsActionPressed("debug_ready") )
            {
                PlayerManager.GlobalInstance.AttemptAddPlayer(inputEvent);
            } else if (inputEvent.IsActionPressed("play_default_special"))
            {
                PlayerManager.GlobalInstance.AttemptRemovePlayer(inputEvent);
            } else if (inputEvent.IsActionPressed("start"))
            {
                charSelectScene.GoToPlayScene();
            }
        }
    }

    public class HostMenuState: CharSelectMenuState
    {
        public HostMenuState(CharacterSelect _charSelectScene) : base(_charSelectScene)
        {
        }

        public override void HandleInput(InputEvent inputEvent)
        {
            if (!inputEvent.IsPressed()) return;
            if (inputEvent.IsActionPressed("debug_ready") )
            {
                PlayerManager.GlobalInstance.AttemptAddPlayer(inputEvent);
            } else if (inputEvent.IsActionPressed("play_default_special"))
            {
                PlayerManager.GlobalInstance.AttemptRemovePlayer(inputEvent);
            } else if (inputEvent.IsActionPressed("start"))
            {
            }
        }
    }

    public class ClientMenuState: CharSelectMenuState
    {
        public ClientMenuState(CharacterSelect _charSelectScene) : base(_charSelectScene)
        {
        }

        

        public override void HandleInput(InputEvent inputEvent)
        {
            if (!inputEvent.IsPressed()) return;
            if (inputEvent.IsActionPressed("debug_ready") )
            {
            } else if (inputEvent.IsActionPressed("play_default_special"))
            {
            } else if (inputEvent.IsActionPressed("start"))
            {
            }
        }
    }


    public class CharSelectMenuState
    {
        public CharacterSelect charSelectScene;
        public CharSelectMenuState(CharacterSelect _charSelectScene)
        {
            charSelectScene = _charSelectScene;
        }
        public virtual void  Ready(){}
        public virtual void HandleInput(InputEvent inputEvent ){}
    }
}




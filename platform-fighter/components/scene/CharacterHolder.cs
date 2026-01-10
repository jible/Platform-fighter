using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterHolder : Node, ITickable
{
    public PlayerManager playerManager;
    [Export] public PlayManager playManager; 

    public void Config()
    {
        playerManager = PlayerManager.GlobalInstance;
        InstancePlayers();
    }

    public void Tick()
    {
        
    }


    public void InstancePlayers()
    {
        foreach(var Player in playerManager.AllPlayers){
            if (Player== null) continue;

            
            var characterPath = Player.SelectedCharacterPath;
            PackedScene scene = (PackedScene)GD.Load(characterPath);
            BaseCharacter3d instance = (BaseCharacter3d)scene.Instantiate();

            AddChild(instance);

            Player.Instance= instance;
            instance.Name = "Player" + Player.PlayerNumber.ToString();
            instance.ConfigurePlayer(Player.TeamNumber, Player.PlayerNumber, Player.playerTag);
        }
    }

    // public Dictionary<string, object> SerializeState()
    // {
    //     throw new NotImplementedException();
    // }

    // public void LoadState(Dictionary<string, object> State)
    // {
    //     throw new NotImplementedException();
    // }

}

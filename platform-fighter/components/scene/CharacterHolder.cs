using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterHolder : Node
{
    public PlayerManager playerManager;
    [Export] public PlayManager playManager; 

    public void Config()
    {
        playerManager = PlayerManager.GlobalInstance;
        InstancePlayers();
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
            instance.ConfigurePlayer(Player.PlayerNumber);
        }
    }


}

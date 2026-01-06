using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterHolder : Node
{
    [Export] PlayerManager playerManager;
    List<Node> Players= [];

    public void Config()
    {
        InstancePlayers();
    }

    public void InstancePlayers()
    {
        foreach(var Player in playerManager.AllPlayers){
            if (Players == null){ continue;}
            
        }
    }
}

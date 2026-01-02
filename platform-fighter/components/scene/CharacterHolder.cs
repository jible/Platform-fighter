using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterHolder : Node
{
    [Export] PlayManager payManager;
    List<Node> Players= [];

    public void Config()
    {
        InstancePlayers();
    }

    public void InstancePlayers()
    {
        foreach(var Player in PlayerManager.AllPlayers){
            if (Players == null){ continue;}
            
        }
    }
}

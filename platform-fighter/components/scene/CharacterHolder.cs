using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterHolder : Node, ITickable,ISerializable
{
    [Export] PlayerManager playerManager;
    List<Node> Players= [];

    public void Config()
    {
        InstancePlayers();
    }

    public void Tick()
    {
        
    }


    public void InstancePlayers()
    {
        foreach(var Player in playerManager.AllPlayers){
            if (Players == null){ continue;}
            
        }
    }

    public Dictionary<string, object> SerializeState()
    {
        throw new NotImplementedException();
    }

    public void LoadState(Dictionary<string, object> State)
    {
        throw new NotImplementedException();
    }

}

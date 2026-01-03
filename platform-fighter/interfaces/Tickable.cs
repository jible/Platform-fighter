using Godot;
using System;
using System.Collections.Generic;

public interface ITickable
{
    public void Tick();
    public Dictionary<String, object> SerializeState();
    public void LoadState(Dictionary<String, object> State);

}

using Godot;
using System;
using System.Collections.Generic;

public interface ISerializable
{
    public Dictionary<String, object> SerializeState();
    public void LoadState(Dictionary<String, object> State);
}

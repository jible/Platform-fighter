using Godot;
using System;
using System.Xml.Linq;

public interface ISerializable
{
    ISaveHandler saveHandler {get; set;}
}


public interface ISerializedState<T>
where T : ISerializable
{
    // Extract properties of Given Object into the given state
    public void Save(T e);

    // Given An object, this state applies itself to the object's properties
    public void Load(T e);
}

public interface ISaveHandler
{
    public void Save( int Tick);
    public void Load( int Tick);
}

public class SaveHandler<TObject,TState> : ISaveHandler
where TObject : ISerializable
where TState : ISerializedState<TObject>, new()
{
    TObject Owner;
    TState[] States;
    public SaveHandler(int Ticks, TObject owner)
    {
        States = new TState[Ticks];
        for(int Tick = 0; Tick < Ticks; Tick++)
        {
            States[Tick] = new TState();
        }
        Owner = owner;
    }

    public void Load(int Tick)
    {
        States[Tick].Load(Owner);
    }

    public void Save(int Tick)
    {
        States[Tick].Save(Owner);
    }

}
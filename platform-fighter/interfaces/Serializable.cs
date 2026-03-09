using Godot;
using System;
using System.Xml.Linq;

public interface ISerializable
{
    // Literally just return a save handler that is correctly typed with a reference
    // To itself. Then the tick manager can store the save handler
    public ISaveHandler MakeSaveHandler();
}

// Note: This Type has to be named "SerializedState" and be public!
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
    public SaveHandler(TObject owner)
    {
        States = new TState[NetworkManager.MAX_ROLLBACK_FRAMES];
        for(int Tick = 0; Tick < NetworkManager.MAX_ROLLBACK_FRAMES; Tick++)
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
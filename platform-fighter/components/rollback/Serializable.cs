using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Every piece of rollback state in the match. Properties register themselves when they're constructed,
// so Reset has to run before a match builds its objects
public static class SerializableDataManager
{
    public static List<ISerializable> AllSerializableData = new();
    private static readonly HashSet<ISerializable> registered = new();
    public static void Register(ISerializable serializable)
    {
        if (registered.Contains(serializable)) return;
        AllSerializableData.Add(serializable);
        registered.Add(serializable);
    }
    public static void Reset()
    {
        AllSerializableData.Clear();
        registered.Clear();
    }

    public static void SaveAll(int tickIndex)
    {
        foreach (ISerializable serializable in AllSerializableData)
        {
            serializable.Save(tickIndex);
        }
    }
    public static void LoadAll(int tickIndex)
    {
        foreach (ISerializable serializable in AllSerializableData)
        {
            serializable.Load(tickIndex);
        }
    }

    // One hash of everything saved for a tick, compared between machines to catch desyncs.
    // Combined in registration order, so every machine has to build its objects in the same order
    public static int HashAll(int tickIndex)
    {
        uint hash = FnvOffset;
        foreach (ISerializable serializable in AllSerializableData)
        {
            hash = (hash ^ (uint)serializable.GetHash(tickIndex)) * FnvPrime;
        }
        return (int)hash;
    }

    // FNV-1a. HashCode and string.GetHashCode are randomized per process, so they can't be compared between machines
    public const uint FnvOffset = 2166136261u;
    public const uint FnvPrime = 16777619u;

    // Hashes the raw bytes of an unmanaged value, so it's the same on every machine
    public static int HashBytes<T>(in T value) where T : unmanaged
    {
        uint hash = FnvOffset;
        foreach (byte b in MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)))
        {
            hash = (hash ^ b) * FnvPrime;
        }
        return (int)hash;
    }
}
// Each serializable object
public interface ISerializable
{
    public void Save(int tickIndex);
    public void Load(int tickIndex);
    public int GetHash(int tickIndex);
}

public class SerializableProperty<T> : ISerializable where T : unmanaged
{
    public T Value;
    // Lets the owner react to a rollback load, which writes Value directly and bypasses any property setter
    public Action OnLoaded;
    private readonly T[] values = new T[TickManager.MaxTicks];
    public void Save(int tickIndex)
    {
        values[tickIndex] = Value;
    }
    public void Load(int tickIndex)
    {
        Value = values[tickIndex];
        OnLoaded?.Invoke();
    }
    public int GetHash(int tickIndex)
    {
        return SerializableDataManager.HashBytes(in values[tickIndex]);
    }
    public SerializableProperty(){
        SerializableDataManager.Register(this);
    }
    public T GetDataFromFrame(int tickIndex)
    {
        return values[tickIndex];
    }

}

// This is a serializable data instance that doesn't register so the object can handle loading and saving its data on its own, instead of passing it onto the manager
public class UnregisteredSerializableData<T> : ISerializable where T : unmanaged
{
    public T Value;
    private readonly T[] values = new T[TickManager.MaxTicks];
    public void Save(int tickIndex)
    {
        values[tickIndex] = Value;
    }
    public void Load(int tickIndex)
    {
        Value = values[tickIndex];
    }
    public int GetHash(int tickIndex)
    {
        return SerializableDataManager.HashBytes(in values[tickIndex]);
    }
    public T GetDataFromFrame(int tickIndex)
    {
        return values[tickIndex];
    }

}

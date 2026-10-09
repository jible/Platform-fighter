using System;

// Small value for passing a character choice around (character select, match setup, netplay)
public enum CharacterId : byte
{
    Platty,
}

// Each character's data is declared in its own file as another part of this class
public static partial class Roster
{
    // A switch instead of a prebuilt table: static fields in other partial files
    // have no guaranteed init order, and a table built here could see them as null
    public static CharacterData Get(CharacterId id)
    {
        return id switch
        {
            CharacterId.Platty => Platty,
            _ => throw new ArgumentOutOfRangeException(nameof(id), $"No data registered for {id}"),
        };
    }
}

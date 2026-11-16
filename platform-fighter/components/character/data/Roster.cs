// Small value for passing a character choice around (character select, match setup, netplay).
// Adding a value is all the code a new character needs: build, then use the Character Tools dock in the
// Godot editor to make its folder, copied from CharacterFiles.Template.
// Add new values at the end. The number is what's sent over the network, so inserting one shifts every character after it
public enum CharacterId : byte
{
    Platty,
}

// The code's values for each character, which its file's overrides are applied over, see CharacterFiles.LoadData.
// Every character's values live in its file now, so all of them start from the class defaults.
// A character that ever needs values its file can't hold gets a case here and a partial of this class in its own file
public static partial class Roster
{
    // A switch instead of a prebuilt table: static fields in other partial files
    // have no guaranteed init order, and a table built here could see them as null.
    // A new instance each call, so nothing that edits it changes another character
    public static CharacterData Get(CharacterId id)
    {
        return id switch
        {
            _ => new CharacterData(),
        };
    }
}

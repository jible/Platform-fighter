using Godot;

// Finds, reads and writes a character's files, for both the hitbox editor and the game.
// Everything for a character lives in one folder: res://characters/<id>/
// holding <id>.character.json, <id>.skeleton.json and the art.
//
// Run from the editor, files are read and written in the project folder directly, so edits show up without a reimport.
// In an exported build the project folder is packed and read only. Saves go to user:// instead,
// and a saved copy there is read in place of the shipped one.
// Exports only pack .json files if "*.json" is in the preset's "Filters to export non-resource files"
public static class CharacterFiles
{
    // Where the character's shipped files and art live
    public static string ResourceFolder(CharacterId id) => $"res://characters/{id}";

    private static string CharacterName(CharacterId id) => $"{id}.character.json";
    private static string SkeletonName(CharacterId id) => $"{id}.skeleton.json";

    // Where saves go: the project when run from the editor, the player's machine in an exported build
    public static string WriteFolder(CharacterId id) => OS.HasFeature("editor")
        ? ResourceFolder(id)
        : $"user://characters/{id}";

    // A character with no file yet reads as empty
    public static CharacterFile ReadCharacter(CharacterId id)
    {
        string json = ReadText(id, CharacterName(id));
        CharacterFile file = json != null ? CharacterFileJson.ReadCharacter(json) : new CharacterFile();

        // Hand edited files may have keys out of order, and everything that reads keys assumes they are sorted
        foreach (var state in file.States.Values)
        {
            foreach (var box in state.Boxes)
            {
                BoxKeyMath.Sort(box.Keys);
            }
        }
        return file;
    }

    // Null when the character has no bake, as with sprite characters
    public static BakedSkeletonFile ReadSkeleton(CharacterId id)
    {
        string json = ReadText(id, SkeletonName(id));
        return json != null ? CharacterFileJson.ReadSkeleton(json) : null;
    }

    public static void WriteCharacter(CharacterId id, CharacterFile file)
    {
        string folder = WriteFolder(id);
        DirAccess.MakeDirRecursiveAbsolute(folder);
        string path = $"{folder}/{CharacterName(id)}";
        using var access = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (access == null)
        {
            GD.PushError($"Couldn't save {path}: {FileAccess.GetOpenError()}");
            return;
        }
        access.StoreString(CharacterFileJson.WriteCharacter(file));
    }

    // Read fresh on every call, so edits made in the hitbox editor show up on the next match
    public static CharacterDefinition LoadDefinition(CharacterId id)
    {
        return CharacterDefinitionBuilder.Build(ReadCharacter(id), Roster.Get(id).Visuals.BoxUnit, ReadSkeleton(id));
    }

    // Null when the file doesn't exist
    private static string ReadText(CharacterId id, string name)
    {
        // A saved copy first. From the editor this is the shipped file itself
        string path = $"{WriteFolder(id)}/{name}";
        if (!FileAccess.FileExists(path)) path = $"{ResourceFolder(id)}/{name}";
        if (!FileAccess.FileExists(path)) return null;
        return FileAccess.GetFileAsString(path);
    }
}

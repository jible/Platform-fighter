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

    // A character with no file yet reads as empty. An older layout loads as empty states, with an error,
    // since its data can't be read as phases. Upgrade it first
    public static CharacterFile ReadCharacter(CharacterId id)
    {
        string json = ReadText(id, CharacterName(id));
        CharacterFile file = json != null ? CharacterFileJson.ReadCharacter(json) : new CharacterFile();
        if (file.Version != CharacterFile.CurrentVersion)
        {
            GD.PushError($"{id}'s character file is version {file.Version}, but only version {CharacterFile.CurrentVersion} can be read");
        }
        return file;
    }

    // Where a state's sheet is, relative to the character's folder. Found by name, so every state's sheet is
    // Anims/<state name>.png, and dropping a sheet in with the right name is all it takes
    public static string AnimationName(string stateName) => $"Anims/{stateName}";

    // Null when the character has no bake, as with sprite characters. Nothing reads bakes while boxes are per sprite
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

    // The character's data with its file's overrides applied. A copy, so the Roster keeps the code's values
    // and editing it changes nothing else. Read fresh on every call, like LoadDefinition
    public static CharacterData LoadData(CharacterId id) => LoadData(id, ReadCharacter(id));

    public static CharacterData LoadData(CharacterId id, CharacterFile file)
    {
        CharacterData data = CharacterFileJson.Copy(Roster.Get(id));
        if (file.BodySize.HasValue) data.Body.Size = file.BodySize.Value;
        if (file.VisualOffset.HasValue) data.Visuals.Offset = file.VisualOffset.Value;
        // Copied, so editing the data never changes the file it came from
        if (file.Ground != null) data.Ground = CharacterFileJson.Copy(file.Ground);
        if (file.Air != null) data.Air = CharacterFileJson.Copy(file.Air);
        if (file.Jump != null) data.Jump = CharacterFileJson.Copy(file.Jump);
        if (file.Combat != null) data.Combat = CharacterFileJson.Copy(file.Combat);
        if (file.Visuals != null)
        {
            data.Visuals.Kind = file.Visuals.Kind;
            data.Visuals.DefaultAnimation = file.Visuals.DefaultAnimation;
            data.Visuals.SpriteFrameWidth = file.Visuals.SpriteFrameWidth;
            data.Visuals.PixelSize = file.Visuals.PixelSize;
        }
        return data;
    }

    // Read fresh on every call, so edits made in the hitbox editor show up on the next match.
    // Boxes are scaled by the loaded data's box unit, since the file can change the pixel size
    public static CharacterDefinition LoadDefinition(CharacterId id)
    {
        CharacterFile file = ReadCharacter(id);
        return CharacterDefinitionBuilder.Build(file, LoadData(id, file).Visuals.BoxUnit);
    }

    // New characters start as a copy of this one's files, so they play and have placeholder art right away
    public const CharacterId Template = CharacterId.Platty;

    // Whether the character has a file of its own yet. A character without one still loads, with empty states
    public static bool Exists(CharacterId id) => FileAccess.FileExists($"{ResourceFolder(id)}/{CharacterName(id)}");

    // Copies the template's folder into a new character's, renaming files named after the template, like
    // Platty.character.json. Files already in the new folder are kept, so art dropped in first isn't replaced.
    // Godot's .import and .uid files are left out, since they'd point at the template's own resources, and
    // Godot makes new ones when it next scans. Editor only, since an exported build's res:// is read only.
    // Returns why it failed, or null
    public static string CreateFromTemplate(CharacterId id)
    {
        if (!OS.HasFeature("editor")) return "Characters can only be created from the editor";
        if (id == Template) return $"{id} is the template";
        if (Exists(id)) return $"{id} already has a character file";
        if (!DirAccess.DirExistsAbsolute(ResourceFolder(Template))) return $"No template folder at {ResourceFolder(Template)}";
        return CopyFolder(ResourceFolder(Template), ResourceFolder(id), $"{Template}.", $"{id}.");
    }

    private static string CopyFolder(string from, string to, string oldPrefix, string newPrefix)
    {
        Error made = DirAccess.MakeDirRecursiveAbsolute(to);
        if (made != Error.Ok) return $"Couldn't make {to}: {made}";
        using var dir = DirAccess.Open(from);
        if (dir == null) return $"Couldn't open {from}: {DirAccess.GetOpenError()}";

        foreach (string name in dir.GetFiles())
        {
            if (name.EndsWith(".import") || name.EndsWith(".uid")) continue;
            string target = $"{to}/{(name.StartsWith(oldPrefix) ? newPrefix + name[oldPrefix.Length..] : name)}";
            if (FileAccess.FileExists(target)) continue;
            Error copied = DirAccess.CopyAbsolute($"{from}/{name}", target);
            if (copied != Error.Ok) return $"Couldn't copy {from}/{name}: {copied}";
        }
        foreach (string sub in dir.GetDirectories())
        {
            string error = CopyFolder($"{from}/{sub}", $"{to}/{sub}", oldPrefix, newPrefix);
            if (error != null) return error;
        }
        return null;
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

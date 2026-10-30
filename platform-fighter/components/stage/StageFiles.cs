using Godot;

// Reads and writes a stage's file, res://stages/<id>/<id>.stage.json, for both the stage editor and the game.
// Saved and read like CharacterFiles: in the project folder when run from the editor, and in user:// in an exported build,
// where a saved copy is read in place of the shipped one
public static class StageFiles
{
    public static string ResourceFolder(StageId id) => $"res://stages/{id}";

    private static string StageName(StageId id) => $"{id}.stage.json";

    public static string WriteFolder(StageId id) => OS.HasFeature("editor")
        ? ResourceFolder(id)
        : $"user://stages/{id}";

    // Null when the stage has no file yet
    public static StageData ReadStage(StageId id)
    {
        // A saved copy first. From the editor this is the shipped file itself
        string path = $"{WriteFolder(id)}/{StageName(id)}";
        if (!FileAccess.FileExists(path)) path = $"{ResourceFolder(id)}/{StageName(id)}";
        if (!FileAccess.FileExists(path)) return null;
        return CharacterFileJson.ReadStage(FileAccess.GetFileAsString(path));
    }

    public static void WriteStage(StageId id, StageData stage)
    {
        string folder = WriteFolder(id);
        DirAccess.MakeDirRecursiveAbsolute(folder);
        string path = $"{folder}/{StageName(id)}";
        using var access = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (access == null)
        {
            GD.PushError($"Couldn't save {path}: {FileAccess.GetOpenError()}");
            return;
        }
        access.StoreString(CharacterFileJson.WriteStage(stage));
    }
}

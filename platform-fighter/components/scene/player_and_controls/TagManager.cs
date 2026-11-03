using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

// Every player tag on this machine. Loads and saves tags.json, the only saved copy.
// Tags are plain data: the InputManager polls a player's binds every tick, so nothing is registered with Godot,
// and the number of tags costs nothing during play.
// Change tags only through these methods. Changes apply at once, but are only kept after Save
public static class TagManager
{
    public const string FilePath = "user://tags.json";
    // Where an unreadable file is moved, so saving defaults over it doesn't lose it
    public const string BrokenFilePath = "user://tags.broken.json";

    // The default tags, made when there's no file. They can be edited but not deleted, so there's always one to fall back to
    public const int Keyboard1Id = 1;
    public const int Keyboard2Id = 2;
    public const int ControllerId = 3;

    private static readonly JsonSerializerSettings settings = new()
    {
        Converters = { new StringEnumConverter() },
    };

    private static TagFile file = new();
    private static readonly Dictionary<int, InputTag> tagsById = new();

    // Raised after a tag is deleted, with its id, so players using it can fall back to a default
    public static event Action<int> TagDeleted;

    // Read only. Change tags through the methods below
    public static IEnumerable<InputTag> All => file.Tags;
    public static IEnumerable<InputTag> ByMostRecent() => file.Tags.OrderByDescending(tag => tag.LastUsed);
    // Null when there's no tag with the id
    public static InputTag Get(int id) => tagsById.GetValueOrDefault(id);
    // The tag a player gets before they pick one
    public static int DefaultFor(bool isKeyboard) => isKeyboard ? Keyboard1Id : ControllerId;

    // Called once when the game starts. Missing default tags are added back, so a fallback always exists
    public static void Load()
    {
        file = ReadFile() ?? new TagFile();
        foreach (var defaultTag in DefaultTags.Create())
        {
            if (!file.Tags.Any(tag => tag.Id == defaultTag.Id)) file.Tags.Add(defaultTag);
        }

        tagsById.Clear();
        foreach (var tag in file.Tags)
        {
            tagsById[tag.Id] = tag;
        }
        // A hand edited file could have ids past NextId
        file.NextId = Math.Max(file.NextId, file.Tags.Max(tag => tag.Id) + 1);
    }

    public static void Save()
    {
        using var access = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
        if (access == null)
        {
            GD.PushError($"Couldn't save {FilePath}: {FileAccess.GetOpenError()}");
            return;
        }
        access.StoreString(JsonConvert.SerializeObject(file, Formatting.Indented, settings));
    }

    // Starts as a copy of another tag's binds when copyFrom is given, otherwise with none
    public static InputTag Create(string name, int? copyFrom = null)
    {
        var tag = new InputTag { Id = file.NextId++, Name = name };
        if (copyFrom is int sourceId && Get(sourceId) is InputTag source)
        {
            tag.Deadzone = source.Deadzone;
            tag.InputBuffer = source.InputBuffer;
            foreach (var (action, binds) in source.Binds)
            {
                tag.Binds[action] = binds.Select(bind => bind.Copy()).ToList();
            }
        }
        file.Tags.Add(tag);
        tagsById[tag.Id] = tag;
        return tag;
    }

    public static void Rename(int id, string name)
    {
        if (Get(id) is InputTag tag) tag.Name = name;
    }

    // False for the default tags, which can't be deleted
    public static bool Delete(int id)
    {
        if (IsDefault(id) || Get(id) is not InputTag tag) return false;
        file.Tags.Remove(tag);
        tagsById.Remove(id);
        TagDeleted?.Invoke(id);
        return true;
    }

    public static bool IsDefault(int id) => id == Keyboard1Id || id == Keyboard2Id || id == ControllerId;

    // Ignores case and spaces at either end, so names that read the same can't both exist
    public static bool IsNameUsed(string name)
    {
        return file.Tags.Any(tag => string.Equals(tag.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // Binds whatever the event was, like a key the player just pressed in the tag editor.
    // False when the event can't be bound or the action already has it
    public static bool AddBind(int id, GameAction action, InputEvent inputEvent)
    {
        Bind bind = Bind.FromEvent(inputEvent);
        if (bind == null || Get(id) is not InputTag tag) return false;

        List<Bind> binds = BindListFor(tag, action);
        if (binds.Any(existing => existing.SameAs(bind))) return false;
        binds.Add(bind);
        return true;
    }

    // Like AddBind, but replaces the action's binds for the event's scheme. A key replaces keys, and
    // a controller input replaces controller inputs, so the action's other scheme is left alone.
    // False when the event can't be bound
    public static bool ReplaceBind(int id, GameAction action, InputEvent inputEvent)
    {
        Bind bind = Bind.FromEvent(inputEvent);
        if (bind == null || Get(id) is not InputTag tag) return false;

        List<Bind> binds = BindListFor(tag, action);
        binds.RemoveAll(existing => (existing.Type == BindType.Key) == (bind.Type == BindType.Key));
        binds.Add(bind);
        return true;
    }

    // The d-pad always stays on move, whichever stick it's set to
    public static void SetMoveStick(int id, ControllerState.StickTypes stick)
    {
        if (Get(id) is not InputTag tag) return;
        SetStick(tag, GameAction.MoveUp, GameAction.MoveDown, GameAction.MoveLeft, GameAction.MoveRight, stick);
        AddDpadBind(tag, GameAction.MoveUp, JoyButton.DpadUp);
        AddDpadBind(tag, GameAction.MoveDown, JoyButton.DpadDown);
        AddDpadBind(tag, GameAction.MoveLeft, JoyButton.DpadLeft);
        AddDpadBind(tag, GameAction.MoveRight, JoyButton.DpadRight);
    }

    public static void SetAimStick(int id, ControllerState.StickTypes stick)
    {
        if (Get(id) is not InputTag tag) return;
        SetStick(tag, GameAction.AimUp, GameAction.AimDown, GameAction.AimLeft, GameAction.AimRight, stick);
    }

    // Null when the tag has no stick on the action
    public static ControllerState.StickTypes? MoveStick(int id) => StickFor(id, GameAction.MoveUp);
    public static ControllerState.StickTypes? AimStick(int id) => StickFor(id, GameAction.AimUp);

    private static ControllerState.StickTypes? StickFor(int id, GameAction upAction)
    {
        if (Get(id) is not InputTag tag) return null;
        Bind axis = tag.BindsFor(upAction).FirstOrDefault(bind => bind.Type == BindType.JoyAxis);
        if (axis?.Name == nameof(JoyAxis.LeftY)) return ControllerState.StickTypes.LEFT;
        if (axis?.Name == nameof(JoyAxis.RightY)) return ControllerState.StickTypes.RIGHT;
        return null;
    }

    private static void SetStick(InputTag tag, GameAction up, GameAction down, GameAction left, GameAction right, ControllerState.StickTypes stick)
    {
        bool isLeft = stick == ControllerState.StickTypes.LEFT;
        JoyAxis xAxis = isLeft ? JoyAxis.LeftX : JoyAxis.RightX;
        JoyAxis yAxis = isLeft ? JoyAxis.LeftY : JoyAxis.RightY;
        SetAxisBind(tag, up, Bind.JoyAxis(yAxis, -1));
        SetAxisBind(tag, down, Bind.JoyAxis(yAxis, 1));
        SetAxisBind(tag, left, Bind.JoyAxis(xAxis, -1));
        SetAxisBind(tag, right, Bind.JoyAxis(xAxis, 1));
    }

    private static void SetAxisBind(InputTag tag, GameAction action, Bind axisBind)
    {
        List<Bind> binds = BindListFor(tag, action);
        binds.RemoveAll(existing => existing.Type == BindType.JoyAxis);
        binds.Add(axisBind);
    }

    private static void AddDpadBind(InputTag tag, GameAction action, JoyButton button)
    {
        List<Bind> binds = BindListFor(tag, action);
        Bind bind = Bind.JoyButton(button);
        if (!binds.Any(existing => existing.SameAs(bind))) binds.Add(bind);
    }

    // Made when the action has no binds yet
    private static List<Bind> BindListFor(InputTag tag, GameAction action)
    {
        if (!tag.Binds.TryGetValue(action, out var binds)) tag.Binds[action] = binds = new List<Bind>();
        return binds;
    }

    // index is the bind's position in the tag's binds for the action
    public static void RemoveBind(int id, GameAction action, int index)
    {
        if (Get(id) is not InputTag tag || !tag.Binds.TryGetValue(action, out var binds)) return;
        if (index < 0 || index >= binds.Count) return;
        binds.RemoveAt(index);
    }

    public static void ClearBinds(int id, GameAction action)
    {
        Get(id)?.Binds.Remove(action);
    }

    public static void SetDeadzone(int id, float deadzone)
    {
        if (Get(id) is InputTag tag) tag.Deadzone = Mathf.Clamp(deadzone, 0, 1);
    }

    public static void SetInputBuffer(int id, int ticks)
    {
        if (Get(id) is InputTag tag) tag.InputBuffer = Mathf.Clamp(ticks, 0, InputHandler.MaxInputBuffer);
    }

    // Moves the tag to the front of ByMostRecent. Saves right away, so the order is kept even if nothing else is saved
    public static void MarkUsed(int id)
    {
        if (Get(id) is not InputTag tag) return;
        tag.LastUsed = ++file.UseCounter;
        Save();
    }

    // Null when there's no file. An unreadable file is moved aside instead of being overwritten later
    private static TagFile ReadFile()
    {
        if (!FileAccess.FileExists(FilePath)) return null;
        try
        {
            return JsonConvert.DeserializeObject<TagFile>(FileAccess.GetFileAsString(FilePath), settings);
        }
        catch (JsonException exception)
        {
            GD.PushError($"{FilePath} couldn't be read, so the default tags are used. It was moved to {BrokenFilePath}: {exception.Message}");
            DirAccess.RenameAbsolute(FilePath, BrokenFilePath);
            return null;
        }
    }
}

// The tags every machine starts with. Keyboard 1 and Keyboard 2 don't share keys, so two players can share a keyboard
public static class DefaultTags
{
    public static InputTag[] Create() => new[]
    {
        new InputTag
        {
            Id = TagManager.Keyboard1Id,
            Name = "Keyboard 1",
            Binds =
            {
                [GameAction.Jump] = new() { Bind.Key(Key.Space) },
                [GameAction.Light] = new() { Bind.Key(Key.E) },
                [GameAction.Special] = new() { Bind.Key(Key.Shift) },
                [GameAction.Grab] = new() { Bind.Key(Key.Q) },
                [GameAction.MoveUp] = new() { Bind.Key(Key.W) },
                [GameAction.MoveDown] = new() { Bind.Key(Key.S) },
                [GameAction.MoveLeft] = new() { Bind.Key(Key.A) },
                [GameAction.MoveRight] = new() { Bind.Key(Key.D) },
                [GameAction.AimUp] = new() { Bind.Key(Key.I) },
                [GameAction.AimDown] = new() { Bind.Key(Key.K) },
                [GameAction.AimLeft] = new() { Bind.Key(Key.J) },
                [GameAction.AimRight] = new() { Bind.Key(Key.L) },
            },
        },
        new InputTag
        {
            Id = TagManager.Keyboard2Id,
            Name = "Keyboard 2",
            Binds =
            {
                [GameAction.Jump] = new() { Bind.Key(Key.Kp0) },
                [GameAction.Light] = new() { Bind.Key(Key.Kp1) },
                [GameAction.Special] = new() { Bind.Key(Key.Kp2) },
                [GameAction.Grab] = new() { Bind.Key(Key.Kp3) },
                [GameAction.MoveUp] = new() { Bind.Key(Key.Up) },
                [GameAction.MoveDown] = new() { Bind.Key(Key.Down) },
                [GameAction.MoveLeft] = new() { Bind.Key(Key.Left) },
                [GameAction.MoveRight] = new() { Bind.Key(Key.Right) },
                [GameAction.AimUp] = new() { Bind.Key(Key.Kp8) },
                [GameAction.AimDown] = new() { Bind.Key(Key.Kp5) },
                [GameAction.AimLeft] = new() { Bind.Key(Key.Kp4) },
                [GameAction.AimRight] = new() { Bind.Key(Key.Kp6) },
            },
        },
        new InputTag
        {
            Id = TagManager.ControllerId,
            Name = "Controller",
            Binds =
            {
                [GameAction.Jump] = new() { Bind.JoyButton(JoyButton.X) },
                [GameAction.Light] = new() { Bind.JoyButton(JoyButton.A) },
                [GameAction.Special] = new() { Bind.JoyButton(JoyButton.B) },
                [GameAction.Grab] = new() { Bind.JoyButton(JoyButton.RightShoulder) },
                [GameAction.MoveUp] = new() { Bind.JoyAxis(JoyAxis.LeftY, -1), Bind.JoyButton(JoyButton.DpadUp) },
                [GameAction.MoveDown] = new() { Bind.JoyAxis(JoyAxis.LeftY, 1), Bind.JoyButton(JoyButton.DpadDown) },
                [GameAction.MoveLeft] = new() { Bind.JoyAxis(JoyAxis.LeftX, -1), Bind.JoyButton(JoyButton.DpadLeft) },
                [GameAction.MoveRight] = new() { Bind.JoyAxis(JoyAxis.LeftX, 1), Bind.JoyButton(JoyButton.DpadRight) },
                [GameAction.AimUp] = new() { Bind.JoyAxis(JoyAxis.RightY, -1) },
                [GameAction.AimDown] = new() { Bind.JoyAxis(JoyAxis.RightY, 1) },
                [GameAction.AimLeft] = new() { Bind.JoyAxis(JoyAxis.RightX, -1) },
                [GameAction.AimRight] = new() { Bind.JoyAxis(JoyAxis.RightX, 1) },
            },
        },
    };
}

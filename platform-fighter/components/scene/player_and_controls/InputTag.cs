using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// The data half of player tags: what tags.json holds. Read and written by the TagManager,
// which is the only thing that should change these. The InputManager polls a player's binds every tick

// What a tag can bind. Saved by name, so renaming a value breaks existing files
public enum GameAction
{
    Jump,
    Light,
    Special,
    Grab,
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    AimUp,
    AimDown,
    AimLeft,
    AimRight,
}

// Saved by name, so renaming a value breaks existing files
public enum BindType
{
    Key,
    JoyButton,
    JoyAxis,
}

// One key, button, or stick direction that triggers an action
public class Bind
{
    public const long InvalidCode = -1;

    public BindType Type;
    // The name of the Godot enum value: a Key, JoyButton, or JoyAxis.
    // Keys are physical keys, so a bind is the same spot on any keyboard layout
    public string Name = "";
    // Joy axes only: which way the axis is pushed, -1 or 1. Up and left are -1, as Godot reports them
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int Direction;

    public static Bind Key(Key key) => new() { Type = BindType.Key, Name = key.ToString() };
    public static Bind JoyButton(JoyButton button) => new() { Type = BindType.JoyButton, Name = button.ToString() };
    public static Bind JoyAxis(JoyAxis axis, int direction) => new() { Type = BindType.JoyAxis, Name = axis.ToString(), Direction = direction };

    // Null for events a tag can't bind, like the mouse or a stick at rest
    public static Bind FromEvent(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey key:
                return Key(key.PhysicalKeycode != Godot.Key.None ? key.PhysicalKeycode : key.Keycode);
            case InputEventJoypadButton button:
                return JoyButton(button.ButtonIndex);
            case InputEventJoypadMotion motion when motion.AxisValue != 0:
                return JoyAxis(motion.Axis, Math.Sign(motion.AxisValue));
            default:
                return null;
        }
    }

    // Name as its enum's value: cast to Key, JoyButton, or JoyAxis by Type. InvalidCode when Name isn't a value of
    // its enum, like from a hand edited file. Parsed on first use, so polling never parses strings
    [JsonIgnore]
    public long Code => code ??= ParseCode();
    private long? code;

    private long ParseCode()
    {
        long parsed = Type switch
        {
            BindType.Key when Enum.TryParse(Name, out Key key) => (long)key,
            BindType.JoyButton when Enum.TryParse(Name, out JoyButton button) => (long)button,
            BindType.JoyAxis when Enum.TryParse(Name, out JoyAxis axis) && Direction != 0 => (long)axis,
            _ => InvalidCode,
        };
        if (parsed == InvalidCode)
        {
            GD.PushWarning($"A bind Godot doesn't know is ignored: {Type} {Name}");
        }
        return parsed;
    }

    public bool SameAs(Bind other) => Type == other.Type && Name == other.Name && Direction == other.Direction;

    public Bind Copy() => new() { Type = Type, Name = Name, Direction = Direction };
}

// A player's named set of binds. The id never changes, so anything that stores a tag stores its id.
// The name is only for display, so it can hold any characters
public class InputTag
{
    public int Id;
    public string Name = "";
    // Order of last use, higher is more recent. See TagManager.MarkUsed
    public long LastUsed;
    // Stick distance before a stick bind registers, from 0 to 1
    public float Deadzone = 0.2f;
    public Dictionary<GameAction, List<Bind>> Binds = new();

    // Binds for an action, empty when it has none
    public IReadOnlyList<Bind> BindsFor(GameAction action) => Binds.TryGetValue(action, out var binds) ? binds : Array.Empty<Bind>();
}

// The whole of tags.json
public class TagFile
{
    // Bump when the layout changes, so old files can be upgraded on load
    public int Version = 1;
    // Handed to the next tag created, so ids are never reused
    public int NextId = 1;
    // Handed out by MarkUsed, so LastUsed orders tags by use
    public long UseCounter;
    public List<InputTag> Tags = new();
}

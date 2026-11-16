#if TOOLS
using Godot;

// Adds the Character Tools dock to the Godot editor. Editor only, so none of it is in an exported build.
// Enable it in Project Settings > Plugins
[Tool]
public partial class CharacterToolsPlugin : EditorPlugin
{
    private EditorDock dock;

    public override void _EnterTree()
    {
        dock = new EditorDock
        {
            Title = "Characters",
            DefaultSlot = EditorDock.DockSlot.RightUl,
        };
        dock.AddChild(new CharacterToolsDock());
        AddDock(dock);
    }

    public override void _ExitTree()
    {
        RemoveDock(dock);
        dock.QueueFree();
        dock = null;
    }
}
#endif

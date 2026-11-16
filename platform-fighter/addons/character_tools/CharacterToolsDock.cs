#if TOOLS
using System;
using System.Linq;
using Godot;

// Lists the CharacterIds with no character file yet and makes one's folder from the template.
// A new enum value only shows up here after building, since the editor runs the last built code
[Tool]
public partial class CharacterToolsDock : VBoxContainer
{
    private OptionButton missing;
    private Button create;
    private Label status;

    private const float LabelWidth = 200;

    public override void _Ready()
    {
        AddChild(new Label
        {
            Text = $"Characters with no folder yet. Creating one copies {CharacterFiles.Template}'s files and art as placeholders",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            // A wrapping label with no width wraps every letter, and its height pushes the editor's other docks away
            CustomMinimumSize = new Vector2(LabelWidth, 0),
        });

        missing = new OptionButton();
        AddChild(missing);

        var buttons = new HBoxContainer();
        create = new Button { Text = $"Create from {CharacterFiles.Template}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        create.Pressed += Create;
        buttons.AddChild(create);
        var refresh = new Button { Text = "Refresh" };
        refresh.Pressed += Refresh;
        buttons.AddChild(refresh);
        AddChild(buttons);

        status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(LabelWidth, 0) };
        AddChild(status);

        Refresh();
    }

    public override void _Notification(int what)
    {
        // So an id added and built while the dock was hidden shows up when it's opened
        if (what == NotificationVisibilityChanged && IsVisibleInTree() && missing != null) Refresh();
    }

    private void Refresh()
    {
        missing.Clear();
        foreach (CharacterId id in Enum.GetValues<CharacterId>().Where(id => !CharacterFiles.Exists(id)))
        {
            missing.AddItem(id.ToString(), (int)id);
        }
        bool any = missing.ItemCount > 0;
        create.Disabled = !any;
        if (!any) missing.AddItem("Every character has a folder");
        missing.Disabled = !any;
    }

    private void Create()
    {
        if (missing.Selected < 0) return;
        var id = (CharacterId)missing.GetItemId(missing.Selected);
        string error = CharacterFiles.CreateFromTemplate(id);
        if (error != null)
        {
            status.Text = error;
            GD.PushError(error);
            return;
        }
        // Imports the copied art, so it loads the next time the game runs
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        status.Text = $"Created {CharacterFiles.ResourceFolder(id)}. Replace its sheets in Anims/ with the same names to swap in the new art";
        GD.Print(status.Text);
        Refresh();
    }
}
#endif

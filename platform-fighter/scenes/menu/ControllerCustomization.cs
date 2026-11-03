using Godot;
using System;
using System.Linq;

// Edits tags through the TagManager. Edits apply to the tags in memory straight away, so Save keeps them
// and Back drops them by reloading the file. Default tags can be looked at but not changed
public partial class ControllerCustomization : Control
{
    [Export] OptionButton TagDropdown;
    [Export] Button NewTagButton;
    [Export] OptionButton ModeDropdown;
    [Export] PackedScene RowScene;
    [Export] Container Rows;
    [Export] Button SaveButton;
    [Export] Button BackButton;
    [Export] Control BindOverlay;
    [Export] Label BindPrompt;
    [Export] ConfirmationDialog NewTagDialog;
    [Export] LineEdit NewTagName;
    [Export] Label NewTagError;

    string ModeSelectScenePath = "uid://cwlsejxx2roi7";

    const int KeyboardModeItem = 0;
    const int ControllerModeItem = 1;

    // The actions with their own row in controller mode. Directions are set per stick instead
    static readonly GameAction[] ControllerButtonActions = { GameAction.Jump, GameAction.Light, GameAction.Special, GameAction.Grab };

    // Null unless the overlay is waiting for an input to bind to this action
    private GameAction? bindingAction;

    private int SelectedTag => TagDropdown.GetSelectedId();
    private bool InControllerMode => ModeDropdown.GetSelectedId() == ControllerModeItem;

    public override void _Ready()
    {
        ModeDropdown.AddItem("Keyboard", KeyboardModeItem);
        ModeDropdown.AddItem("Controller", ControllerModeItem);
        RefreshTags(TagManager.DefaultFor(true));

        TagDropdown.ItemSelected += index => RefreshRows();
        ModeDropdown.ItemSelected += index => RefreshRows();
        NewTagButton.Pressed += OpenNewTagDialog;
        NewTagDialog.Confirmed += OnNewTagConfirmed;
        NewTagDialog.RegisterTextEnter(NewTagName);
        SaveButton.Pressed += TagManager.Save;
        BackButton.Pressed += () =>
        {
            TagManager.Load();
            GetTree().ChangeSceneToFile(ModeSelectScenePath);
        };
    }

    // While binding, every input goes here, so a key can't also press whichever button has focus
    public override void _Input(InputEvent @event)
    {
        if (bindingAction is not GameAction action) return;
        GetViewport().SetInputAsHandled();

        // The mouse can't be bound, so clicking cancels without giving up a key
        if (@event is InputEventMouseButton { Pressed: true })
        {
            StopBinding();
        }
        else if (CanBindInCurrentMode(@event))
        {
            TagManager.ReplaceBind(SelectedTag, action, @event);
            StopBinding();
            RefreshRows();
        }
    }

    private bool CanBindInCurrentMode(InputEvent @event)
    {
        if (!InControllerMode) return @event is InputEventKey { Pressed: true, Echo: false };
        return @event switch
        {
            InputEventJoypadButton button => button.Pressed,
            // Sticks are set with the stick dropdowns, so triggers are the only axes that act as buttons
            InputEventJoypadMotion motion => motion.Axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight && motion.AxisValue > TagManager.Get(SelectedTag).Deadzone,
            _ => false,
        };
    }

    private void StartBinding(GameAction action)
    {
        bindingAction = action;
        BindPrompt.Text = $"Press a button to bind the button to {ActionName(action)}";
        BindOverlay.Visible = true;
    }

    private void StopBinding()
    {
        bindingAction = null;
        BindOverlay.Visible = false;
    }

    private void OpenNewTagDialog()
    {
        NewTagName.Text = "";
        NewTagError.Visible = false;
        NewTagDialog.PopupCentered();
        NewTagName.GrabFocus();
    }

    private void OnNewTagConfirmed()
    {
        string name = NewTagName.Text.Trim();
        if (name == "" || TagManager.IsNameUsed(name))
        {
            NewTagError.Text = name == "" ? "You may not use a blank name" : "You may not use a name that is already used";
            NewTagError.Visible = true;
            return;
        }

        InputTag tag = TagManager.Create(name, SelectedTag);
        NewTagDialog.Hide();
        RefreshTags(tag.Id);
    }

    private void RefreshTags(int selectedId)
    {
        TagDropdown.Clear();
        foreach (var tag in TagManager.ByMostRecent())
        {
            TagDropdown.AddItem(tag.Name, tag.Id);
        }
        TagDropdown.Select(TagDropdown.GetItemIndex(selectedId));
        RefreshRows();
    }

    private void RefreshRows()
    {
        foreach (var row in Rows.GetChildren())
        {
            row.QueueFree();
        }

        bool editable = !TagManager.IsDefault(SelectedTag);
        SaveButton.Disabled = !editable;
        if (InControllerMode)
        {
            foreach (var action in ControllerButtonActions)
            {
                AddBindRow(action, editable);
            }
            AddStickRow("Move", TagManager.MoveStick(SelectedTag), TagManager.SetMoveStick, editable);
            AddStickRow("Aim", TagManager.AimStick(SelectedTag), TagManager.SetAimStick, editable);
        }
        else
        {
            foreach (var action in Enum.GetValues<GameAction>())
            {
                AddBindRow(action, editable);
            }
        }
    }

    private BindRow AddRow()
    {
        BindRow row = RowScene.Instantiate<BindRow>();
        Rows.AddChild(row);
        return row;
    }

    private void AddBindRow(GameAction action, bool editable)
    {
        BindRow row = AddRow();
        row.ShowBind(ActionName(action), BoundTo(action), editable);
        row.ChangePressed += () => StartBinding(action);
    }

    private void AddStickRow(string inputName, ControllerState.StickTypes? stick, Action<int, ControllerState.StickTypes> setStick, bool editable)
    {
        BindRow row = AddRow();
        row.ShowStick(inputName, stick, editable);
        row.StickSelected += picked => setStick(SelectedTag, picked);
    }

    // Only the current mode's binds, since one action can have binds in both
    private string BoundTo(GameAction action)
    {
        var binds = TagManager.Get(SelectedTag).BindsFor(action).Where(bind => (bind.Type == BindType.Key) != InControllerMode);
        return binds.Any() ? string.Join(", ", binds.Select(bind => bind.Name)) : "Not bound";
    }

    private static string ActionName(GameAction action) => action.ToString().Capitalize();
}

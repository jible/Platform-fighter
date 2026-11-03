using Godot;
using System;

// One row on the controller customization screen: an input's name, then either what it's bound to
// with a button to change it, or which stick it reads from
public partial class BindRow : HBoxContainer
{
    [Export] Label NameLabel;
    [Export] Label BindLabel;
    [Export] Button ChangeButton;
    [Export] OptionButton StickDropdown;

    public event Action ChangePressed;
    public event Action<ControllerState.StickTypes> StickSelected;

    public override void _Ready()
    {
        StickDropdown.AddItem("Left Stick", (int)ControllerState.StickTypes.LEFT);
        StickDropdown.AddItem("Right Stick", (int)ControllerState.StickTypes.RIGHT);

        ChangeButton.Pressed += () => ChangePressed?.Invoke();
        StickDropdown.ItemSelected += index => StickSelected?.Invoke((ControllerState.StickTypes)StickDropdown.GetItemId((int)index));
    }

    public void ShowBind(string inputName, string boundTo, bool editable)
    {
        NameLabel.Text = inputName;
        BindLabel.Text = boundTo;
        ChangeButton.Disabled = !editable;
        StickDropdown.Visible = false;
    }

    public void ShowStick(string inputName, ControllerState.StickTypes? stick, bool editable)
    {
        NameLabel.Text = inputName;
        BindLabel.Visible = false;
        ChangeButton.Visible = false;
        StickDropdown.Disabled = !editable;
        // -1 shows nothing picked, for a tag with no stick on this input
        StickDropdown.Select(stick is ControllerState.StickTypes picked ? StickDropdown.GetItemIndex((int)picked) : -1);
    }
}

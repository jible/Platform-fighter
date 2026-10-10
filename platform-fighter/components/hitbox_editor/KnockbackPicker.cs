using Godot;
using System;
using System.Globalization;

// Picks a knockback vector by clicking in a circle: the center is no knockback, the edge is MaxMagnitude.
// Keeps its X and Y fields in sync both ways, so clicking writes the fields and typing moves the line.
// Editor UI only. Y is up, like the simulation, so clicking above the center knocks upward
[GlobalClass]
public partial class KnockbackPicker : Control
{
    // Placeholder until knockback has a real scale
    [Export] public float MaxMagnitude = 10;
    [Export] public LineEdit XInput;
    [Export] public LineEdit YInput;

    [Export] public Color FillColor = new(0.1f, 0.1f, 0.12f);
    [Export] public Color RingColor = new(0.45f, 0.45f, 0.5f);
    [Export] public Color LineColor = new(1f, 0.3f, 0.3f);

    // Raised when the value is picked or typed. Not raised by SetValue
    public event Action<Vector2> ValueChanged;

    public Vector2 Value { get; private set; }

    private Vector2 Center => Size / 2;
    private float Radius => Mathf.Min(Size.X, Size.Y) / 2 - 2;

    public override void _Ready()
    {
        XInput.TextSubmitted += _ => ReadFields();
        XInput.FocusExited += ReadFields;
        YInput.TextSubmitted += _ => ReadFields();
        YInput.FocusExited += ReadFields;
        SetValue(Vector2.Zero);
    }

    // For showing a box's saved knockback. Never raises ValueChanged
    public void SetValue(Vector2 value)
    {
        Value = value;
        WriteFields();
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        // Dragging keeps picking, so the line follows the mouse while the button is held
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
        {
            Pick(click.Position);
            AcceptEvent();
        }
        else if (inputEvent is InputEventMouseMotion motion && motion.ButtonMask.HasFlag(MouseButtonMask.Left))
        {
            Pick(motion.Position);
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        DrawCircle(Center, Radius, FillColor);
        DrawArc(Center, Radius, 0, Mathf.Tau, 48, RingColor, 1.5f, true);

        // A typed value past the max still saves as typed, the line just stops at the edge
        Vector2 offset = ToScreen(Value) / MaxMagnitude * Radius;
        if (offset.Length() > Radius) offset = offset.Normalized() * Radius;
        DrawLine(Center, Center + offset, LineColor, 2, true);
        DrawCircle(Center, 3, RingColor);
    }

    private void Pick(Vector2 position)
    {
        Vector2 offset = position - Center;
        // Past the edge holds the max in that direction
        if (offset.Length() > Radius) offset = offset.Normalized() * Radius;
        Change(ToScreen(offset) / Radius * MaxMagnitude);
    }

    // Typed values the fields can't read are put back to the current value
    private void ReadFields()
    {
        if (TryRead(XInput.Text, out float x) && TryRead(YInput.Text, out float y))
        {
            if (x != Value.X || y != Value.Y) Change(new Vector2(x, y));
        }
        else
        {
            WriteFields();
        }
    }

    private void Change(Vector2 value)
    {
        SetValue(value);
        ValueChanged?.Invoke(value);
    }

    private void WriteFields()
    {
        XInput.Text = Value.X.ToString("0.###", CultureInfo.InvariantCulture);
        YInput.Text = Value.Y.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool TryRead(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    // Screen y points down, knockback y points up. Flipping is its own inverse, so this converts both ways
    private static Vector2 ToScreen(Vector2 vector) => new(vector.X, -vector.Y);
}

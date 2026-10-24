using Godot;
using System;
using System.Globalization;

// Lets a number field's label be dragged to change the field, like Unity's or Blender's labels.
// Each step writes the new value into the field and hands it to the field's own edit handler,
// so a scrub is applied exactly like typing that value would be.
// Plain C#, made by the HitboxEditor. The label holds its handlers, so nothing needs disposing
public class FieldScrubber
{
    // Mouse travel for one step, so small wobbles don't change the value
    private const float PixelsPerStep = 4f;
    // Held while dragging to make each step bigger
    private const decimal FastMultiplier = 10m;

    private readonly LineEdit field;
    private readonly Action<string> handler;
    private readonly decimal step;
    // Dragging up raises the value instead of dragging right, for fields that move things vertically
    private readonly bool vertical;

    private bool dragging;
    private decimal value;
    // Travel not yet turned into a step
    private float pixels;

    public FieldScrubber(Label label, LineEdit field, Action<string> handler, decimal step, bool vertical)
    {
        this.field = field;
        this.handler = handler;
        this.step = step;
        this.vertical = vertical;
        label.MouseFilter = Control.MouseFilterEnum.Stop;
        label.MouseDefaultCursorShape = vertical ? Control.CursorShape.Vsize : Control.CursorShape.Hsize;
        label.GuiInput += OnGuiInput;
    }

    // The label keeps getting the mouse's events while the button is held, even once it leaves the label
    private void OnGuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left)
        {
            if (button.Pressed) Begin();
            else dragging = false;
        }
        else if (dragging && input is InputEventMouseMotion motion)
        {
            // Screen y grows downwards
            Drag(vertical ? -motion.Relative.Y : motion.Relative.X, motion.ShiftPressed);
        }
    }

    // Starts from what the field shows, so a field that's disabled or showing nothing can't be scrubbed
    private void Begin()
    {
        dragging = field.Editable
            && decimal.TryParse(field.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        pixels = 0;
    }

    private void Drag(float travel, bool fast)
    {
        pixels += travel;
        int steps = (int)(pixels / PixelsPerStep);
        if (steps == 0) return;
        pixels -= steps * PixelsPerStep;
        value += steps * step * (fast ? FastMultiplier : 1m);
        string text = value.ToString(CultureInfo.InvariantCulture);
        field.Text = text;
        handler(text);
    }
}

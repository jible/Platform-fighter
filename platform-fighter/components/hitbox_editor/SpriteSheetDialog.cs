using Godot;
using System;

// The Set Anim popup: picks a state's sheet, how it's cut into frames and how fast its sequence plays.
// Frame size and the column and row counts are two views of the same grid, so editing one updates the other,
// and only the counts are submitted. Builds its own controls. Editor UI only
public partial class SpriteSheetDialog : ConfirmationDialog
{
    // Sheet name relative to the character's folder, columns, rows and frame rate
    public event Action<string, int, int, int> Submitted;

    private static readonly Color WarningColor = new(1f, 0.75f, 0.3f);

    private string folder;
    // Null while the typed path has no sheet
    private Texture2D sheet;
    // Set while one grid field updates the others, so their own change handlers don't run
    private bool syncing;

    private LineEdit pathInput;
    private Label sheetLabel, gridWarning, frameRateHint;
    private SpinBox columnsInput, rowsInput, widthInput, heightInput, frameRateInput;

    private int SheetWidth => sheet?.GetWidth() ?? 1;
    private int SheetHeight => sheet?.GetHeight() ?? 1;

    public override void _Ready()
    {
        Title = "Set Animation";
        OkButtonText = "Submit";
        CancelButtonText = "Cancel";
        Confirmed += OnConfirmed;

        var grid = new GridContainer { Columns = 2 };
        AddChild(grid);
        pathInput = AddRow(grid, "Sheet:", new LineEdit { CustomMinimumSize = new Vector2(220, 0), PlaceholderText = "Jab.png" });
        sheetLabel = AddRow(grid, "", new Label());
        columnsInput = AddRow(grid, "Columns:", NewSpinBox());
        rowsInput = AddRow(grid, "Rows:", NewSpinBox());
        widthInput = AddRow(grid, "Frame width:", NewSpinBox("px"));
        heightInput = AddRow(grid, "Frame height:", NewSpinBox("px"));
        gridWarning = AddRow(grid, "", new Label { Modulate = WarningColor });
        frameRateInput = AddRow(grid, "Frame rate:", NewSpinBox("fps"));
        frameRateInput.MaxValue = TickManager.TicksPerSecond;
        frameRateHint = AddRow(grid, "", new Label());

        pathInput.TextChanged += _ => OnPathChanged();
        columnsInput.ValueChanged += _ => OnCountChanged();
        rowsInput.ValueChanged += _ => OnCountChanged();
        widthInput.ValueChanged += _ => OnSizeChanged();
        heightInput.ValueChanged += _ => OnSizeChanged();
        frameRateInput.ValueChanged += _ => ShowFrameRateHint();
    }

    // folder is where sheet names are looked up, see SpriteCharacterView.SheetPath
    public void Open(string folder, string sheetName, int columns, int rows, int frameRate)
    {
        this.folder = folder;
        pathInput.Text = sheetName;
        LoadSheet();
        syncing = true;
        columnsInput.Value = columns;
        rowsInput.Value = rows;
        frameRateInput.Value = SpriteSequenceMath.ClampFrameRate(frameRate);
        syncing = false;
        OnCountChanged();
        ShowFrameRateHint();
        PopupCentered();
    }

    private void OnPathChanged()
    {
        LoadSheet();
        OnCountChanged();
    }

    // Counts set the size, rounded down when the sheet doesn't split evenly
    private void OnCountChanged()
    {
        if (syncing) return;
        syncing = true;
        widthInput.Value = SheetWidth / Math.Max(1, (int)columnsInput.Value);
        heightInput.Value = SheetHeight / Math.Max(1, (int)rowsInput.Value);
        syncing = false;
        ShowGridWarning();
    }

    // Sizes set the counts. The size itself is left as typed, so a mismatch shows as a warning instead of jumping
    private void OnSizeChanged()
    {
        if (syncing) return;
        syncing = true;
        columnsInput.Value = Math.Max(1, SheetWidth / Math.Max(1, (int)widthInput.Value));
        rowsInput.Value = Math.Max(1, SheetHeight / Math.Max(1, (int)heightInput.Value));
        syncing = false;
        ShowGridWarning();
    }

    private void LoadSheet()
    {
        string path = SpriteCharacterView.SheetPath(folder, pathInput.Text.Trim());
        sheet = pathInput.Text.Trim() != "" && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        sheetLabel.Text = sheet != null ? $"{SheetWidth} x {SheetHeight} px" : $"No sheet at {path}";
        sheetLabel.Modulate = sheet != null ? Colors.White : WarningColor;

        // The size fields mean nothing without a sheet to measure
        widthInput.Editable = sheet != null;
        heightInput.Editable = sheet != null;
        columnsInput.MaxValue = SheetWidth;
        rowsInput.MaxValue = SheetHeight;
        widthInput.MaxValue = SheetWidth;
        heightInput.MaxValue = SheetHeight;
        GetOkButton().Disabled = sheet == null;
    }

    private void ShowGridWarning()
    {
        int columns = (int)columnsInput.Value;
        int rows = (int)rowsInput.Value;
        bool even = SheetWidth % columns == 0 && SheetHeight % rows == 0
            && (int)widthInput.Value * columns == SheetWidth
            && (int)heightInput.Value * rows == SheetHeight;
        gridWarning.Text = sheet == null || even ? "" : "The sheet isn't a whole number of frames this size";
        gridWarning.Visible = gridWarning.Text != "";
    }

    // Ticks are what boxes are timed in, so this is the number that matters for tuning
    private void ShowFrameRateHint()
    {
        int frameRate = SpriteSequenceMath.ClampFrameRate((int)frameRateInput.Value);
        int ticks = TickManager.TicksPerSecond / frameRate;
        frameRateHint.Text = TickManager.TicksPerSecond % frameRate == 0
            ? $"{ticks} ticks per frame"
            : $"{ticks} to {ticks + 1} ticks per frame, uneven";
    }

    private void OnConfirmed()
    {
        if (sheet == null) return;
        Submitted?.Invoke(pathInput.Text.Trim(), (int)columnsInput.Value, (int)rowsInput.Value, (int)frameRateInput.Value);
    }

    private static T AddRow<T>(GridContainer grid, string label, T control) where T : Control
    {
        grid.AddChild(new Label { Text = label });
        grid.AddChild(control);
        return control;
    }

    private static SpinBox NewSpinBox(string suffix = "")
    {
        return new SpinBox { MinValue = 1, Step = 1, Rounded = true, Suffix = suffix, CustomMinimumSize = new Vector2(120, 0) };
    }
}

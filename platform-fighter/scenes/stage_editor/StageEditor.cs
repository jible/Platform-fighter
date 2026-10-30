using Godot;
using System;

// Edits a stage's collision blocks and saves them to its file. No simulation runs: the blocks are drawn as plain meshes,
// redrawn on every edit, so it shows the layout without physics. The camera matches the play scene's, so what fits here fits in a match.
// A floating panel like the property editor's, built in code
public partial class StageEditor : Node3D
{
    // How deep the blocks are drawn, matching the StageHolder
    private const float BlockDepth = 2;
    private const decimal DMScale = 4294967296m; // 2^32, DM64's fractional bits

    private static readonly Color SolidColor = new(0.55f, 0.55f, 0.6f);
    private static readonly Color PlatformColor = new(0.35f, 0.6f, 0.95f, 0.6f);
    private static readonly Color SelectedTint = new(1f, 0.8f, 0.3f);

    private StageId stageId = StageId.Test;
    private StageData stage;
    private bool unsaved;

    private Node3D blockMeshes;
    private OptionButton stageSelect;
    private OptionButton collisionSelect;
    private Button removeButton;
    private LineEdit nameInput;
    private SpinBox positionX, positionY, sizeX, sizeY;
    private CheckBox platformCheck;
    private Label unsavedLabel;

    private StageBlock Selected => stage.Blocks.Count > 0 ? stage.Blocks[collisionSelect.Selected] : null;

    public override void _Ready()
    {
        blockMeshes = new Node3D { Name = "Blocks" };
        AddChild(blockMeshes);
        BuildPanel();

        foreach (StageId id in Enum.GetValues<StageId>())
        {
            stageSelect.AddItem(id.ToString(), (int)id);
        }
        stageSelect.ItemSelected += index =>
        {
            stageId = (StageId)stageSelect.GetItemId((int)index);
            LoadStage();
        };
        LoadStage();
    }

    // Unsaved edits are dropped
    private void LoadStage()
    {
        stage = Stages.Get(stageId);
        SetUnsaved(false);
        RefreshCollisionList(0);
    }

    private void Save()
    {
        StageFiles.WriteStage(stageId, stage);
        SetUnsaved(false);
        GD.Print($"Saved the {stageId} stage");
    }

    private void AddBlock()
    {
        stage.Blocks.Add(new StageBlock(UniqueName("Collision"), new DMVector(0, 5), new DMVector(4, 1)));
        Edited();
        RefreshCollisionList(stage.Blocks.Count - 1);
    }

    private void RemoveBlock()
    {
        if (Selected == null) return;
        int index = collisionSelect.Selected;
        stage.Blocks.RemoveAt(index);
        Edited();
        RefreshCollisionList(Math.Min(index, stage.Blocks.Count - 1));
    }

    // "Collision 1", "Collision 2", ... skipping names already taken
    private string UniqueName(string baseName)
    {
        for (int number = 1; ; number++)
        {
            string name = $"{baseName} {number}";
            if (!stage.Blocks.Exists(block => block.Name == name)) return name;
        }
    }

    private void RefreshCollisionList(int select)
    {
        collisionSelect.Clear();
        foreach (var block in stage.Blocks)
        {
            collisionSelect.AddItem(block.Name);
        }
        if (stage.Blocks.Count > 0) collisionSelect.Select(Math.Max(select, 0));
        ShowSelected();
    }

    // Fills the fields from the selected block without firing their change signals
    private void ShowSelected()
    {
        StageBlock block = Selected;
        bool any = block != null;
        foreach (Control field in new Control[] { collisionSelect, removeButton, nameInput, positionX, positionY, sizeX, sizeY, platformCheck })
        {
            SetEnabled(field, any);
        }
        if (any)
        {
            nameInput.Text = block.Name;
            positionX.SetValueNoSignal(ToDouble(block.Center.x));
            positionY.SetValueNoSignal(ToDouble(block.Center.y));
            sizeX.SetValueNoSignal(ToDouble(block.Size.x));
            sizeY.SetValueNoSignal(ToDouble(block.Size.y));
            platformCheck.SetPressedNoSignal(block.Platform);
        }
        RedrawBlocks();
    }

    private static void SetEnabled(Control field, bool enabled)
    {
        switch (field)
        {
            case BaseButton button: button.Disabled = !enabled; break;
            case LineEdit line: line.Editable = enabled; break;
            case SpinBox spin: spin.Editable = enabled; break;
        }
    }

    private void Edited()
    {
        SetUnsaved(true);
        RedrawBlocks();
    }

    private void SetUnsaved(bool value)
    {
        unsaved = value;
        unsavedLabel.Text = unsaved ? "Unsaved" : "";
    }

    private void RedrawBlocks()
    {
        foreach (Node child in blockMeshes.GetChildren())
        {
            blockMeshes.RemoveChild(child);
            child.QueueFree();
        }
        StageBlock selected = Selected;
        foreach (var block in stage.Blocks)
        {
            Color color = block.Platform ? PlatformColor : SolidColor;
            var material = new StandardMaterial3D
            {
                AlbedoColor = block == selected ? color.Lerp(SelectedTint, 0.6f) with { A = color.A } : color,
                Transparency = color.A < 1 ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
            };
            blockMeshes.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = block.Size.ToVector3(BlockDepth), Material = material },
                Position = block.Center.ToVector3(0),
            });
        }
    }

    private void BuildPanel()
    {
        var layer = new CanvasLayer();
        AddChild(layer);
        var panel = new PanelContainer
        {
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -296,
            OffsetTop = 16,
            OffsetRight = -16,
            GrowHorizontal = Control.GrowDirection.Begin,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.7f),
            ContentMarginLeft = 12, ContentMarginTop = 12, ContentMarginRight = 12, ContentMarginBottom = 12,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        });
        layer.AddChild(panel);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        panel.AddChild(layout);

        layout.AddChild(new Label { Text = "Stage" });
        stageSelect = new OptionButton { FocusMode = Control.FocusModeEnum.None };
        layout.AddChild(stageSelect);

        layout.AddChild(new Label { Text = "Collision" });
        var collisionRow = new HBoxContainer();
        collisionSelect = new OptionButton { FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
        collisionSelect.ItemSelected += _ => ShowSelected();
        var addButton = new Button { Text = "+", TooltipText = "Add a collision", FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(32, 0) };
        addButton.Pressed += AddBlock;
        removeButton = new Button { Text = "-", TooltipText = "Remove this collision", FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(32, 0) };
        removeButton.Pressed += RemoveBlock;
        collisionRow.AddChild(collisionSelect);
        collisionRow.AddChild(addButton);
        collisionRow.AddChild(removeButton);
        layout.AddChild(collisionRow);

        layout.AddChild(new HSeparator());

        nameInput = new LineEdit { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameInput.TextChanged += text =>
        {
            if (Selected == null) return;
            Selected.Name = text;
            collisionSelect.SetItemText(collisionSelect.Selected, text);
            SetUnsaved(true);
        };
        nameInput.TextSubmitted += _ => nameInput.ReleaseFocus();
        layout.AddChild(Row("Name", nameInput));

        positionX = NumberInput(-1000, (block, value) => block.Center = new DMVector(value, block.Center.y));
        positionY = NumberInput(-1000, (block, value) => block.Center = new DMVector(block.Center.x, value));
        layout.AddChild(Row("Position", positionX, positionY));
        // Never zero, so a block can't vanish
        sizeX = NumberInput(0.1, (block, value) => block.Size = new DMVector(value, block.Size.y));
        sizeY = NumberInput(0.1, (block, value) => block.Size = new DMVector(block.Size.x, value));
        layout.AddChild(Row("Size", sizeX, sizeY));

        platformCheck = new CheckBox { Text = "Droppable platform", FocusMode = Control.FocusModeEnum.None };
        platformCheck.Toggled += pressed =>
        {
            if (Selected == null) return;
            Selected.Platform = pressed;
            Edited();
        };
        layout.AddChild(platformCheck);

        layout.AddChild(new HSeparator());

        var fileRow = new HBoxContainer();
        var reloadButton = new Button { Text = "Reload", TooltipText = "Drop unsaved edits", FocusMode = Control.FocusModeEnum.None };
        reloadButton.Pressed += LoadStage;
        var saveButton = new Button { Text = "Save", FocusMode = Control.FocusModeEnum.None };
        saveButton.Pressed += Save;
        unsavedLabel = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Right };
        fileRow.AddChild(reloadButton);
        fileRow.AddChild(saveButton);
        fileRow.AddChild(unsavedLabel);
        layout.AddChild(fileRow);
    }

    private static HBoxContainer Row(string label, params Control[] fields)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(70, 0) });
        foreach (var field in fields)
        {
            field.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(field);
        }
        return row;
    }

    private SpinBox NumberInput(double min, Action<StageBlock, DM64> apply)
    {
        var input = new SpinBox
        {
            // Fine, so typed values aren't snapped. The arrows step by half a unit
            Step = 0.01,
            MinValue = min,
            MaxValue = 1000,
            AllowGreater = true,
            CustomArrowStep = 0.5,
        };
        input.ValueChanged += value =>
        {
            if (Selected == null) return;
            apply(Selected, ToDM64(value));
            Edited();
        };
        input.GetLineEdit().TextSubmitted += _ => input.GetLineEdit().ReleaseFocus();
        return input;
    }

    // Through decimal, like the file converter, so a typed 0.5 is the exact DM64 the file would read back
    private static DM64 ToDM64(double value) => DM64.FromRaw((long)Math.Round((decimal)value * DMScale, MidpointRounding.AwayFromZero));

    private static double ToDouble(DM64 value) => (double)Math.Round(value.RawValue / DMScale, 4);
}

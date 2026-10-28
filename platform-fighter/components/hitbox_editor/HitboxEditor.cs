using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Connects the hitbox editor scene's controls to the HitboxEditorManager.
// Controls raise edits, which go to the manager, and its Changed event redraws everything.
// Selection, the current sprite and playback live here, since the manager only holds file data.
// Two modes: editing phase ranges, where the timeline covers the whole sheet and its markers move phase boundaries,
// and editing a phase, where the timeline is locked to the phase's sprites and boxes are edited sprite by sprite.
// Controls are found by their unique names, and signals are connected in _Ready
public partial class HitboxEditor : Node
{
    private const int NoBox = -1;
    // DM64's fractional bits, for converting typed text without passing through a float, like CharacterFileJson
    private const decimal DMScale = 4294967296m;
    private static readonly Color WarningColor = new(1f, 0.55f, 0.35f);
    private static readonly Color FoundColor = new(0.55f, 0.9f, 0.55f);

    [Export] public int PlaybackFramesPerSecond = 60;
    // Write the character file after every edit
    [Export] public bool AutoSave = true;

    private static readonly CharacterId[] Characters = Enum.GetValues<CharacterId>();
    // In property dropdown order. Collision follows them
    private static readonly BoxType[] BoxTypes = { BoxType.Pushbox, BoxType.Hitbox, BoxType.Hurtbox };
    private static readonly int CollisionIndex = BoxTypes.Length;

    // A state with no data yet can still be selected, and is set up by setting its grid
    public string SelectedState { get; private set; } = HitboxEditorManager.AllStateNames[0];
    public int SelectedPhase { get; private set; }
    // The timeline covers the whole sheet and moves phase boundaries, instead of editing the selected phase
    public bool EditingRanges { get; private set; }
    // Only boxes of this type are listed, and new boxes are made with it
    public BoxType SelectedType { get; private set; } = BoxType.Hitbox;
    // Editing the character's body and sprite offset, which aren't per state. SelectedType is kept for switching back
    public bool EditingCollision { get; private set; }
    // Kept while stepping to sprites without the box, so stepping back to one with it selects it again
    public int SelectedBoxId { get; private set; } = NoBox;
    // Sprite of the sheet being edited. Always within the selected phase unless editing ranges
    public int CurrentSprite { get; private set; }
    public bool Playing { get; private set; }

    private HitboxEditorManager manager;
    private HitboxPreview preview;
    // Playback runs every phase once in order, then returns to CurrentSprite
    private double playbackTime;
    private int playbackPhase;
    private int playbackTick;
    // The text each field was last shown, so leaving a field that wasn't changed isn't an edit
    private readonly Dictionary<LineEdit, string> shownText = new();
    // What each On End dropdown item sets, in item order
    private readonly List<(PhaseEnd OnEnd, int Target)> onEndOptions = new();

    // File
    private Label unsavedLabel;
    private Button reloadButton, saveButton;
    // State
    private OptionButton characterDropdown, stateDropdown;
    private Label animationLabel, gridSummaryLabel;
    private Button setGridButton;
    private SpriteSheetDialog sheetDialog;
    // Shown when the sheet's size no longer matches the grid
    private Control sheetChangedRow, sheetChangedIndexRow;
    private Label sheetChangedLabel;
    private LineEdit sheetChangedIndexInput;
    private Button applySheetChangeButton, ignoreSheetChangeButton;
    // Attacks only, so the row is hidden for states
    private Control cancelOnLandRow;
    private CheckBox cancelOnLandToggle;
    // Phases
    private OptionButton phaseDropdown;
    private Button addPhaseButton, removePhaseButton;
    private CheckButton editRangesToggle;
    private Control phaseProperties;
    private LineEdit phaseNameInput, phaseLengthInput;
    private Label phaseLengthWarning;
    private OptionButton onEndDropdown;
    // Property and box
    private Control propertySection;
    private OptionButton propertyDropdown, boxDropdown;
    private Control boxRow;
    private Button addBoxButton, removeBoxButton;
    // Shared box properties
    private Control sharedProperties;
    private LineEdit nameInput, widthInput, heightInput;
    // One section per box type, only the selected type's shown
    private readonly Dictionary<BoxType, Control> typeSections = new();
    // Hitbox properties
    private OptionButton hitGroupDropdown;
    private Button addHitGroupButton, removeHitGroupButton;
    private LineEdit damageInput, hitstunInput;
    private KnockbackPicker knockbackPicker;
    // Collision properties
    private Control collisionProperties;
    private LineEdit bodyWidthInput, bodyHeightInput, spriteOffsetXInput, spriteOffsetYInput;
    // Timeline
    private PhaseTimeline phaseTimeline;
    private HSlider frameSlider;
    private Label frameLabel;
    private Button playButton, stopButton, previousFrameButton, nextFrameButton;
    // Sprite properties
    private Label frameHeader;
    private Control frameProperties;
    private LineEdit positionXInput, positionYInput;
    private Button copyToNextButton;

    public override void _Ready()
    {
        FindControls();
        ConnectControls();

        sheetDialog = new SpriteSheetDialog { Name = "SpriteSheetDialog" };
        AddChild(sheetDialog);
        sheetDialog.Submitted += OnGridSubmitted;

        var characterRoot = new Node3D { Name = "CharacterRoot" };
        GetNode<SubViewport>("%SubViewport").AddChild(characterRoot);
        preview = new HitboxPreview(characterRoot);

        manager = new HitboxEditorManager();
        manager.Changed += OnManagerChanged;
        manager.LoadCharacter(Characters[0]);
    }

    public override void _ExitTree()
    {
        preview?.Dispose();
    }

    // Plays every phase once in order at its real speed, ignoring where each phase goes on ending
    public override void _Process(double delta)
    {
        if (!Playing) return;
        if (!HasData)
        {
            StopPlayback();
            return;
        }
        playbackTime += delta;
        double tickTime = 1.0 / PlaybackFramesPerSecond;
        if (playbackTime < tickTime) return;
        List<PhaseFile> phases = manager.GetState(SelectedState).Phases;
        while (playbackTime >= tickTime)
        {
            playbackTime -= tickTime;
            playbackTick++;
            if (playbackTick < phases[playbackPhase].Length) continue;
            playbackTick = 0;
            playbackPhase++;
            if (playbackPhase >= phases.Count)
            {
                StopPlayback();
                return;
            }
        }
        Refresh();
    }

    // ---------- Setup ----------

    private void FindControls()
    {
        unsavedLabel = GetNode<Label>("%UnsavedLabel");
        reloadButton = GetNode<Button>("%ReloadButton");
        saveButton = GetNode<Button>("%SaveButton");

        characterDropdown = GetNode<OptionButton>("%CharacterDropdown");
        stateDropdown = GetNode<OptionButton>("%StateDropdown");
        animationLabel = GetNode<Label>("%AnimationLabel");
        gridSummaryLabel = GetNode<Label>("%GridSummaryLabel");
        setGridButton = GetNode<Button>("%SetGridButton");
        sheetChangedRow = GetNode<Control>("%SheetChangedRow");
        sheetChangedLabel = GetNode<Label>("%SheetChangedLabel");
        sheetChangedIndexRow = GetNode<Control>("%SheetChangedIndexRow");
        sheetChangedIndexInput = GetNode<LineEdit>("%SheetChangedIndexInput");
        applySheetChangeButton = GetNode<Button>("%ApplySheetChangeButton");
        ignoreSheetChangeButton = GetNode<Button>("%IgnoreSheetChangeButton");
        cancelOnLandRow = GetNode<Control>("%CancelOnLandRow");
        cancelOnLandToggle = GetNode<CheckBox>("%CancelOnLandToggle");

        phaseDropdown = GetNode<OptionButton>("%PhaseDropdown");
        addPhaseButton = GetNode<Button>("%AddPhaseButton");
        removePhaseButton = GetNode<Button>("%RemovePhaseButton");
        editRangesToggle = GetNode<CheckButton>("%EditRangesToggle");
        phaseProperties = GetNode<Control>("%PhaseProperties");
        phaseNameInput = GetNode<LineEdit>("%PhaseNameInput");
        onEndDropdown = GetNode<OptionButton>("%OnEndDropdown");
        phaseLengthInput = GetNode<LineEdit>("%PhaseLengthInput");
        phaseLengthWarning = GetNode<Label>("%PhaseLengthWarning");

        propertySection = GetNode<Control>("%PropertySection");
        propertyDropdown = GetNode<OptionButton>("%PropertyDropdown");
        boxRow = GetNode<Control>("%BoxRow");
        boxDropdown = GetNode<OptionButton>("%BoxDropdown");
        addBoxButton = GetNode<Button>("%AddBoxButton");
        removeBoxButton = GetNode<Button>("%RemoveBoxButton");

        sharedProperties = GetNode<Control>("%SharedProperties");
        nameInput = GetNode<LineEdit>("%NameInput");
        widthInput = GetNode<LineEdit>("%WidthInput");
        heightInput = GetNode<LineEdit>("%HeightInput");

        typeSections[BoxType.Pushbox] = GetNode<Control>("%PushboxProperties");
        typeSections[BoxType.Hitbox] = GetNode<Control>("%HitboxProperties");
        typeSections[BoxType.Hurtbox] = GetNode<Control>("%HurtboxProperties");
        hitGroupDropdown = GetNode<OptionButton>("%HitGroupDropdown");
        addHitGroupButton = GetNode<Button>("%AddHitGroupButton");
        removeHitGroupButton = GetNode<Button>("%RemoveHitGroupButton");
        damageInput = GetNode<LineEdit>("%DamageInput");
        hitstunInput = GetNode<LineEdit>("%HitstunInput");
        knockbackPicker = GetNode<KnockbackPicker>("%KnockbackPicker");

        collisionProperties = GetNode<Control>("%CollisionProperties");
        bodyWidthInput = GetNode<LineEdit>("%BodyWidthInput");
        bodyHeightInput = GetNode<LineEdit>("%BodyHeightInput");
        spriteOffsetXInput = GetNode<LineEdit>("%SpriteOffsetXInput");
        spriteOffsetYInput = GetNode<LineEdit>("%SpriteOffsetYInput");

        phaseTimeline = GetNode<PhaseTimeline>("%PhaseTimeline");
        frameSlider = GetNode<HSlider>("%FrameSlider");
        frameLabel = GetNode<Label>("%FrameLabel");
        playButton = GetNode<Button>("%PlayButton");
        stopButton = GetNode<Button>("%StopButton");
        previousFrameButton = GetNode<Button>("%PreviousFrameButton");
        nextFrameButton = GetNode<Button>("%NextFrameButton");

        frameHeader = GetNode<Label>("%FrameHeader");
        frameProperties = GetNode<Control>("%FrameProperties");
        positionXInput = GetNode<LineEdit>("%PositionXInput");
        positionYInput = GetNode<LineEdit>("%PositionYInput");
        copyToNextButton = GetNode<Button>("%CopyToNextButton");
    }

    private void ConnectControls()
    {
        reloadButton.Pressed += () => manager.LoadCharacter(manager.CharacterId);
        saveButton.Pressed += () => manager.Save();

        characterDropdown.ItemSelected += index => OnCharacterSelected((int)index);
        stateDropdown.ItemSelected += index => OnStateSelected((int)index);
        setGridButton.Pressed += OnSetGridClicked;
        applySheetChangeButton.Pressed += OnApplySheetChangeClicked;
        ignoreSheetChangeButton.Pressed += OnIgnoreSheetChangeClicked;
        cancelOnLandToggle.Toggled += on => manager.SetAttackCancelOnLand(SelectedState, on);

        phaseDropdown.ItemSelected += index => SelectPhase((int)index);
        addPhaseButton.Pressed += OnAddPhaseClicked;
        removePhaseButton.Pressed += OnRemovePhaseClicked;
        editRangesToggle.Toggled += OnEditRangesToggled;
        OnEndEdit(phaseNameInput, OnPhaseNameEdited);
        onEndDropdown.ItemSelected += index => OnOnEndSelected((int)index);
        OnEndEdit(phaseLengthInput, OnPhaseLengthEdited);

        propertyDropdown.ItemSelected += index => OnPropertySelected((int)index);
        boxDropdown.ItemSelected += index => OnBoxSelected((int)index);
        addBoxButton.Pressed += OnAddBoxClicked;
        removeBoxButton.Pressed += OnRemoveBoxClicked;

        OnEndEdit(nameInput, OnBoxNameEdited);
        OnEndEdit(widthInput, text => OnBoxRectEdited(text, BoxField.Width));
        OnEndEdit(heightInput, text => OnBoxRectEdited(text, BoxField.Height));

        hitGroupDropdown.ItemSelected += index => OnHitGroupSelected((int)index);
        addHitGroupButton.Pressed += OnAddHitGroupClicked;
        removeHitGroupButton.Pressed += OnRemoveHitGroupClicked;
        OnEndEdit(damageInput, OnDamageEdited);
        OnEndEdit(hitstunInput, OnHitstunEdited);
        // The picker keeps its own X and Y fields in sync, so only its result is connected
        knockbackPicker.ValueChanged += OnKnockbackChanged;

        OnEndEdit(bodyWidthInput, text => OnBodySizeEdited(text, true));
        OnEndEdit(bodyHeightInput, text => OnBodySizeEdited(text, false));
        OnEndEdit(spriteOffsetXInput, text => OnSpriteOffsetEdited(text, true));
        OnEndEdit(spriteOffsetYInput, text => OnSpriteOffsetEdited(text, false));

        phaseTimeline.BoundaryMoved += OnBoundaryMoved;
        phaseTimeline.SpriteClicked += OnTimelineSpriteClicked;
        phaseTimeline.DragPreviewed += OnBoundaryDragPreviewed;
        frameSlider.Step = 1;
        frameSlider.ValueChanged += value => OnSliderChanged((int)Math.Round(value));
        playButton.Pressed += OnPlayClicked;
        stopButton.Pressed += StopPlayback;
        previousFrameButton.Pressed += () => StepSprite(-1);
        nextFrameButton.Pressed += () => StepSprite(1);

        OnEndEdit(positionXInput, text => OnBoxRectEdited(text, BoxField.X));
        OnEndEdit(positionYInput, text => OnBoxRectEdited(text, BoxField.Y));
        copyToNextButton.Pressed += OnCopyToNextClicked;
    }

    // A field's edit is applied on enter or when it loses focus, so a half typed value is never applied
    private void OnEndEdit(LineEdit field, Action<string> handler)
    {
        field.TextSubmitted += _ => EndEdit(field, handler);
        field.FocusExited += () => EndEdit(field, handler);
    }

    private void EndEdit(LineEdit field, Action<string> handler)
    {
        if (shownText.TryGetValue(field, out string shown) && shown == field.Text) return;
        handler(field.Text);
    }

    // ---------- Manager ----------

    // Saving raises Changed again with nothing unsaved, and that second call does the refresh
    private void OnManagerChanged()
    {
        if (AutoSave && manager.HasUnsavedChanges)
        {
            manager.Save();
            return;
        }
        preview.SetCharacter(manager.CharacterId, manager.Data);
        ValidateSelection();
        Refresh();
    }

    // ---------- State ----------

    // Unsaved edits to the previous character are dropped, the same as a reload
    private void OnCharacterSelected(int index)
    {
        Playing = false;
        SelectedPhase = 0;
        SelectedBoxId = NoBox;
        CurrentSprite = 0;
        manager.LoadCharacter(Characters[index]);
    }

    private void OnStateSelected(int index)
    {
        Playing = false;
        SelectedState = manager.StateNames[index];
        SelectedPhase = 0;
        SelectedBoxId = NoBox;
        CurrentSprite = 0;
        ValidateSelection();
        Refresh();
    }

    // A state with no data starts from its sheet cut into square sprites, as the view guesses
    private void OnSetGridClicked()
    {
        SpriteFile sprite = HasData ? manager.GetState(SelectedState).Sprite : null;
        string animation = CharacterFiles.AnimationName(SelectedState);
        sheetDialog.Open(
            CharacterFiles.ResourceFolder(manager.CharacterId),
            animation,
            sprite?.Columns ?? Math.Max(1, preview.FrameCount(animation)),
            sprite?.Rows ?? 1);
    }

    // Sets the state up if it has no data yet
    private void OnGridSubmitted(int columns, int rows, int sheetWidth, int sheetHeight)
    {
        manager.AddState(SelectedState, columns, rows, sheetWidth, sheetHeight);
    }

    // Moves the data with the sprites added or removed. Bad text puts the old value back
    private void OnApplySheetChangeClicked()
    {
        if (!TryGetSheetChange(out SheetChange change)) return;
        if (change.Delta == 0)
        {
            manager.SetGrid(SelectedState, change.Columns, change.Rows, change.Width, change.Height);
            return;
        }
        if (!int.TryParse(sheetChangedIndexInput.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
        {
            Refresh();
            return;
        }
        manager.ShiftSprites(SelectedState, index, change.Delta, change.Columns, change.Rows, change.Width, change.Height);
    }

    private void OnIgnoreSheetChangeClicked()
    {
        if (!TryGetSheetChange(out SheetChange change)) return;
        manager.AcceptSheetSize(SelectedState, change.Width, change.Height);
    }

    // ---------- Phases ----------

    // Moves the timeline to the phase's first sprite
    private void SelectPhase(int index)
    {
        if (!HasData) return;
        Playing = false;
        SelectedPhase = Math.Clamp(index, 0, manager.GetState(SelectedState).Phases.Count - 1);
        CurrentSprite = manager.GetPhase(SelectedState, SelectedPhase).Start;
        ValidateSelection();
        Refresh();
    }

    // Splits the selected phase and selects its back half
    private void OnAddPhaseClicked()
    {
        if (!HasData || !manager.CanSplitPhase(SelectedState, SelectedPhase)) return;
        int added = manager.SplitPhase(SelectedState, SelectedPhase);
        SelectPhase(added);
    }

    private void OnRemovePhaseClicked()
    {
        if (!HasData) return;
        int removed = SelectedPhase;
        SelectedPhase = Math.Max(0, removed - 1);
        manager.RemovePhase(SelectedState, removed);
    }

    private void OnEditRangesToggled(bool on)
    {
        Playing = false;
        EditingRanges = on;
        ValidateSelection();
        Refresh();
    }

    private void OnPhaseNameEdited(string text)
    {
        if (!HasData) return;
        manager.RenamePhase(SelectedState, SelectedPhase, text);
    }

    private void OnOnEndSelected(int index)
    {
        if (!HasData || index < 0 || index >= onEndOptions.Count) return;
        (PhaseEnd onEnd, int target) = onEndOptions[index];
        manager.SetPhaseEnd(SelectedState, SelectedPhase, onEnd, target);
    }

    // Never shorter than its sprites, so each shows for at least a tick. Bad text puts the old value back
    private void OnPhaseLengthEdited(string text)
    {
        if (!HasData || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int length))
        {
            Refresh();
            return;
        }
        manager.SetPhaseLength(SelectedState, SelectedPhase, length);
    }

    private void OnBoundaryMoved(int phaseIndex, int lastSprite)
    {
        if (!HasData) return;
        manager.SetPhaseBoundary(SelectedState, phaseIndex, lastSprite);
    }

    // Shows the sprite the dragged marker is on, so it's clear what the phase will start or end on.
    // Nothing is edited until the drag is released, so the phase list still shows the old ranges
    private void OnBoundaryDragPreviewed(int sprite)
    {
        if (!HasData) return;
        Playing = false;
        CurrentSprite = Math.Clamp(sprite, 0, manager.SpriteCount(SelectedState) - 1);
        Refresh();
    }

    // Selects the phase playing the sprite too, so the phase list follows the timeline
    private void OnTimelineSpriteClicked(int sprite)
    {
        if (!HasData) return;
        Playing = false;
        int phase = manager.PhaseOf(SelectedState, sprite);
        // Outside ranges mode the timeline only moves within the phase, so clicking another phase selects it
        if (phase != SelectedPhase) SelectedPhase = phase;
        CurrentSprite = sprite;
        ValidateSelection();
        Refresh();
    }

    // ---------- Property and box ----------

    // A new box type picks its first box, since the old selection was another type
    private void OnPropertySelected(int index)
    {
        EditingCollision = index == CollisionIndex;
        if (!EditingCollision && BoxTypes[index] != SelectedType)
        {
            SelectedType = BoxTypes[index];
            SelectedBoxId = NoBox;
        }
        ValidateSelection();
        Refresh();
    }

    // The dropdown only lists boxes of the selected type on the current sprite
    private void OnBoxSelected(int index)
    {
        List<BoxFile> boxes = BoxesOfSelectedType();
        if (index < 0 || index >= boxes.Count) return;
        SelectedBoxId = boxes[index].Id;
        Refresh();
    }

    private void OnAddBoxClicked()
    {
        if (!HasData) return;
        BoxFile box = manager.AddBox(SelectedState, CurrentSprite, SelectedType);
        SelectedBoxId = box.Id;
        Refresh();
    }

    // Only this sprite's copy
    private void OnRemoveBoxClicked()
    {
        if (!TryGetBox(out _)) return;
        int boxId = SelectedBoxId;
        SelectedBoxId = NoBox;
        manager.RemoveBox(SelectedState, CurrentSprite, boxId);
    }

    // Copies the box onto the next sprite of the phase and moves there, so a box held over several sprites
    // is set up one click per sprite
    private void OnCopyToNextClicked()
    {
        if (!TryGetBox(out _) || CurrentSprite >= SpriteRangeEnd) return;
        int next = CurrentSprite + 1;
        manager.CopyBox(SelectedState, CurrentSprite, SelectedBoxId, next);
        CurrentSprite = next;
        Refresh();
    }

    private void OnBoxNameEdited(string text)
    {
        if (!TryGetBox(out _)) return;
        manager.RenameBox(SelectedState, SelectedBoxId, text);
    }

    private enum BoxField { X, Y, Width, Height }

    // This sprite's copy only. Bad text puts the old value back
    private void OnBoxRectEdited(string text, BoxField field)
    {
        if (!TryGetBox(out BoxFile box) || !TryParse(text, out DM64 value))
        {
            Refresh();
            return;
        }
        DMVector offset = box.Offset;
        DMVector size = box.Size;
        switch (field)
        {
            case BoxField.X: offset = new DMVector(value, offset.y); break;
            case BoxField.Y: offset = new DMVector(offset.x, value); break;
            case BoxField.Width: size = new DMVector(value, size.y); break;
            case BoxField.Height: size = new DMVector(size.x, value); break;
        }
        manager.SetBoxRect(SelectedState, CurrentSprite, SelectedBoxId, offset, size);
    }

    // ---------- Hitbox properties ----------

    // Every hitbox is in a group, so there's no "None". Moves every copy of the box
    private void OnHitGroupSelected(int index)
    {
        if (!TryGetHitbox(out _)) return;
        List<HitGroupFile> groups = manager.GetState(SelectedState).HitGroups;
        if (index < 0 || index >= groups.Count) return;
        manager.SetBoxHitGroup(SelectedState, SelectedBoxId, groups[index].Id);
    }

    // Creates a group and puts the selected box in it
    private void OnAddHitGroupClicked()
    {
        if (!TryGetHitbox(out _)) return;
        HitGroupFile group = manager.AddHitGroup(SelectedState);
        manager.SetBoxHitGroup(SelectedState, SelectedBoxId, group.Id);
    }

    // Removes the selected box's group. Its hitboxes move to the first group left
    private void OnRemoveHitGroupClicked()
    {
        if (!TryGetHitbox(out BoxFile box)) return;
        manager.RemoveHitGroup(SelectedState, box.HitGroup);
    }

    // What the hit deals belongs to each copy, so a hitbox can hit harder on its first sprite than its last
    private void OnDamageEdited(string text)
    {
        if (!TryGetHitbox(out BoxFile box) || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int damage))
        {
            Refresh();
            return;
        }
        manager.SetBoxHit(SelectedState, CurrentSprite, SelectedBoxId, damage, box.Hitstun, box.Knockback);
    }

    private void OnHitstunEdited(string text)
    {
        if (!TryGetHitbox(out BoxFile box) || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hitstun))
        {
            Refresh();
            return;
        }
        manager.SetBoxHit(SelectedState, CurrentSprite, SelectedBoxId, box.Damage, hitstun, box.Knockback);
    }

    private void OnKnockbackChanged(Vector2 knockback)
    {
        if (!TryGetHitbox(out BoxFile box))
        {
            Refresh();
            return;
        }
        manager.SetBoxHit(SelectedState, CurrentSprite, SelectedBoxId, box.Damage, box.Hitstun, new DMVector(ToDM64(knockback.X), ToDM64(knockback.Y)));
    }

    // ---------- Collision properties ----------

    // Bad text puts the old value back
    private void OnBodySizeEdited(string text, bool width)
    {
        if (!TryParse(text, out DM64 value))
        {
            Refresh();
            return;
        }
        DMVector size = manager.Data.Body.Size;
        manager.SetBodySize(width ? new DMVector(value, size.y) : new DMVector(size.x, value));
    }

    private void OnSpriteOffsetEdited(string text, bool x)
    {
        if (!TryParse(text, out DM64 value))
        {
            Refresh();
            return;
        }
        DMVector offset = manager.Data.Visuals.Offset;
        manager.SetVisualOffset(x ? new DMVector(value, offset.y) : new DMVector(offset.x, value));
    }

    // ---------- Timeline ----------

    // The slider covers the whole sheet when editing ranges, otherwise just the selected phase
    private void OnSliderChanged(int step)
    {
        Playing = false;
        CurrentSprite = Math.Clamp(SpriteRangeStart + step, SpriteRangeStart, SpriteRangeEnd);
        ValidateSelection();
        Refresh();
    }

    // Wraps within the slider's range
    private void StepSprite(int direction)
    {
        Playing = false;
        int count = SpriteRangeEnd - SpriteRangeStart + 1;
        int step = (CurrentSprite - SpriteRangeStart + direction + count) % count;
        CurrentSprite = SpriteRangeStart + step;
        ValidateSelection();
        Refresh();
    }

    private void OnPlayClicked()
    {
        if (!HasData) return;
        Playing = true;
        playbackTime = 0;
        playbackPhase = 0;
        playbackTick = 0;
        Refresh();
    }

    // Back to the sprite being edited, which playback never moves
    private void StopPlayback()
    {
        Playing = false;
        Refresh();
    }

    // ---------- Refresh ----------

    // Redraws every control and the preview from the manager's loaded file and the current selection.
    // Nothing here raises the controls' signals, so showing a value is never mistaken for an edit
    private void Refresh()
    {
        if (manager.Loaded == null) return;

        unsavedLabel.Visible = manager.HasUnsavedChanges;

        Fill(characterDropdown, Characters.Select(id => id.ToString()), Array.IndexOf(Characters, manager.CharacterId));
        string[] stateNames = manager.StateNames;
        Fill(stateDropdown, stateNames, Array.IndexOf(stateNames, SelectedState));

        StateFile state = HasData ? manager.GetState(SelectedState) : null;
        ShowStateProperties(state);
        ShowPhaseProperties(state);

        // Property and box. Hidden while editing ranges, and the box controls while editing collision
        propertySection.Visible = !EditingRanges;
        Fill(propertyDropdown, BoxTypes.Select(type => type.ToString()).Append("Collision"),
            EditingCollision ? CollisionIndex : Array.IndexOf(BoxTypes, SelectedType));
        bool editingBoxes = EditingBoxes;
        boxRow.Visible = editingBoxes;
        sharedProperties.Visible = editingBoxes;
        List<BoxFile> boxes = BoxesOfSelectedType();
        Fill(boxDropdown, boxes.Select(b => b.Name), boxes.FindIndex(b => b.Id == SelectedBoxId));
        boxDropdown.Disabled = boxes.Count == 0;
        addBoxButton.Disabled = !HasData;
        bool hasBox = TryGetBox(out BoxFile box);
        removeBoxButton.Disabled = !hasBox;

        ShowText(nameInput, hasBox ? box.Name : "", hasBox);
        ShowText(widthInput, hasBox ? Format(box.Size.x) : "", hasBox);
        ShowText(heightInput, hasBox ? Format(box.Size.y) : "", hasBox);
        foreach (var (type, section) in typeSections)
        {
            section.Visible = editingBoxes && type == SelectedType;
        }
        ShowHitboxProperties(hasBox ? box : null);
        collisionProperties.Visible = !EditingRanges && EditingCollision;
        ShowCollisionProperties();

        ShowTimeline(state);

        // Sprite properties
        frameProperties.Visible = editingBoxes;
        ShowText(positionXInput, hasBox ? Format(box.Offset.x) : "", hasBox);
        ShowText(positionYInput, hasBox ? Format(box.Offset.y) : "", hasBox);
        copyToNextButton.Disabled = !hasBox || CurrentSprite >= SpriteRangeEnd;

        preview.Show(manager.Definition, SelectedState, ShownSprite, hasBox ? SelectedBoxId : NoBox,
            EditingCollision && !EditingRanges ? manager.Data.Body.Size : null);
    }

    // state is null when the state has no data yet
    private void ShowStateProperties(StateFile state)
    {
        string path = SpriteCharacterView.SheetPath(CharacterFiles.ResourceFolder(manager.CharacterId), CharacterFiles.AnimationName(SelectedState));
        bool found = LoadSheet() != null;
        animationLabel.Text = found ? $"Found animation at {path}" : $"Did not find animation at {path}";
        animationLabel.Modulate = found ? FoundColor : WarningColor;

        SpriteFile sprite = state?.Sprite;
        gridSummaryLabel.Text = state == null
            ? "Not set up. Set the grid to start"
            : $"{sprite.Columns} x {sprite.Rows}, {sprite.Count} sprites";
        setGridButton.Disabled = !found;

        bool changed = TryGetSheetChange(out SheetChange change);
        sheetChangedRow.Visible = changed;
        if (changed)
        {
            string was = $"{sprite.SheetWidth} x {sprite.SheetHeight}";
            string now = $"{change.Width} x {change.Height}";
            sheetChangedLabel.Text = change.Delta > 0 ? $"The sheet grew from {was} to {now}: {change.Delta} sprites were added. Before which sprite?"
                : change.Delta < 0 ? $"The sheet shrank from {was} to {now}: {-change.Delta} sprites were removed. Starting at which sprite?"
                : $"The sheet changed from {was} to {now}, but has as many sprites. Recut it at the new size?";
            sheetChangedIndexRow.Visible = change.Delta != 0;
            // Defaults to the end, and left alone while being typed in
            if (!sheetChangedIndexInput.HasFocus())
            {
                ShowText(sheetChangedIndexInput, (change.Delta > 0 ? sprite.Count : Math.Max(0, sprite.Count + change.Delta)).ToString(CultureInfo.InvariantCulture), true);
            }
            applySheetChangeButton.Text = change.Delta == 0 ? "Recut" : "Move data";
        }

        AttackFile attack = manager.GetAttack(SelectedState);
        cancelOnLandRow.Visible = attack != null;
        cancelOnLandToggle.SetPressedNoSignal(attack != null && attack.CancelOnLand);
    }

    private void ShowPhaseProperties(StateFile state)
    {
        bool hasData = state != null;
        Fill(phaseDropdown, hasData ? state.Phases.Select((p, i) => $"{i}: {p.Name}  ({p.Start}-{p.End})") : Array.Empty<string>(), SelectedPhase);
        phaseDropdown.Disabled = !hasData;
        addPhaseButton.Disabled = !hasData || !manager.CanSplitPhase(SelectedState, SelectedPhase);
        removePhaseButton.Disabled = !hasData || state.Phases.Count < 2;
        editRangesToggle.SetPressedNoSignal(EditingRanges);
        editRangesToggle.Disabled = !hasData;

        phaseProperties.Visible = hasData && !EditingRanges;
        if (!hasData) return;
        PhaseFile phase = state.Phases[SelectedPhase];
        ShowText(phaseNameInput, phase.Name, true);
        ShowText(phaseLengthInput, phase.Length.ToString(CultureInfo.InvariantCulture), true);
        string warning = phase.Length % phase.SpriteCount == 0
            ? ""
            : $"{phase.Length} ticks don't divide evenly into {phase.SpriteCount} sprites, so some show a tick longer";
        phaseLengthWarning.Visible = warning != "";
        phaseLengthInput.TooltipText = warning;

        // Next, restart and first, then going to any other phase by name
        onEndOptions.Clear();
        var names = new List<string>();
        bool last = SelectedPhase == state.Phases.Count - 1;
        onEndOptions.Add((PhaseEnd.Next, PhaseFile.NoPhase));
        names.Add(last ? "End the state" : "Go to next phase");
        onEndOptions.Add((PhaseEnd.Restart, PhaseFile.NoPhase));
        names.Add("Restart phase");
        onEndOptions.Add((PhaseEnd.First, PhaseFile.NoPhase));
        names.Add("Go to first phase");
        for (int i = 0; i < state.Phases.Count; i++)
        {
            if (i == SelectedPhase) continue;
            onEndOptions.Add((PhaseEnd.Phase, state.Phases[i].Id));
            names.Add($"Go to {state.Phases[i].Name}");
        }
        int selected = onEndOptions.FindIndex(option => option.OnEnd == phase.OnEnd
            && (phase.OnEnd != PhaseEnd.Phase || option.Target == phase.Target));
        Fill(onEndDropdown, names, selected);
    }

    private void ShowTimeline(StateFile state)
    {
        bool hasData = state != null;
        int count = hasData ? state.Sprite?.Count ?? 1 : 1;
        IEnumerable<(int, int, string)> ranges = hasData
            ? state.Phases.Select(p => (p.Start, p.End, p.Name))
            : Array.Empty<(int, int, string)>();
        phaseTimeline.Show(count, ranges, SelectedPhase, ShownSprite, hasData && EditingRanges && !Playing);

        int start = SpriteRangeStart;
        int end = SpriteRangeEnd;
        frameSlider.MaxValue = Math.Max(0, end - start);
        frameSlider.SetValueNoSignal(CurrentSprite - start);
        frameSlider.Editable = hasData;
        frameLabel.Text = $"{CurrentSprite - start} / {end - start}";
        playButton.Disabled = Playing || !hasData;
        stopButton.Disabled = !Playing;

        if (!hasData)
        {
            frameHeader.Text = "SPRITE 0";
            return;
        }
        int sprite = ShownSprite;
        int phaseIndex = Playing ? playbackPhase : manager.PhaseOf(SelectedState, sprite);
        PhaseFile phase = state.Phases[phaseIndex];
        int entry = sprite - phase.Start;
        int first = SpriteSequenceMath.EntryStart(entry, phase.SpriteCount, phase.Length);
        int last = SpriteSequenceMath.EntryStart(entry + 1, phase.SpriteCount, phase.Length) - 1;
        frameHeader.Text = Playing
            ? $"PLAYING  {phase.Name}  tick {playbackTick} / {phase.Length - 1}"
            : $"SPRITE {sprite}  ({phase.Name}, ticks {first}-{last} of {phase.Length})";
    }

    // box is null when no box is selected
    private void ShowHitboxProperties(BoxFile box)
    {
        bool hasBox = box != null;
        var hitGroupNames = new List<string>();
        int hitGroupIndex = -1;
        if (HasData)
        {
            List<HitGroupFile> groups = manager.GetState(SelectedState).HitGroups;
            for (int i = 0; i < groups.Count; i++)
            {
                hitGroupNames.Add(groups[i].Name);
                if (hasBox && groups[i].Id == box.HitGroup) hitGroupIndex = i;
            }
        }
        Fill(hitGroupDropdown, hitGroupNames, hitGroupIndex);
        hitGroupDropdown.Disabled = !hasBox;
        addHitGroupButton.Disabled = !hasBox;
        removeHitGroupButton.Disabled = !hasBox || hitGroupIndex < 0 || !manager.CanRemoveHitGroup(SelectedState, box.HitGroup);

        ShowText(damageInput, hasBox ? box.Damage.ToString(CultureInfo.InvariantCulture) : "", hasBox);
        ShowText(hitstunInput, hasBox ? box.Hitstun.ToString(CultureInfo.InvariantCulture) : "", hasBox);
        knockbackPicker.SetValue(hasBox ? box.Knockback.ToStandardVector() : Vector2.Zero);
        knockbackPicker.MouseFilter = hasBox ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        knockbackPicker.XInput.Editable = hasBox;
        knockbackPicker.YInput.Editable = hasBox;
    }

    // The character's, not the state's, so always editable
    private void ShowCollisionProperties()
    {
        DMVector size = manager.Data.Body.Size;
        DMVector offset = manager.Data.Visuals.Offset;
        ShowText(bodyWidthInput, Format(size.x), true);
        ShowText(bodyHeightInput, Format(size.y), true);
        ShowText(spriteOffsetXInput, Format(offset.x), true);
        ShowText(spriteOffsetYInput, Format(offset.y), true);
    }

    private void ShowText(LineEdit field, string text, bool editable)
    {
        field.Text = text;
        field.Editable = editable;
        shownText[field] = text;
    }

    // Selecting by index never raises ItemSelected
    private static void Fill(OptionButton dropdown, IEnumerable<string> names, int selected)
    {
        dropdown.Clear();
        foreach (string name in names) dropdown.AddItem(name);
        dropdown.Select(selected >= 0 && selected < dropdown.ItemCount ? selected : -1);
    }

    // ---------- Helpers ----------

    private bool HasData => manager.Loaded != null && manager.HasState(SelectedState);

    private bool EditingBoxes => !EditingRanges && !EditingCollision;

    // First and last sprite the slider covers: the whole sheet when editing ranges, otherwise the selected phase
    private int SpriteRangeStart => HasData && !EditingRanges ? manager.GetPhase(SelectedState, SelectedPhase).Start : 0;
    private int SpriteRangeEnd => !HasData ? 0
        : EditingRanges ? manager.SpriteCount(SelectedState) - 1
        : manager.GetPhase(SelectedState, SelectedPhase).End;

    // The sprite on screen, which is playback's while playing
    private int ShownSprite
    {
        get
        {
            if (!Playing || !HasData) return CurrentSprite;
            PhaseFile phase = manager.GetPhase(SelectedState, playbackPhase);
            return phase.Start + SpriteSequenceMath.EntryAt(playbackTick, phase.SpriteCount, phase.Length);
        }
    }

    // On the current sprite, in file order, for the box dropdown
    private List<BoxFile> BoxesOfSelectedType()
    {
        if (!HasData) return new List<BoxFile>();
        return manager.GetFrame(SelectedState, CurrentSprite).Boxes.Where(box => box.Type == SelectedType).ToList();
    }

    // The selected box's copy on the current sprite. Never while editing ranges or collision
    private bool TryGetBox(out BoxFile box)
    {
        box = null;
        if (!EditingBoxes || SelectedBoxId == NoBox || !HasData) return false;
        box = manager.FindBox(SelectedState, CurrentSprite, SelectedBoxId);
        return box != null && box.Type == SelectedType;
    }

    private bool TryGetHitbox(out BoxFile box) => TryGetBox(out box) && box.Type == BoxType.Hitbox;

    // After a load or edit the selected state, phase or sprite may be gone, so each is pulled back into range.
    // A selected box missing from the sprite gives way to the type's first box there, if it has one
    private void ValidateSelection()
    {
        if (manager.Loaded == null) return;
        // An attack is only listed while the loaded character has it
        if (Array.IndexOf(manager.StateNames, SelectedState) < 0)
        {
            SelectedState = HitboxEditorManager.AllStateNames[0];
            SelectedBoxId = NoBox;
        }
        if (!HasData)
        {
            SelectedPhase = 0;
            CurrentSprite = 0;
            return;
        }

        int count = manager.SpriteCount(SelectedState);
        CurrentSprite = Math.Clamp(CurrentSprite, 0, count - 1);
        SelectedPhase = Math.Clamp(SelectedPhase, 0, manager.GetState(SelectedState).Phases.Count - 1);
        if (EditingRanges) SelectedPhase = manager.PhaseOf(SelectedState, CurrentSprite);
        else CurrentSprite = Math.Clamp(CurrentSprite, SpriteRangeStart, SpriteRangeEnd);
        if (Playing && playbackPhase >= manager.GetState(SelectedState).Phases.Count) Playing = false;

        if (EditingBoxes && !TryGetBox(out _))
        {
            List<BoxFile> boxes = BoxesOfSelectedType();
            if (boxes.Count > 0) SelectedBoxId = boxes[0].Id;
        }
    }

    // ---------- Sheet ----------

    // How the sheet's size moved away from the grid's. Sprites keep the size they were cut at,
    // so a sheet that grew by whole sprites reads as sprites added
    private readonly struct SheetChange
    {
        public readonly int Width, Height, Columns, Rows, Delta;

        public SheetChange(int width, int height, int columns, int rows, int delta)
        {
            Width = width;
            Height = height;
            Columns = columns;
            Rows = rows;
            Delta = delta;
        }
    }

    // False when the state has no data, no sheet, no remembered size, or the size still matches
    private bool TryGetSheetChange(out SheetChange change)
    {
        change = default;
        if (!HasData) return false;
        SpriteFile sprite = manager.GetState(SelectedState).Sprite;
        Texture2D sheet = LoadSheet();
        if (sprite == null || sheet == null || sprite.SheetWidth <= 0 || sprite.SheetHeight <= 0) return false;
        int width = sheet.GetWidth();
        int height = sheet.GetHeight();
        if (width == sprite.SheetWidth && height == sprite.SheetHeight) return false;

        int spriteWidth = Math.Max(1, sprite.SheetWidth / Math.Max(1, sprite.Columns));
        int spriteHeight = Math.Max(1, sprite.SheetHeight / Math.Max(1, sprite.Rows));
        int columns = Math.Max(1, width / spriteWidth);
        int rows = Math.Max(1, height / spriteHeight);
        // Same count means the sprites themselves changed size, so the grid is kept as it was
        int delta = columns * rows - sprite.Count;
        if (delta == 0)
        {
            columns = sprite.Columns;
            rows = sprite.Rows;
        }
        change = new SheetChange(width, height, columns, rows, delta);
        return true;
    }

    // Null when the state's sheet isn't where its name says. Loaded fresh, so a sheet changed outside shows up
    private Texture2D LoadSheet()
    {
        string path = SpriteCharacterView.SheetPath(CharacterFiles.ResourceFolder(manager.CharacterId), CharacterFiles.AnimationName(SelectedState));
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    // Through decimal, so typed text never passes through a float and 0.1 saves as exactly what was typed
    private static bool TryParse(string text, out DM64 value)
    {
        value = default;
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)) return false;
        try
        {
            value = DM64.FromRaw((long)Math.Round(number * DMScale, MidpointRounding.AwayFromZero));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    // Picked values come from the mouse, so a float is as exact as they get. Rounded through decimal like typed text
    private static DM64 ToDM64(float value) => DM64.FromRaw((long)Math.Round((decimal)value * DMScale, MidpointRounding.AwayFromZero));

    private static string Format(DM64 value) => Math.Round(value.RawValue / DMScale, 4).ToString("0.####", CultureInfo.InvariantCulture);
}

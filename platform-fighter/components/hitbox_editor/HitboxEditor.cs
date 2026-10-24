using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Connects the hitbox editor scene's controls to the HitboxEditorManager.
// Controls raise edits, which go to the manager, and its Changed event redraws everything.
// Selection, the selected property and the current frame live here, since the manager only holds file data.
// Controls are found by their unique names, and signals are connected in _Ready
public partial class HitboxEditor : Node
{
    private const int NoBox = -1;
    private const int NoEntry = -1;
    // DM64's fractional bits, for converting typed text without passing through a float, like CharacterFileJson
    private const decimal DMScale = 4294967296m;
    // Sequence entries that never show or aren't in the sheet
    private static readonly Color WarningColor = new(1f, 0.55f, 0.35f);

    [Export] public int PlaybackFramesPerSecond = 60;
    // Write the character file after every edit
    [Export] public bool AutoSave = true;
    // The preview camera's orthographic size, in world units shown top to bottom. Each wheel notch scales it by ZoomStep
    [Export] public float MinZoomSize = 0.5f;
    [Export] public float MaxZoomSize = 20f;
    [Export] public float ZoomStep = 1.15f;

    private static readonly CharacterId[] Characters = Enum.GetValues<CharacterId>();
    // In property dropdown order. The animation and collision follow them
    private static readonly BoxType[] BoxTypes = { BoxType.Pushbox, BoxType.Hitbox, BoxType.Hurtbox };
    private static readonly int AnimationIndex = BoxTypes.Length;
    private static readonly int CollisionIndex = BoxTypes.Length + 1;

    // A state with no data yet can still be selected, and is created on the first edit to it
    public string SelectedState { get; private set; } = HitboxEditorManager.AllStateNames[0];
    // Only boxes of this type are listed, and new boxes are made with it
    public BoxType SelectedType { get; private set; } = BoxType.Hitbox;
    // Editing the state's sprite sequence instead of its boxes. SelectedType is kept for switching back
    public bool EditingAnimation { get; private set; }
    // Editing the character's body and sprite offset, which aren't per state or keyed
    public bool EditingCollision { get; private set; }
    public bool EditingBoxes => !EditingAnimation && !EditingCollision;
    public int SelectedBoxId { get; private set; } = NoBox;
    // Index into the sprite sequence, or NoEntry. Follows the timeline, and picking one moves the timeline to it
    public int SelectedEntry { get; private set; } = NoEntry;
    // Always a tick. When stepping by sprite it is the first tick of the current sprite, except mid playback
    public int CurrentFrame { get; private set; }
    public bool Playing { get; private set; }

    private HitboxEditorManager manager;
    private HitboxPreview preview;
    private Camera3D previewCamera;
    private double playbackTime;
    // The text each field was last shown, so leaving a field that wasn't changed isn't an edit
    private readonly Dictionary<LineEdit, string> shownText = new();

    // File
    private Label unsavedLabel;
    private Button reloadButton, saveButton;
    // Pickers
    private OptionButton characterDropdown, stateDropdown, propertyDropdown, boxDropdown;
    private Control boxRow;
    private Button addBoxButton, removeBoxButton;
    // State properties
    private LineEdit lengthInput;
    private Label lengthWarning;
    private CheckBox loopToggle;
    // Attacks only, so the row is hidden for states
    private Control cancelOnLandRow;
    private CheckBox cancelOnLandToggle;
    // Shared box properties
    private Control sharedProperties;
    private LineEdit nameInput;
    private OptionButton parentDropdown;
    // One section per box type, only the selected type's shown
    private readonly Dictionary<BoxType, Control> typeSections = new();
    // Hitbox properties
    private OptionButton hitGroupDropdown;
    private Button addHitGroupButton, removeHitGroupButton;
    private LineEdit damageInput, hitstunInput;
    private KnockbackPicker knockbackPicker;
    // Animation properties
    private Control animationProperties;
    private Button setAnimButton, addEntryButton, removeEntryButton;
    private Label sheetSummaryLabel;
    private SequenceList sequenceList;
    private LineEdit entrySpriteInput;
    private SpriteSheetDialog sheetDialog;
    // Collision properties
    private Control collisionProperties;
    private LineEdit bodyWidthInput, bodyHeightInput, spriteOffsetXInput, spriteOffsetYInput;
    // Timeline
    private HSlider frameSlider;
    private Label frameLabel;
    private Button playButton, pauseButton, previousFrameButton, nextFrameButton;
    // Frame properties
    private Label frameHeader, keyStateLabel;
    private LineEdit positionXInput, positionYInput, widthInput, heightInput;
    private CheckBox activeToggle;
    private Button removeKeyButton;

    public override void _Ready()
    {
        FindControls();
        ConnectControls();

        sheetDialog = new SpriteSheetDialog { Name = "SpriteSheetDialog" };
        AddChild(sheetDialog);
        sheetDialog.Submitted += OnSheetSubmitted;

        var characterRoot = new Node3D { Name = "CharacterRoot" };
        var subViewport = GetNode<SubViewport>("%SubViewport");
        subViewport.AddChild(characterRoot);
        previewCamera = subViewport.GetNode<Camera3D>("Camera3D");
        subViewport.GetParent<Control>().GuiInput += OnPreviewInput;
        preview = new HitboxPreview(characterRoot);

        manager = new HitboxEditorManager();
        manager.Changed += OnManagerChanged;
        manager.LoadCharacter(Characters[0]);
    }

    public override void _ExitTree()
    {
        preview?.Dispose();
    }

    // Playback always loops, whether or not the state does
    public override void _Process(double delta)
    {
        if (!Playing) return;
        playbackTime += delta;
        double frameTime = 1.0 / PlaybackFramesPerSecond;
        if (playbackTime < frameTime) return;
        while (playbackTime >= frameTime)
        {
            playbackTime -= frameTime;
            CurrentFrame = (CurrentFrame + 1) % StateLength;
        }
        FollowFrame();
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
        propertyDropdown = GetNode<OptionButton>("%PropertyDropdown");
        boxRow = GetNode<Control>("%BoxRow");
        boxDropdown = GetNode<OptionButton>("%BoxDropdown");
        addBoxButton = GetNode<Button>("%AddBoxButton");
        removeBoxButton = GetNode<Button>("%RemoveBoxButton");

        lengthInput = GetNode<LineEdit>("%LengthInput");
        lengthWarning = GetNode<Label>("%LengthWarning");
        loopToggle = GetNode<CheckBox>("%LoopToggle");
        cancelOnLandRow = GetNode<Control>("%CancelOnLandRow");
        cancelOnLandToggle = GetNode<CheckBox>("%CancelOnLandToggle");

        sharedProperties = GetNode<Control>("%SharedProperties");
        nameInput = GetNode<LineEdit>("%NameInput");
        parentDropdown = GetNode<OptionButton>("%ParentDropdown");
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

        animationProperties = GetNode<Control>("%AnimationProperties");
        setAnimButton = GetNode<Button>("%SetAnimButton");
        sheetSummaryLabel = GetNode<Label>("%SheetSummaryLabel");
        sequenceList = GetNode<SequenceList>("%SequenceList");
        entrySpriteInput = GetNode<LineEdit>("%EntrySpriteInput");
        addEntryButton = GetNode<Button>("%AddEntryButton");
        removeEntryButton = GetNode<Button>("%RemoveEntryButton");

        collisionProperties = GetNode<Control>("%CollisionProperties");
        bodyWidthInput = GetNode<LineEdit>("%BodyWidthInput");
        bodyHeightInput = GetNode<LineEdit>("%BodyHeightInput");
        spriteOffsetXInput = GetNode<LineEdit>("%SpriteOffsetXInput");
        spriteOffsetYInput = GetNode<LineEdit>("%SpriteOffsetYInput");

        frameSlider = GetNode<HSlider>("%FrameSlider");
        frameLabel = GetNode<Label>("%FrameLabel");
        playButton = GetNode<Button>("%PlayButton");
        pauseButton = GetNode<Button>("%PauseButton");
        previousFrameButton = GetNode<Button>("%PreviousFrameButton");
        nextFrameButton = GetNode<Button>("%NextFrameButton");

        frameHeader = GetNode<Label>("%FrameHeader");
        positionXInput = GetNode<LineEdit>("%PositionXInput");
        positionYInput = GetNode<LineEdit>("%PositionYInput");
        activeToggle = GetNode<CheckBox>("%ActiveToggle");
        keyStateLabel = GetNode<Label>("%KeyStateLabel");
        removeKeyButton = GetNode<Button>("%RemoveKeyButton");
    }

    private void ConnectControls()
    {
        reloadButton.Pressed += () => manager.LoadCharacter(manager.CharacterId);
        saveButton.Pressed += () => manager.Save();

        characterDropdown.ItemSelected += index => OnCharacterSelected((int)index);
        stateDropdown.ItemSelected += index => OnStateSelected((int)index);
        propertyDropdown.ItemSelected += index => OnPropertySelected((int)index);
        boxDropdown.ItemSelected += index => OnBoxSelected((int)index);
        addBoxButton.Pressed += OnAddBoxClicked;
        removeBoxButton.Pressed += OnRemoveBoxClicked;

        OnEndEdit(lengthInput, OnLengthEdited);
        loopToggle.Toggled += OnLoopToggled;
        cancelOnLandToggle.Toggled += on => manager.SetAttackCancelOnLand(SelectedState, on);

        OnEndEdit(nameInput, OnBoxNameEdited);
        parentDropdown.ItemSelected += index => OnBoxParentSelected((int)index);
        OnEndEdit(widthInput, OnWidthEdited);
        OnEndEdit(heightInput, OnHeightEdited);

        hitGroupDropdown.ItemSelected += index => OnHitGroupSelected((int)index);
        addHitGroupButton.Pressed += OnAddHitGroupClicked;
        removeHitGroupButton.Pressed += OnRemoveHitGroupClicked;
        OnEndEdit(damageInput, OnDamageEdited);
        OnEndEdit(hitstunInput, OnHitstunEdited);
        // The picker keeps its own X and Y fields in sync, so only its result is connected
        knockbackPicker.ValueChanged += OnKnockbackChanged;

        setAnimButton.Pressed += OnSetAnimClicked;
        sequenceList.ItemSelected += index => OnEntrySelected((int)index);
        sequenceList.ItemMoved += OnEntryMoved;
        OnEndEdit(entrySpriteInput, OnEntrySpriteEdited);
        addEntryButton.Pressed += OnAddEntryClicked;
        removeEntryButton.Pressed += OnRemoveEntryClicked;

        OnEndEdit(bodyWidthInput, text => OnBodySizeEdited(text, true));
        OnEndEdit(bodyHeightInput, text => OnBodySizeEdited(text, false));
        OnEndEdit(spriteOffsetXInput, text => OnSpriteOffsetEdited(text, true));
        OnEndEdit(spriteOffsetYInput, text => OnSpriteOffsetEdited(text, false));

        frameSlider.Step = 1;
        frameSlider.ValueChanged += value => OnFrameChanged((int)Math.Round(value));
        playButton.Pressed += OnPlayClicked;
        pauseButton.Pressed += OnPauseClicked;
        previousFrameButton.Pressed += OnPreviousFrameClicked;
        nextFrameButton.Pressed += OnNextFrameClicked;

        OnEndEdit(positionXInput, OnPositionXEdited);
        OnEndEdit(positionYInput, OnPositionYEdited);
        activeToggle.Toggled += OnActiveToggled;
        removeKeyButton.Pressed += OnRemoveKeyClicked;

        ConnectScrubbers();
    }

    // Number fields whose labels can be dragged. Each uses the same handler as typing into it
    private void ConnectScrubbers()
    {
        Scrub(lengthInput, OnLengthEdited, 1m);
        Scrub(widthInput, OnWidthEdited, 1m);
        Scrub(heightInput, OnHeightEdited, 1m);
        Scrub(damageInput, OnDamageEdited, 1m);
        Scrub(hitstunInput, OnHitstunEdited, 1m);
        // The body is in world units, which are much bigger than the pixels everything else is in
        Scrub(bodyWidthInput, text => OnBodySizeEdited(text, true), 0.1m);
        Scrub(bodyHeightInput, text => OnBodySizeEdited(text, false), 0.1m);
        Scrub(spriteOffsetXInput, text => OnSpriteOffsetEdited(text, true), 1m);
        Scrub(spriteOffsetYInput, text => OnSpriteOffsetEdited(text, false), 1m);
        Scrub(positionXInput, OnPositionXEdited, 1m);
        Scrub(positionYInput, OnPositionYEdited, 1m);
    }

    // The label is the one just before the field in its row, like "X:" or the row's own label
    private static void Scrub(LineEdit field, Action<string> handler, decimal step)
    {
        if (field.GetParent().GetChild(field.GetIndex() - 1) is not Label label)
        {
            GD.PushWarning($"{field.Name} has no label before it to scrub with");
            return;
        }
        new FieldScrubber(label, field, handler, step);
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

    // ---------- Preview ----------

    // The wheel, a trackpad's two finger scroll or a pinch zooms the camera about the character,
    // which sits at the middle of the view. Trackpads send gestures rather than wheel clicks
    private void OnPreviewInput(InputEvent input)
    {
        float scale = input switch
        {
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } => 1f / ZoomStep,
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } => ZoomStep,
            // Scrolling down, which is a positive delta, zooms out like the wheel
            InputEventPanGesture pan => Mathf.Pow(ZoomStep, pan.Delta.Y),
            // Spreading the fingers is a factor above 1, which zooms in
            InputEventMagnifyGesture magnify when magnify.Factor > 0f => 1f / magnify.Factor,
            _ => 1f,
        };
        if (scale == 1f) return;
        previewCamera.Size = Mathf.Clamp(previewCamera.Size * scale, MinZoomSize, MaxZoomSize);
        GetViewport().SetInputAsHandled();
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

    // ---------- Pickers ----------

    // Unsaved edits to the previous character are dropped, the same as a reload
    private void OnCharacterSelected(int index)
    {
        SelectedBoxId = NoBox;
        SelectedEntry = NoEntry;
        CurrentFrame = 0;
        manager.LoadCharacter(Characters[index]);
    }

    private void OnStateSelected(int index)
    {
        SelectedState = manager.StateNames[index];
        SelectedBoxId = NoBox;
        SelectedEntry = NoEntry;
        CurrentFrame = 0;
        ValidateSelection();
        Refresh();
    }

    // A new box type picks its first box, since the old selection was another type
    private void OnPropertySelected(int index)
    {
        EditingAnimation = index == AnimationIndex;
        EditingCollision = index == CollisionIndex;
        if (EditingBoxes && BoxTypes[index] != SelectedType)
        {
            SelectedType = BoxTypes[index];
            SelectedBoxId = NoBox;
        }
        ValidateSelection();
        Refresh();
    }

    // The dropdown only lists boxes of the selected type
    private void OnBoxSelected(int index)
    {
        List<BoxFile> boxes = BoxesOfSelectedType();
        if (index < 0 || index >= boxes.Count) return;
        SelectedBoxId = boxes[index].Id;
        Refresh();
    }

    private void OnAddBoxClicked()
    {
        manager.AddState(SelectedState);
        BoxFile box = manager.AddBox(SelectedState, SelectedType);
        SelectedBoxId = box.Id;
        Refresh();
    }

    private void OnRemoveBoxClicked()
    {
        if (!TryGetBox(out _)) return;
        int boxId = SelectedBoxId;
        SelectedBoxId = NoBox;
        manager.RemoveBox(SelectedState, boxId);
    }

    // ---------- State properties ----------

    // Editing a state with no data creates it. Bad text puts the old value back.
    // Never shorter than its sprites, so each shows for at least a tick
    private void OnLengthEdited(string text)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int length))
        {
            Refresh();
            return;
        }
        manager.AddState(SelectedState);
        // The manager only knows about the sequence, so a sheet spread without one is held to its frame count here
        if (SelectedSprite == null) length = Math.Max(length, SpriteCount);
        manager.SetStateLength(SelectedState, length);
    }

    private void OnLoopToggled(bool on)
    {
        manager.AddState(SelectedState);
        manager.SetStateLoop(SelectedState, on);
    }

    // ---------- Shared box properties ----------

    private void OnBoxNameEdited(string text)
    {
        if (!TryGetBox(out _)) return;
        manager.RenameBox(SelectedState, SelectedBoxId, text);
    }

    // Index 0 is the root
    private void OnBoxParentSelected(int index)
    {
        if (!TryGetBox(out BoxFile box)) return;
        List<string> parents = GetParentOptions(box);
        manager.SetParent(SelectedState, SelectedBoxId, index == 0 ? "" : parents[index]);
    }

    // Size is keyed, so this sets it on the current frame. Bad text puts the old value back
    private void OnWidthEdited(string text)
    {
        if (!TryGetBox(out _) || !TryParse(text, out DM64 width))
        {
            Refresh();
            return;
        }
        manager.SampleBox(SelectedState, SelectedBoxId, CurrentFrame, out _, out DMVector size);
        manager.SetSize(SelectedState, SelectedBoxId, CurrentFrame, new DMVector(width, size.y));
    }

    private void OnHeightEdited(string text)
    {
        if (!TryGetBox(out _) || !TryParse(text, out DM64 height))
        {
            Refresh();
            return;
        }
        manager.SampleBox(SelectedState, SelectedBoxId, CurrentFrame, out _, out DMVector size);
        manager.SetSize(SelectedState, SelectedBoxId, CurrentFrame, new DMVector(size.x, height));
    }

    // ---------- Hitbox properties ----------

    // Index 0 is "None". Puts the selected box in the group
    private void OnHitGroupSelected(int index)
    {
        if (!TryGetHitbox(out _)) return;
        int hitGroupId = index == 0 ? BoxFile.NoHitGroup : manager.GetState(SelectedState).HitGroups[index - 1].Id;
        manager.SetBoxHitGroup(SelectedState, SelectedBoxId, hitGroupId);
    }

    // Creates a group and puts the selected box in it
    private void OnAddHitGroupClicked()
    {
        if (!TryGetHitbox(out _)) return;
        HitGroupFile group = manager.AddHitGroup(SelectedState);
        manager.SetBoxHitGroup(SelectedState, SelectedBoxId, group.Id);
    }

    // Removes the selected box's group, which also clears it from every other box in it
    private void OnRemoveHitGroupClicked()
    {
        if (!TryGetHitbox(out BoxFile box) || box.HitGroup == BoxFile.NoHitGroup) return;
        manager.RemoveHitGroup(SelectedState, box.HitGroup);
    }

    // What the hit deals belongs to each hitbox, so boxes sharing a hit group can deal different hits
    private void OnDamageEdited(string text)
    {
        if (!TryGetHitbox(out BoxFile box) || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int damage))
        {
            Refresh();
            return;
        }
        manager.SetBoxHit(SelectedState, SelectedBoxId, damage, box.Hitstun, box.Knockback);
    }

    private void OnHitstunEdited(string text)
    {
        if (!TryGetHitbox(out BoxFile box) || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hitstun))
        {
            Refresh();
            return;
        }
        manager.SetBoxHit(SelectedState, SelectedBoxId, box.Damage, hitstun, box.Knockback);
    }

    private void OnKnockbackChanged(Vector2 knockback)
    {
        if (!TryGetHitbox(out BoxFile box))
        {
            Refresh();
            return;
        }
        manager.SetBoxHit(SelectedState, SelectedBoxId, box.Damage, box.Hitstun, new DMVector(ToDM64(knockback.X), ToDM64(knockback.Y)));
    }

    // ---------- Animation properties ----------

    // A state with no sequence yet starts from its sheet cut into square frames, as the view guesses
    private void OnSetAnimClicked()
    {
        StateFile state = manager.HasState(SelectedState) ? manager.GetState(SelectedState) : null;
        SpriteFile sprite = state?.Sprite;
        string sheet = state != null && state.Animation != "" ? state.Animation : SelectedState;
        sheetDialog.Open(
            CharacterFiles.ResourceFolder(manager.CharacterId),
            sheet,
            sprite?.Columns ?? Math.Max(1, preview.FrameCount(sheet)),
            sprite?.Rows ?? 1);
    }

    private void OnSheetSubmitted(string sheet, int columns, int rows)
    {
        manager.AddState(SelectedState);
        manager.SetSprite(SelectedState, sheet, columns, rows);
    }

    private void OnEntrySelected(int index)
    {
        SelectEntry(index);
        Refresh();
    }

    private void OnEntryMoved(int from, int to)
    {
        SelectEntry(to);
        manager.MoveSequenceEntry(SelectedState, from, to);
    }

    // Bad text, or a sprite the sheet doesn't have, puts the old value back
    private void OnEntrySpriteEdited(string text)
    {
        SpriteFile sprite = SelectedSprite;
        if (sprite == null || SelectedEntry == NoEntry
            || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            || value < 0 || value >= sprite.Columns * sprite.Rows)
        {
            Refresh();
            return;
        }
        manager.SetSequenceEntry(SelectedState, SelectedEntry, value);
    }

    // Adds a copy of the selected entry after it, so holding a sprite longer is one click.
    // The state grows a tick if it's already as short as the sequence.
    // Works on an emptied sequence too, so SelectedSprite isn't used
    private void OnAddEntryClicked()
    {
        SpriteFile sprite = manager.HasState(SelectedState) ? manager.GetSprite(SelectedState) : null;
        if (sprite == null) return;
        bool hasEntry = SelectedEntry != NoEntry && SelectedEntry < sprite.Sequence.Count;
        int index = hasEntry ? SelectedEntry + 1 : sprite.Sequence.Count;
        int value = hasEntry ? sprite.Sequence[SelectedEntry] : 0;
        // Selected after it's added, since where an entry starts depends on how many there are
        manager.AddSequenceEntry(SelectedState, index, value);
        SelectEntry(index);
        Refresh();
    }

    private void OnRemoveEntryClicked()
    {
        if (SelectedSprite == null || SelectedEntry == NoEntry) return;
        manager.RemoveSequenceEntry(SelectedState, SelectedEntry);
    }

    // Moves the timeline to the entry's first tick. An entry the state is too short to reach is still selected
    private void SelectEntry(int index)
    {
        Playing = false;
        SelectedEntry = index;
        SpriteFile sprite = manager.HasState(SelectedState) ? manager.GetSprite(SelectedState) : null;
        if (sprite == null || sprite.Sequence.Count == 0) return;
        int start = SpriteSequenceMath.EntryStart(index, sprite.Sequence.Count, StateLength);
        if (start < StateLength) CurrentFrame = start;
    }

    // Selects the entry showing on the current frame, after the timeline moves
    private void FollowFrame()
    {
        SpriteFile sprite = SelectedSprite;
        SelectedEntry = sprite == null
            ? NoEntry
            : SpriteSequenceMath.EntryAt(CurrentFrame, sprite.Sequence.Count, StateLength);
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

    // The slider is in steps, which are sprites or ticks
    private void OnFrameChanged(int step)
    {
        CurrentFrame = StepStart(Math.Clamp(step, 0, StepCount - 1));
        FollowFrame();
        Refresh();
    }

    private void OnPlayClicked()
    {
        Playing = true;
        playbackTime = 0;
        Refresh();
    }

    // Snaps back to the start of the sprite, so edits never land mid sprite
    private void OnPauseClicked()
    {
        Playing = false;
        CurrentFrame = StepStart(StepOf(CurrentFrame));
        FollowFrame();
        Refresh();
    }

    private void OnPreviousFrameClicked()
    {
        Playing = false;
        CurrentFrame = StepStart((StepOf(CurrentFrame) - 1 + StepCount) % StepCount);
        FollowFrame();
        Refresh();
    }

    private void OnNextFrameClicked()
    {
        Playing = false;
        CurrentFrame = StepStart((StepOf(CurrentFrame) + 1) % StepCount);
        FollowFrame();
        Refresh();
    }

    // ---------- Frame properties ----------

    private void OnPositionXEdited(string text)
    {
        if (!TryGetBox(out _) || !TryParse(text, out DM64 x))
        {
            Refresh();
            return;
        }
        manager.SampleBox(SelectedState, SelectedBoxId, CurrentFrame, out DMVector offset, out _);
        manager.SetPosition(SelectedState, SelectedBoxId, CurrentFrame, new DMVector(x, offset.y));
    }

    private void OnPositionYEdited(string text)
    {
        if (!TryGetBox(out _) || !TryParse(text, out DM64 y))
        {
            Refresh();
            return;
        }
        manager.SampleBox(SelectedState, SelectedBoxId, CurrentFrame, out DMVector offset, out _);
        manager.SetPosition(SelectedState, SelectedBoxId, CurrentFrame, new DMVector(offset.x, y));
    }

    // Covers every tick the current sprite is shown for
    private void OnActiveToggled(bool on)
    {
        if (!TryGetBox(out _)) return;
        bool[] active = manager.GetActiveFrames(SelectedState, SelectedBoxId);
        int step = StepOf(CurrentFrame);
        for (int frame = StepStart(step); frame < StepStart(step + 1); frame++)
        {
            active[frame] = on;
        }
        manager.SetActiveFrames(SelectedState, SelectedBoxId, active);
    }

    private void OnRemoveKeyClicked()
    {
        if (!TryGetBox(out _)) return;
        manager.RemoveKey(SelectedState, SelectedBoxId, CurrentFrame);
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

        // State properties. Always editable, since editing a state with no data creates it
        StateFile state = manager.HasState(SelectedState) ? manager.GetState(SelectedState) : null;
        ShowText(lengthInput, state != null ? state.Length.ToString(CultureInfo.InvariantCulture) : "", true);
        loopToggle.SetPressedNoSignal(state != null && state.Loop);
        AttackFile attack = manager.GetAttack(SelectedState);
        cancelOnLandRow.Visible = attack != null;
        cancelOnLandToggle.SetPressedNoSignal(attack != null && attack.CancelOnLand);
        string warning = LengthWarning();
        lengthWarning.Visible = warning != "";
        lengthInput.TooltipText = warning;

        // Property and box. The box controls are hidden while editing the animation
        Fill(propertyDropdown, BoxTypes.Select(type => type.ToString()).Append("Animation").Append("Collision"),
            EditingAnimation ? AnimationIndex : EditingCollision ? CollisionIndex : Array.IndexOf(BoxTypes, SelectedType));
        boxRow.Visible = EditingBoxes;
        sharedProperties.Visible = EditingBoxes;
        List<BoxFile> boxes = BoxesOfSelectedType();
        Fill(boxDropdown, boxes.Select(b => b.Name), boxes.FindIndex(b => b.Id == SelectedBoxId));
        boxDropdown.Disabled = boxes.Count == 0;
        bool hasBox = TryGetBox(out BoxFile box);
        removeBoxButton.Disabled = !hasBox;

        DMVector offset = DMVector.zero;
        DMVector size = DMVector.zero;
        bool active = false;
        bool hasKey = false;
        if (hasBox)
        {
            manager.SampleBox(SelectedState, SelectedBoxId, CurrentFrame, out offset, out size);
            active = manager.GetActiveFrames(SelectedState, SelectedBoxId)[CurrentFrame];
            hasKey = manager.HasKey(SelectedState, SelectedBoxId, CurrentFrame);
        }

        // Shared properties
        ShowText(nameInput, hasBox ? box.Name : "", hasBox);
        List<string> parents = hasBox ? GetParentOptions(box) : new List<string> { "Root" };
        Fill(parentDropdown, parents, hasBox && box.Bone != "" ? parents.IndexOf(box.Bone) : 0);
        parentDropdown.Disabled = !hasBox;
        ShowText(widthInput, hasBox ? Format(size.x) : "", hasBox);
        ShowText(heightInput, hasBox ? Format(size.y) : "", hasBox);

        // Only the selected type's own properties
        foreach (var (type, section) in typeSections)
        {
            section.Visible = EditingBoxes && type == SelectedType;
        }
        ShowHitboxProperties(hasBox ? box : null);
        animationProperties.Visible = EditingAnimation;
        ShowAnimationProperties(state);
        collisionProperties.Visible = EditingCollision;
        ShowCollisionProperties();

        // Timeline. Key notches aren't drawn yet, see the scene's FrameSlider
        int stepCount = StepCount;
        int currentStep = StepOf(CurrentFrame);
        frameSlider.MaxValue = Math.Max(0, stepCount - 1);
        frameSlider.SetValueNoSignal(currentStep);
        frameLabel.Text = $"{currentStep} / {stepCount - 1}";
        playButton.Disabled = Playing;
        pauseButton.Disabled = !Playing;

        // Frame properties
        frameHeader.Text = $"FRAME {currentStep}";
        ShowText(positionXInput, hasBox ? Format(offset.x) : "", hasBox);
        ShowText(positionYInput, hasBox ? Format(offset.y) : "", hasBox);
        activeToggle.SetPressedNoSignal(active);
        activeToggle.Disabled = !hasBox;
        keyStateLabel.Text = !hasBox ? "" : hasKey ? "Keyed" : "Blended";
        removeKeyButton.Disabled = !(hasBox && hasKey && box.Keys.Count > 1);

        preview.Show(manager.Definition, SelectedState, CurrentFrame, hasBox ? SelectedBoxId : NoBox,
            EditingCollision ? manager.Data.Body.Size : null);
    }

    // box is null when no box is selected
    private void ShowHitboxProperties(BoxFile box)
    {
        bool hasBox = box != null;
        var hitGroupNames = new List<string> { "None" };
        int hitGroupIndex = 0;
        if (manager.HasState(SelectedState))
        {
            List<HitGroupFile> groups = manager.GetState(SelectedState).HitGroups;
            for (int i = 0; i < groups.Count; i++)
            {
                hitGroupNames.Add(groups[i].Name);
                if (hasBox && groups[i].Id == box.HitGroup) hitGroupIndex = i + 1;
            }
        }
        Fill(hitGroupDropdown, hitGroupNames, hitGroupIndex);
        hitGroupDropdown.Disabled = !hasBox;
        addHitGroupButton.Disabled = !hasBox;
        removeHitGroupButton.Disabled = !hasBox || hitGroupIndex == 0;

        ShowText(damageInput, hasBox ? box.Damage.ToString(CultureInfo.InvariantCulture) : "", hasBox);
        ShowText(hitstunInput, hasBox ? box.Hitstun.ToString(CultureInfo.InvariantCulture) : "", hasBox);
        knockbackPicker.SetValue(hasBox ? box.Knockback.ToStandardVector() : Vector2.Zero);
        knockbackPicker.MouseFilter = hasBox ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        knockbackPicker.XInput.Editable = hasBox;
        knockbackPicker.YInput.Editable = hasBox;
    }

    // state is null when the state has no data yet
    private void ShowAnimationProperties(StateFile state)
    {
        SpriteFile sprite = state?.Sprite;
        sheetSummaryLabel.Text = sprite == null
            ? "No sequence, the sheet is spread evenly over the state"
            : $"{state.Animation}: {sprite.Columns} x {sprite.Rows}, spread over {state.Length} ticks";

        // Only rebuilt when an entry changes, so the list keeps its scroll while the timeline plays
        var items = new List<string>();
        var problems = new List<bool>();
        if (sprite != null)
        {
            for (int i = 0; i < sprite.Sequence.Count; i++)
            {
                int start = SpriteSequenceMath.EntryStart(i, sprite.Sequence.Count, StateLength);
                int end = SpriteSequenceMath.EntryStart(i + 1, sprite.Sequence.Count, StateLength) - 1;
                bool inSheet = sprite.Sequence[i] >= 0 && sprite.Sequence[i] < sprite.Columns * sprite.Rows;
                // Only a hand edited file is shorter than its sequence
                bool shows = end >= start;
                string note = !inSheet ? "  (not in sheet)" : !shows ? "  (never shows)" : "";
                items.Add($"{i}:  sprite {sprite.Sequence[i]}    ticks {start}-{end}{note}");
                problems.Add(!inSheet || !shows);
            }
        }
        bool same = items.Count == sequenceList.ItemCount;
        for (int i = 0; same && i < items.Count; i++) same = sequenceList.GetItemText(i) == items[i];
        if (!same)
        {
            sequenceList.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                sequenceList.AddItem(items[i]);
                if (problems[i]) sequenceList.SetItemCustomFgColor(i, WarningColor);
            }
        }

        bool hasEntry = sprite != null && SelectedEntry != NoEntry && SelectedEntry < sprite.Sequence.Count;
        if (hasEntry)
        {
            sequenceList.Select(SelectedEntry);
            sequenceList.EnsureCurrentIsVisible();
        }
        else
        {
            sequenceList.DeselectAll();
        }
        ShowText(entrySpriteInput, hasEntry ? sprite.Sequence[SelectedEntry].ToString(CultureInfo.InvariantCulture) : "", hasEntry);
        addEntryButton.Disabled = sprite == null;
        removeEntryButton.Disabled = !hasEntry;
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

    // 1 for a state with no data yet
    private int StateLength => manager.Loaded != null && manager.HasState(SelectedState) ? manager.GetState(SelectedState).Length : 1;

    // The selected state's sprite sequence. Null when it has none, or it's empty, since an empty one isn't played
    private SpriteFile SelectedSprite
    {
        get
        {
            if (manager.Loaded == null || !manager.HasState(SelectedState)) return null;
            SpriteFile sprite = manager.GetSprite(SelectedState);
            return sprite != null && sprite.Sequence.Count > 0 ? sprite : null;
        }
    }

    // In file order, for the box dropdown
    private List<BoxFile> BoxesOfSelectedType()
    {
        if (manager.Loaded == null || !manager.HasState(SelectedState)) return new List<BoxFile>();
        return manager.GetState(SelectedState).Boxes.Where(box => box.Type == SelectedType).ToList();
    }

    // ---------- Steps ----------
    // The timeline moves one step at a time. For a sprite animation a step is one sprite, or one sequence entry,
    // covering every tick it's shown for, since boxes never need to change mid sprite. Otherwise a step is one tick.
    // Steps are numbered from 0 and grow by at most one a tick, since the state is never shorter than its sprites,
    // so StepOf alone defines them

    // 0 when there's no data for the state, no sheet, or the view doesn't step
    private int SpriteCount
    {
        get
        {
            if (manager.Loaded == null || !manager.HasState(SelectedState)) return 0;
            StateDefinition state = manager.Definition.GetStateOrAttack(SelectedState);
            return preview.FrameCount(state != null ? state.AnimationName : SelectedState);
        }
    }

    // A state shorter than its sheet can't give every sprite a tick, so it steps per tick instead
    private bool StepsBySprite => SpriteCount > 0 && SpriteCount < StateLength;

    private int StepCount => StepOf(StateLength - 1) + 1;

    // Matches SpriteCharacterView, which spreads the sequence, or the sheet without one, evenly over the length
    private int StepOf(int frame)
    {
        SpriteFile sprite = SelectedSprite;
        if (sprite != null) return SpriteSequenceMath.EntryAt(frame, sprite.Sequence.Count, StateLength);
        return StepsBySprite ? frame * SpriteCount / StateLength : frame;
    }

    // First tick of the step. StepStart(StepCount) is the state's length
    private int StepStart(int step)
    {
        for (int frame = 0; frame < StateLength; frame++)
        {
            if (StepOf(frame) >= step) return frame;
        }
        return StateLength;
    }

    // Empty when the length splits evenly over the sequence, or the sheet without one, or there are no sprites
    private string LengthWarning()
    {
        SpriteFile sprite = SelectedSprite;
        int sprites = sprite != null ? sprite.Sequence.Count : SpriteCount;
        int length = StateLength;
        if (sprites == 0 || length % sprites == 0) return "";
        if (length < sprites) return $"{length} ticks is shorter than the {sprites} sprites, so some will never show";
        return $"{length} ticks don't divide evenly into {sprites} sprites, so some show a tick longer than others";
    }

    // Never while editing the animation or collision, so box controls and edits are off without losing the selection
    private bool TryGetBox(out BoxFile box)
    {
        box = null;
        if (!EditingBoxes) return false;
        if (SelectedBoxId == NoBox || manager.Loaded == null || !manager.HasState(SelectedState)) return false;
        foreach (var candidate in manager.GetState(SelectedState).Boxes)
        {
            if (candidate.Id == SelectedBoxId)
            {
                box = candidate;
                return true;
            }
        }
        return false;
    }

    private bool TryGetHitbox(out BoxFile box) => TryGetBox(out box) && box.Type == BoxType.Hitbox;

    // After a load, removal or type change the selected box may be gone, so the type's first box is picked instead.
    // The state may also have shrunk past the current frame, or its sequence past the selected entry
    private void ValidateSelection()
    {
        if (manager.Loaded == null) return;
        // An attack is only listed while the loaded character has it
        if (Array.IndexOf(manager.StateNames, SelectedState) < 0)
        {
            SelectedState = HitboxEditorManager.AllStateNames[0];
            SelectedBoxId = NoBox;
            SelectedEntry = NoEntry;
        }
        if (EditingBoxes && (!TryGetBox(out BoxFile box) || box.Type != SelectedType))
        {
            List<BoxFile> boxes = BoxesOfSelectedType();
            SelectedBoxId = boxes.Count > 0 ? boxes[0].Id : NoBox;
        }
        CurrentFrame = Math.Clamp(CurrentFrame, 0, StateLength - 1);
        if (!Playing) CurrentFrame = StepStart(StepOf(CurrentFrame));

        SpriteFile sprite = SelectedSprite;
        if (sprite == null) SelectedEntry = NoEntry;
        else if (SelectedEntry >= sprite.Sequence.Count) SelectedEntry = sprite.Sequence.Count - 1;
        else if (SelectedEntry == NoEntry) FollowFrame();
    }

    // "Root", then the bones baked for the state. The box's own bone is kept even when it has no bake
    private List<string> GetParentOptions(BoxFile box)
    {
        var parents = new List<string> { "Root" };
        parents.AddRange(manager.GetBones(SelectedState));
        if (box.Bone != "" && !parents.Contains(box.Bone)) parents.Add(box.Bone);
        return parents;
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

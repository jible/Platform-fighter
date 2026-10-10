using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// Connects the hitbox editor scene's controls to the HitboxEditorManager.
// Controls raise edits, which go to the manager, and its Changed event redraws everything.
// Selection, the selected box type and the current frame live here, since the manager only holds file data.
// Controls are found by their unique names, and signals are connected in _Ready
public partial class HitboxEditor : Node
{
    private const int NoBox = -1;
    // DM64's fractional bits, for converting typed text without passing through a float, like CharacterFileJson
    private const decimal DMScale = 4294967296m;

    [Export] public int PlaybackFramesPerSecond = 60;
    // Write the character file after every edit
    [Export] public bool AutoSave = true;

    private static readonly CharacterId[] Characters = Enum.GetValues<CharacterId>();

    // A state with no data yet can still be selected, and is created on the first edit to it
    public string SelectedState { get; private set; } = HitboxEditorManager.AllStateNames[0];
    // Only boxes of this type are listed, and new boxes are made with it
    public BoxType SelectedType { get; private set; } = BoxType.Hitbox;
    public int SelectedBoxId { get; private set; } = NoBox;
    // Always a tick. When stepping by sprite it is the first tick of the current sprite, except mid playback
    public int CurrentFrame { get; private set; }
    public bool Playing { get; private set; }

    private HitboxEditorManager manager;
    private HitboxPreview preview;
    private double playbackTime;
    // The text each field was last shown, so leaving a field that wasn't changed isn't an edit
    private readonly Dictionary<LineEdit, string> shownText = new();

    // File
    private Label unsavedLabel;
    private Button reloadButton, saveButton;
    // Pickers
    private OptionButton characterDropdown, stateDropdown, boxDropdown;
    private Button addBoxButton, removeBoxButton;
    private readonly Dictionary<BoxType, Button> typeButtons = new();
    // State properties
    private LineEdit lengthInput, animationInput;
    private Label lengthWarning;
    private CheckBox loopToggle;
    // Shared box properties
    private LineEdit nameInput, widthInput, heightInput;
    private OptionButton parentDropdown;
    // One section per box type, only the selected type's shown
    private readonly Dictionary<BoxType, Control> typeSections = new();
    // Hitbox properties
    private OptionButton hitGroupDropdown;
    private Button addHitGroupButton, removeHitGroupButton;
    private LineEdit damageInput, hitstunInput;
    private KnockbackPicker knockbackPicker;
    // Timeline
    private HSlider frameSlider;
    private Label frameLabel;
    private Button playButton, pauseButton, previousFrameButton, nextFrameButton;
    // Frame properties
    private Label frameHeader, keyStateLabel;
    private LineEdit positionXInput, positionYInput;
    private CheckBox activeToggle;
    private Button removeKeyButton;

    public override void _Ready()
    {
        FindControls();
        ConnectControls();

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
        boxDropdown = GetNode<OptionButton>("%BoxDropdown");
        addBoxButton = GetNode<Button>("%AddBoxButton");
        removeBoxButton = GetNode<Button>("%RemoveBoxButton");
        typeButtons[BoxType.Pushbox] = GetNode<Button>("%PushboxTypeButton");
        typeButtons[BoxType.Hitbox] = GetNode<Button>("%HitboxTypeButton");
        typeButtons[BoxType.Hurtbox] = GetNode<Button>("%HurtboxTypeButton");

        lengthInput = GetNode<LineEdit>("%LengthInput");
        lengthWarning = GetNode<Label>("%LengthWarning");
        loopToggle = GetNode<CheckBox>("%LoopToggle");
        animationInput = GetNode<LineEdit>("%AnimationInput");

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
        foreach (var (type, button) in typeButtons)
        {
            button.Toggled += pressed => { if (pressed) OnTypeSelected(type); };
        }
        boxDropdown.ItemSelected += index => OnBoxSelected((int)index);
        addBoxButton.Pressed += OnAddBoxClicked;
        removeBoxButton.Pressed += OnRemoveBoxClicked;

        OnEndEdit(lengthInput, OnLengthEdited);
        loopToggle.Toggled += OnLoopToggled;
        OnEndEdit(animationInput, OnAnimationEdited);

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
        preview.SetCharacter(manager.CharacterId);
        ValidateSelection();
        Refresh();
    }

    // ---------- Pickers ----------

    // Unsaved edits to the previous character are dropped, the same as a reload
    private void OnCharacterSelected(int index)
    {
        SelectedBoxId = NoBox;
        CurrentFrame = 0;
        manager.LoadCharacter(Characters[index]);
    }

    private void OnStateSelected(int index)
    {
        SelectedState = HitboxEditorManager.AllStateNames[index];
        SelectedBoxId = NoBox;
        CurrentFrame = 0;
        ValidateSelection();
        Refresh();
    }

    // Picks the type's first box, since the old selection was another type
    private void OnTypeSelected(BoxType type)
    {
        if (type == SelectedType) return;
        SelectedType = type;
        SelectedBoxId = NoBox;
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

    // Editing a state with no data creates it. Bad text puts the old value back
    private void OnLengthEdited(string text)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int length))
        {
            Refresh();
            return;
        }
        manager.AddState(SelectedState);
        manager.SetStateLength(SelectedState, length);
    }

    private void OnLoopToggled(bool on)
    {
        manager.AddState(SelectedState);
        manager.SetStateLoop(SelectedState, on);
    }

    private void OnAnimationEdited(string text)
    {
        manager.AddState(SelectedState);
        manager.SetStateAnimation(SelectedState, text);
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

    // ---------- Timeline ----------

    // The slider is in steps, which are sprites or ticks
    private void OnFrameChanged(int step)
    {
        CurrentFrame = StepStart(Math.Clamp(step, 0, StepCount - 1));
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
        Refresh();
    }

    private void OnPreviousFrameClicked()
    {
        Playing = false;
        CurrentFrame = StepStart((StepOf(CurrentFrame) - 1 + StepCount) % StepCount);
        Refresh();
    }

    private void OnNextFrameClicked()
    {
        Playing = false;
        CurrentFrame = StepStart((StepOf(CurrentFrame) + 1) % StepCount);
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
        Fill(stateDropdown, HitboxEditorManager.AllStateNames, Array.IndexOf(HitboxEditorManager.AllStateNames, SelectedState));

        // State properties. Always editable, since editing a state with no data creates it
        StateFile state = manager.HasState(SelectedState) ? manager.GetState(SelectedState) : null;
        ShowText(lengthInput, state != null ? state.Length.ToString(CultureInfo.InvariantCulture) : "", true);
        loopToggle.SetPressedNoSignal(state != null && state.Loop);
        ShowText(animationInput, state != null ? state.Animation : "", true);
        string warning = LengthWarning();
        lengthWarning.Visible = warning != "";
        lengthInput.TooltipText = warning;

        // Box type and box
        foreach (var (type, button) in typeButtons)
        {
            button.SetPressedNoSignal(type == SelectedType);
        }
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
            section.Visible = type == SelectedType;
        }
        ShowHitboxProperties(hasBox ? box : null);

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

        preview.Show(manager.Definition, SelectedState, CurrentFrame, hasBox ? SelectedBoxId : NoBox);
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

    // In file order, for the box dropdown
    private List<BoxFile> BoxesOfSelectedType()
    {
        if (manager.Loaded == null || !manager.HasState(SelectedState)) return new List<BoxFile>();
        return manager.GetState(SelectedState).Boxes.Where(box => box.Type == SelectedType).ToList();
    }

    // ---------- Steps ----------
    // The timeline moves one step at a time. For a sprite animation a step is one sprite, covering every tick
    // it's shown for, since boxes never need to change mid sprite. Otherwise a step is one tick

    // 0 when there's no data for the state, no sheet, or the view doesn't step
    private int SpriteCount
    {
        get
        {
            if (manager.Loaded == null || !manager.HasState(SelectedState)) return 0;
            StateDefinition state = manager.Definition.GetState(SelectedState);
            return preview.FrameCount(state != null ? state.AnimationName : SelectedState);
        }
    }

    // A state shorter than its sheet can't give every sprite a tick, so it steps per tick instead
    private bool StepsBySprite => SpriteCount > 0 && SpriteCount < StateLength;

    private int StepCount => StepsBySprite ? SpriteCount : StateLength;

    // First tick showing the step's sprite. The inverse of SpriteCharacterView's frame * sprites / length,
    // so it holds when the length doesn't divide evenly. StepStart(StepCount) is the state's length
    private int StepStart(int step)
    {
        if (!StepsBySprite) return step;
        int sprites = SpriteCount;
        return (step * StateLength + sprites - 1) / sprites;
    }

    private int StepOf(int frame) => StepsBySprite ? frame * SpriteCount / StateLength : frame;

    // Empty when the length splits evenly over the sprites, or there are no sprites
    private string LengthWarning()
    {
        int sprites = SpriteCount;
        int length = StateLength;
        if (sprites == 0 || length % sprites == 0) return "";
        if (length < sprites) return $"{length} ticks is shorter than the {sprites} sprites, so some will never show";
        return $"{length} ticks don't divide evenly into {sprites} sprites, so some show longer than others";
    }

    private bool TryGetBox(out BoxFile box)
    {
        box = null;
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
    // The state may also have shrunk past the current frame
    private void ValidateSelection()
    {
        if (manager.Loaded == null) return;
        if (!TryGetBox(out BoxFile box) || box.Type != SelectedType)
        {
            List<BoxFile> boxes = BoxesOfSelectedType();
            SelectedBoxId = boxes.Count > 0 ? boxes[0].Id : NoBox;
        }
        CurrentFrame = Math.Clamp(CurrentFrame, 0, StateLength - 1);
        if (!Playing) CurrentFrame = StepStart(StepOf(CurrentFrame));
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

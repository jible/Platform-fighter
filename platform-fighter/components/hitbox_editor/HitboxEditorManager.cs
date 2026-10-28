using System;
using System.Collections.Generic;

// Back end of the hitbox editor. Holds one character's file and is the only thing that edits it,
// so the UI calls these methods instead of touching the file classes directly.
// States are addressed by name, phases by index, sprites by their place in the sheet and boxes by id.
// A missing one is a caller bug and throws.
// A box's id, name, type and hit group are what make it the same box on every sprite it's copied to, so editing
// one of those changes every copy. Its place, size and hit belong to the one sprite. Plain C#, owned by the HitboxEditor
public class HitboxEditorManager
{
    private CharacterId characterId;

    public CharacterId CharacterId => characterId;
    public CharacterFile Loaded { get; private set; }
    // The character's data with the loaded file's overrides applied. Edited along with the file, so the preview shows them
    public CharacterData Data { get; private set; }
    public bool HasUnsavedChanges { get; private set; }

    // Raised after every edit and load, so the UI and preview can redraw
    public event Action Changed;

    // Rebuilt lazily after edits, so the preview draws exactly what play would use
    private CharacterDefinition definition;
    public CharacterDefinition Definition => definition ??= CharacterDefinitionBuilder.Build(Loaded, Data.Visuals.BoxUnit);

    // New boxes start about a world unit across, in the character's box units, so they're visible at any pixel size
    private DMVector NewBoxSize
    {
        get
        {
            DM64 side = DM64.Max(new DM64(1), (new DM64(1) / Data.Visuals.BoxUnit).Round());
            return new DMVector(side, side);
        }
    }

    // Every state the game has, whether or not it has data yet. Not Attack, which plays each attack's data instead of its own
    public static string[] AllStateNames => Array.FindAll(Enum.GetNames(typeof(CharacterStateId)), name => name != nameof(CharacterStateId.Attack));

    // Every state, then the loaded character's attacks in the order the game numbers them.
    // Attacks are edited like any other state, since an AttackFile is a StateFile. They're only added in the file
    public string[] StateNames
    {
        get
        {
            var names = new List<string>(AllStateNames);
            if (Loaded == null) return names.ToArray();
            var attacks = new List<string>(Loaded.Attacks.Keys);
            attacks.Sort(StringComparer.Ordinal);
            names.AddRange(attacks);
            return names.ToArray();
        }
    }

    // ---------- Files ----------

    // A character with no file yet starts empty, and gets one on the first save
    public void LoadCharacter(CharacterId id)
    {
        characterId = id;
        Loaded = CharacterFiles.ReadCharacter(id);
        Data = CharacterFiles.LoadData(id, Loaded);
        definition = null;
        HasUnsavedChanges = false;
        Changed?.Invoke();
    }

    public void Save()
    {
        CharacterFiles.WriteCharacter(characterId, Loaded);
        HasUnsavedChanges = false;
        Changed?.Invoke();
    }

    // ---------- Collision ----------
    // Not per state, so these are the same everywhere

    // In world units, like the physics body
    public void SetBodySize(DMVector size)
    {
        size = new DMVector(DM64.Max(size.x, MinBodySide), DM64.Max(size.y, MinBodySide));
        Loaded.BodySize = size;
        Data.Body.Size = size;
        MarkChanged();
    }

    // In box units, like box offsets
    public void SetVisualOffset(DMVector offset)
    {
        Loaded.VisualOffset = offset;
        Data.Visuals.Offset = offset;
        MarkChanged();
    }

    // A body with no width or height would fall through the stage
    private static readonly DM64 MinBodySide = new DM64(1) / 100;

    // ---------- States ----------

    // States and attacks share names, so either can be passed anywhere a state name is taken
    public bool HasState(string stateName) => Loaded.States.ContainsKey(stateName) || Loaded.Attacks.ContainsKey(stateName);

    public StateFile GetState(string stateName)
    {
        if (Loaded.States.TryGetValue(stateName, out StateFile state)) return state;
        if (Loaded.Attacks.TryGetValue(stateName, out AttackFile attack)) return attack;
        throw new ArgumentException($"No state or attack named '{stateName}'");
    }

    // Sets up a state with no data from its grid: one phase over the whole sheet, as long as the game ran
    // the state before it had data. An existing state just gets the grid
    public void AddState(string stateName, int columns, int rows, int sheetWidth, int sheetHeight)
    {
        if (HasState(stateName))
        {
            SetGrid(stateName, columns, rows, sheetWidth, sheetHeight);
            return;
        }
        var state = new StateFile { Sprite = new SpriteFile() };
        SetGridSize(state, columns, rows, sheetWidth, sheetHeight);
        int count = state.Sprite.Count;
        state.Phases.Add(new PhaseFile
        {
            Id = state.NextPhaseId++,
            Name = "Main",
            Start = 0,
            End = count - 1,
            Length = Math.Max(Data.DefaultStateLength, count),
        });
        FitFrames(state);
        Loaded.States[stateName] = state;
        MarkChanged();
    }

    public void RemoveState(string stateName)
    {
        if (Loaded.States.Remove(stateName)) MarkChanged();
    }

    // Null when the name is a state's, or has no data
    public AttackFile GetAttack(string name) => Loaded.Attacks.TryGetValue(name, out AttackFile attack) ? attack : null;

    // Attacks only, since they're only added in the file
    public void SetAttackCancelOnLand(string attackName, bool cancel)
    {
        AttackFile attack = GetAttack(attackName);
        if (attack == null) return;
        attack.CancelOnLand = cancel;
        MarkChanged();
    }

    // Sprites in the state's sheet, at least 1
    public int SpriteCount(string stateName) => GetState(stateName).Sprite?.Count ?? 1;

    // ---------- Sprite grid ----------

    // Recuts the sheet without moving any data. Boxes on sprites past the new count are kept in the file, so
    // shrinking by mistake loses nothing. Phases are cut to fit, and the last one grows to cover new sprites.
    // When sprites were added to or removed from the art instead, use ShiftSprites so the data moves with them
    public void SetGrid(string stateName, int columns, int rows, int sheetWidth, int sheetHeight)
    {
        StateFile state = GetState(stateName);
        state.Sprite ??= new SpriteFile();
        SetGridSize(state, columns, rows, sheetWidth, sheetHeight);
        FitFrames(state);
        FitPhases(state);
        MarkChanged();
    }

    // The art gained delta sprites before index, or lost -delta sprites from index on, so boxes and phases move
    // with the sprites they were on. New sprites join the phase whose sprite they were added in front of, or the last
    // phase at the end. A phase that loses every sprite is removed. Then the grid is set to the sheet's new size
    public void ShiftSprites(string stateName, int index, int delta, int columns, int rows, int sheetWidth, int sheetHeight)
    {
        StateFile state = GetState(stateName);
        state.Sprite ??= new SpriteFile();
        int oldCount = state.Sprite.Count;
        FitFrames(state);

        if (delta > 0)
        {
            index = Math.Clamp(index, 0, oldCount);
            for (int i = 0; i < delta; i++) state.Frames.Insert(index, new FrameFile());
            for (int i = 0; i < state.Phases.Count; i++)
            {
                PhaseFile phase = state.Phases[i];
                bool last = i == state.Phases.Count - 1;
                if (phase.Start > index)
                {
                    phase.Start += delta;
                    phase.End += delta;
                }
                else if (phase.End >= index || last)
                {
                    phase.End += delta;
                }
            }
        }
        else if (delta < 0)
        {
            index = Math.Clamp(index, 0, oldCount - 1);
            int removed = Math.Min(-delta, oldCount - index);
            int lastRemoved = index + removed - 1;
            state.Frames.RemoveRange(index, removed);
            // Every phase is moved first, so the ones left already cover the sprites left when the empty ones go
            var emptied = new List<int>();
            for (int i = 0; i < state.Phases.Count; i++)
            {
                PhaseFile phase = state.Phases[i];
                int overlap = Math.Max(0, Math.Min(phase.End, lastRemoved) - Math.Max(phase.Start, index) + 1);
                if (overlap == phase.SpriteCount)
                {
                    emptied.Add(i);
                    continue;
                }
                phase.Start = phase.Start < index ? phase.Start : phase.Start > lastRemoved ? phase.Start - removed : index;
                phase.End = phase.End < index ? phase.End : phase.End > lastRemoved ? phase.End - removed : index - 1;
            }
            for (int i = emptied.Count - 1; i >= 0 && state.Phases.Count > 1; i--)
            {
                UnlinkPhase(state, emptied[i]);
            }
        }

        SetGridSize(state, columns, rows, sheetWidth, sheetHeight);
        FitFrames(state);
        FitPhases(state);
        MarkChanged();
    }

    // Keeps the grid and data as they are and stops the editor asking about a sheet whose size changed
    public void AcceptSheetSize(string stateName, int sheetWidth, int sheetHeight)
    {
        SpriteFile sprite = GetState(stateName).Sprite ??= new SpriteFile();
        sprite.SheetWidth = sheetWidth;
        sprite.SheetHeight = sheetHeight;
        MarkChanged();
    }

    private static void SetGridSize(StateFile state, int columns, int rows, int sheetWidth, int sheetHeight)
    {
        state.Sprite.Columns = Math.Max(1, columns);
        state.Sprite.Rows = Math.Max(1, rows);
        state.Sprite.SheetWidth = sheetWidth;
        state.Sprite.SheetHeight = sheetHeight;
    }

    // At least one frame per sprite, so every sprite can be given boxes. Extra frames are kept
    private static void FitFrames(StateFile state)
    {
        int count = state.Sprite?.Count ?? 1;
        while (state.Frames.Count < count) state.Frames.Add(new FrameFile());
    }

    // Keeps the phases covering every sprite in order: phases past the end are removed, the rest are cut to fit,
    // the last one runs to the end, and every phase is at least a tick per sprite
    private void FitPhases(StateFile state)
    {
        int count = state.Sprite?.Count ?? 1;
        if (state.Phases.Count == 0)
        {
            state.Phases.Add(new PhaseFile { Id = state.NextPhaseId++, Name = "Main", Length = Math.Max(Data.DefaultStateLength, count) });
        }
        for (int i = state.Phases.Count - 1; i > 0; i--)
        {
            if (state.Phases[i].Start >= count) DropPhase(state, i);
        }
        state.Phases[0].Start = 0;
        for (int i = 0; i < state.Phases.Count; i++)
        {
            PhaseFile phase = state.Phases[i];
            if (i > 0) phase.Start = state.Phases[i - 1].End + 1;
            phase.End = i == state.Phases.Count - 1 ? count - 1 : Math.Clamp(phase.End, phase.Start, count - 1);
            phase.Length = Math.Max(phase.Length, phase.SpriteCount);
        }
    }

    // ---------- Phases ----------

    public PhaseFile GetPhase(string stateName, int phaseIndex) => GetState(stateName).Phases[phaseIndex];

    // Index of the phase playing the sprite. Phases cover every sprite, so there always is one
    public int PhaseOf(string stateName, int sprite)
    {
        List<PhaseFile> phases = GetState(stateName).Phases;
        for (int i = 0; i < phases.Count; i++)
        {
            if (sprite <= phases[i].End) return i;
        }
        return phases.Count - 1;
    }

    // A phase needs at least two sprites to be split
    public bool CanSplitPhase(string stateName, int phaseIndex) => GetPhase(stateName, phaseIndex).SpriteCount >= 2;

    // Splits the phase in two, the new one taking the back half of its sprites and its share of the ticks.
    // The new phase does what the old one did on ending, and the old one now moves on to it. Returns its index
    public int SplitPhase(string stateName, int phaseIndex)
    {
        StateFile state = GetState(stateName);
        PhaseFile phase = state.Phases[phaseIndex];
        if (phase.SpriteCount < 2) return phaseIndex;

        int frontCount = (phase.SpriteCount + 1) / 2;
        int backCount = phase.SpriteCount - frontCount;
        int frontLength = Math.Max(frontCount, phase.Length * frontCount / phase.SpriteCount);
        int id = state.NextPhaseId++;
        var back = new PhaseFile
        {
            Id = id,
            Name = $"Phase {id}",
            Start = phase.Start + frontCount,
            End = phase.End,
            Length = Math.Max(backCount, phase.Length - frontLength),
            OnEnd = phase.OnEnd,
            Target = phase.Target,
        };
        phase.End = back.Start - 1;
        phase.Length = frontLength;
        phase.OnEnd = PhaseEnd.Next;
        phase.Target = PhaseFile.NoPhase;
        state.Phases.Insert(phaseIndex + 1, back);
        MarkChanged();
        return phaseIndex + 1;
    }

    // Merges the phase into the one before it, or the one after for the first, which takes its sprites and ticks.
    // The last phase's ending is kept, so removing the end of a looping state keeps it looping. The only phase stays
    public void RemovePhase(string stateName, int phaseIndex)
    {
        StateFile state = GetState(stateName);
        if (state.Phases.Count < 2) return;
        DropPhase(state, phaseIndex);
        MarkChanged();
    }

    // Moves the boundary after the phase, so it ends on lastSprite and the next phase starts after it.
    // Both keep at least one sprite, and lengths grow if they'd be shorter than their sprites
    public void SetPhaseBoundary(string stateName, int phaseIndex, int lastSprite)
    {
        StateFile state = GetState(stateName);
        if (phaseIndex < 0 || phaseIndex >= state.Phases.Count - 1) return;
        PhaseFile phase = state.Phases[phaseIndex];
        PhaseFile next = state.Phases[phaseIndex + 1];
        phase.End = Math.Clamp(lastSprite, phase.Start, next.End - 1);
        next.Start = phase.End + 1;
        phase.Length = Math.Max(phase.Length, phase.SpriteCount);
        next.Length = Math.Max(next.Length, next.SpriteCount);
        MarkChanged();
    }

    public void RenamePhase(string stateName, int phaseIndex, string name)
    {
        GetPhase(stateName, phaseIndex).Name = name ?? "";
        MarkChanged();
    }

    // Never shorter than its sprites, so each shows for at least a tick
    public void SetPhaseLength(string stateName, int phaseIndex, int length)
    {
        PhaseFile phase = GetPhase(stateName, phaseIndex);
        phase.Length = Math.Max(length, phase.SpriteCount);
        MarkChanged();
    }

    // target is a phase id, used only with PhaseEnd.Phase
    public void SetPhaseEnd(string stateName, int phaseIndex, PhaseEnd onEnd, int target = PhaseFile.NoPhase)
    {
        PhaseFile phase = GetPhase(stateName, phaseIndex);
        phase.OnEnd = onEnd;
        phase.Target = onEnd == PhaseEnd.Phase ? target : PhaseFile.NoPhase;
        MarkChanged();
    }

    // Removes a phase, giving its sprites and ticks to a neighbor
    private static void DropPhase(StateFile state, int phaseIndex)
    {
        PhaseFile phase = state.Phases[phaseIndex];
        PhaseFile neighbor = state.Phases[phaseIndex > 0 ? phaseIndex - 1 : 1];
        neighbor.Start = Math.Min(neighbor.Start, phase.Start);
        neighbor.End = Math.Max(neighbor.End, phase.End);
        neighbor.Length += phase.Length;
        UnlinkPhase(state, phaseIndex);
    }

    // Removes a phase without touching the others' sprites. The neighbor that would take its sprites takes its ending
    // if it was last, and anything going to it goes to that neighbor instead. There must be another phase
    private static void UnlinkPhase(StateFile state, int phaseIndex)
    {
        PhaseFile phase = state.Phases[phaseIndex];
        bool last = phaseIndex == state.Phases.Count - 1;
        PhaseFile neighbor = state.Phases[phaseIndex > 0 ? phaseIndex - 1 : 1];
        if (last)
        {
            neighbor.OnEnd = phase.OnEnd;
            neighbor.Target = phase.Target;
        }
        state.Phases.RemoveAt(phaseIndex);
        foreach (var other in state.Phases)
        {
            if (other.OnEnd == PhaseEnd.Phase && other.Target == phase.Id) other.Target = neighbor.Id;
            // Going to itself is a restart
            if (other.OnEnd == PhaseEnd.Phase && other.Target == other.Id)
            {
                other.OnEnd = PhaseEnd.Restart;
                other.Target = PhaseFile.NoPhase;
            }
        }
    }

    // ---------- Boxes ----------

    public FrameFile GetFrame(string stateName, int sprite)
    {
        StateFile state = GetState(stateName);
        FitFrames(state);
        if (sprite < 0 || sprite >= SpriteCount(stateName))
        {
            throw new ArgumentOutOfRangeException(nameof(sprite), $"'{stateName}' has no sprite {sprite}");
        }
        return state.Frames[sprite];
    }

    // Null when the sprite has no copy of the box
    public BoxFile FindBox(string stateName, int sprite, int boxId)
    {
        foreach (var box in GetFrame(stateName, sprite).Boxes)
        {
            if (box.Id == boxId) return box;
        }
        return null;
    }

    public BoxFile GetBox(string stateName, int sprite, int boxId)
    {
        return FindBox(stateName, sprite, boxId) ?? throw new ArgumentException($"'{stateName}' sprite {sprite} has no box {boxId}");
    }

    // A new hitbox joins the state's first hit group, or a new group if it has none
    public BoxFile AddBox(string stateName, int sprite, BoxType type)
    {
        StateFile state = GetState(stateName);
        FrameFile frame = GetFrame(stateName, sprite);
        int id = state.NextBoxId++;
        var box = new BoxFile
        {
            Id = id,
            Name = $"{type} {id}",
            Type = type,
            Size = NewBoxSize,
        };
        if (type == BoxType.Hitbox) box.HitGroup = DefaultHitGroup(state);
        frame.Boxes.Add(box);
        MarkChanged();
        return box;
    }

    // Only this sprite's copy
    public void RemoveBox(string stateName, int sprite, int boxId)
    {
        GetFrame(stateName, sprite).Boxes.Remove(GetBox(stateName, sprite, boxId));
        MarkChanged();
    }

    // Copies the box onto another sprite, replacing that sprite's copy of it if it has one
    public void CopyBox(string stateName, int fromSprite, int boxId, int toSprite)
    {
        BoxFile copy = CharacterFileJson.Copy(GetBox(stateName, fromSprite, boxId));
        List<BoxFile> boxes = GetFrame(stateName, toSprite).Boxes;
        int existing = boxes.FindIndex(box => box.Id == boxId);
        if (existing >= 0) boxes[existing] = copy;
        else boxes.Add(copy);
        MarkChanged();
    }

    // Every copy
    public void RenameBox(string stateName, int boxId, string name)
    {
        foreach (var box in Copies(stateName, boxId)) box.Name = name ?? "";
        MarkChanged();
    }

    // Every copy. A box still on its default name is renamed to match, so the name never lies about the type.
    // A box becoming a hitbox joins the first hit group, and one leaving it leaves its group
    public void SetBoxType(string stateName, int boxId, BoxType type)
    {
        StateFile state = GetState(stateName);
        foreach (var box in Copies(stateName, boxId))
        {
            if (box.Name == $"{box.Type} {box.Id}") box.Name = $"{type} {box.Id}";
            box.Type = type;
            box.HitGroup = type == BoxType.Hitbox ? DefaultHitGroup(state) : BoxFile.NoHitGroup;
        }
        MarkChanged();
    }

    // This sprite's copy only
    public void SetBoxRect(string stateName, int sprite, int boxId, DMVector offset, DMVector size)
    {
        BoxFile box = GetBox(stateName, sprite, boxId);
        box.Offset = offset;
        box.Size = size;
        MarkChanged();
    }

    // What this sprite's copy deals
    public void SetBoxHit(string stateName, int sprite, int boxId, int damage, int hitstun, DMVector knockback)
    {
        BoxFile box = GetBox(stateName, sprite, boxId);
        box.Damage = damage;
        box.Hitstun = hitstun;
        box.Knockback = knockback;
        MarkChanged();
    }

    // Every copy, since a box's copies are one hitbox
    public void SetBoxHitGroup(string stateName, int boxId, int hitGroupId)
    {
        GetHitGroup(stateName, hitGroupId);
        foreach (var box in Copies(stateName, boxId)) box.HitGroup = hitGroupId;
        MarkChanged();
    }

    private IEnumerable<BoxFile> Copies(string stateName, int boxId)
    {
        foreach (var frame in GetState(stateName).Frames)
        {
            foreach (var box in frame.Boxes)
            {
                if (box.Id == boxId) yield return box;
            }
        }
    }

    // ---------- Hit groups ----------

    public HitGroupFile GetHitGroup(string stateName, int hitGroupId)
    {
        foreach (var group in GetState(stateName).HitGroups)
        {
            if (group.Id == hitGroupId) return group;
        }
        throw new ArgumentException($"State '{stateName}' has no hit group {hitGroupId}");
    }

    public HitGroupFile AddHitGroup(string stateName)
    {
        HitGroupFile group = NewHitGroup(GetState(stateName));
        MarkChanged();
        return group;
    }

    // Every hitbox needs a group, so the last group can't go while a hitbox is in it
    public bool CanRemoveHitGroup(string stateName, int hitGroupId)
    {
        StateFile state = GetState(stateName);
        if (state.HitGroups.Count > 1) return true;
        foreach (var frame in state.Frames)
        {
            foreach (var box in frame.Boxes)
            {
                if (box.Type == BoxType.Hitbox && box.HitGroup == hitGroupId) return false;
            }
        }
        return true;
    }

    // Its hitboxes move to the first group left
    public void RemoveHitGroup(string stateName, int hitGroupId)
    {
        if (!CanRemoveHitGroup(stateName, hitGroupId)) return;
        StateFile state = GetState(stateName);
        state.HitGroups.Remove(GetHitGroup(stateName, hitGroupId));
        int fallback = state.HitGroups.Count > 0 ? state.HitGroups[0].Id : BoxFile.NoHitGroup;
        foreach (var frame in state.Frames)
        {
            foreach (var box in frame.Boxes)
            {
                if (box.HitGroup == hitGroupId) box.HitGroup = fallback;
            }
        }
        MarkChanged();
    }

    private static int DefaultHitGroup(StateFile state) => state.HitGroups.Count > 0 ? state.HitGroups[0].Id : NewHitGroup(state).Id;

    private static HitGroupFile NewHitGroup(StateFile state)
    {
        int id = state.NextHitGroupId++;
        var group = new HitGroupFile { Id = id, Name = $"Hit {id}" };
        state.HitGroups.Add(group);
        return group;
    }

    // ---------- Helpers ----------

    private void MarkChanged()
    {
        definition = null;
        HasUnsavedChanges = true;
        Changed?.Invoke();
    }
}

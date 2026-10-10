using System;
using System.Collections.Generic;

// Back end of the hitbox editor. Holds one character's files and is the only thing that edits them,
// so the UI calls these methods instead of touching the file classes directly.
// States and boxes are addressed by state name and box id. A missing one is a caller bug and throws.
// Plain C#, owned by the HitboxEditor
public class HitboxEditorManager
{
    private CharacterId characterId;

    public CharacterId CharacterId => characterId;
    public CharacterFile Loaded { get; private set; }
    // Null when the character has no bake, as with sprite characters
    public BakedSkeletonFile Skeleton { get; private set; }
    public bool HasUnsavedChanges { get; private set; }

    // Raised after every edit and load, so the UI and preview can redraw
    public event Action Changed;

    // Rebuilt lazily after edits, so the preview draws exactly what play would use
    private CharacterDefinition definition;
    public CharacterDefinition Definition => definition ??= CharacterDefinitionBuilder.Build(Loaded, Skeleton);

    // Every state the game has, whether or not it has data yet
    public static string[] AllStateNames => Enum.GetNames(typeof(CharacterStateId));

    // ---------- Files ----------

    // A character with no file yet starts empty, and gets one on the first save
    public void LoadCharacter(CharacterId id)
    {
        characterId = id;
        Loaded = CharacterFiles.ReadCharacter(id);
        Skeleton = CharacterFiles.ReadSkeleton(id);
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

    // ---------- States ----------

    public bool HasState(string stateName) => Loaded.States.ContainsKey(stateName);

    public StateFile GetState(string stateName)
    {
        if (!Loaded.States.TryGetValue(stateName, out StateFile state))
        {
            throw new ArgumentException($"No state named '{stateName}'");
        }
        return state;
    }

    public StateFile AddState(string stateName)
    {
        if (HasState(stateName)) return GetState(stateName);
        // Starts as long as the game ran the state before it had data
        var state = new StateFile { Length = Roster.Get(characterId).DefaultStateLength };
        Loaded.States[stateName] = state;
        MarkChanged();
        return state;
    }

    public void RemoveState(string stateName)
    {
        if (Loaded.States.Remove(stateName)) MarkChanged();
    }

    // Keys and active frames past the new end are kept, so shortening by mistake loses nothing.
    // The runtime build ignores them. Active ranges that ran to the old end are stretched to the new one
    public void SetStateLength(string stateName, int length)
    {
        StateFile state = GetState(stateName);
        int oldEnd = state.Length - 1;
        state.Length = Math.Max(1, length);
        if (state.Length - 1 > oldEnd)
        {
            foreach (var box in state.Boxes)
            {
                for (int i = 0; i < box.Active.Count; i++)
                {
                    if (box.Active[i].End == oldEnd) box.Active[i] = new FrameRange(box.Active[i].Start, state.Length - 1);
                }
            }
        }
        MarkChanged();
    }

    public void SetStateLoop(string stateName, bool loop)
    {
        GetState(stateName).Loop = loop;
        MarkChanged();
    }

    public void SetStateAnimation(string stateName, string animation)
    {
        GetState(stateName).Animation = animation ?? "";
        MarkChanged();
    }

    // ---------- Boxes ----------

    public BoxFile GetBox(string stateName, int boxId)
    {
        StateFile state = GetState(stateName);
        foreach (var box in state.Boxes)
        {
            if (box.Id == boxId) return box;
        }
        throw new ArgumentException($"State '{stateName}' has no box {boxId}");
    }

    // Starts with one key at frame 0 and inactive, so it does nothing until its frames are picked
    public BoxFile AddBox(string stateName, BoxType type, string bone = "")
    {
        StateFile state = GetState(stateName);
        int id = state.NextBoxId++;
        var box = new BoxFile
        {
            Id = id,
            Name = $"{type} {id}",
            Type = type,
            Bone = bone ?? "",
        };
        box.Keys.Add(new BoxKey { Frame = 0, Size = new DMVector(1, 1) });
        state.Boxes.Add(box);
        MarkChanged();
        return box;
    }

    public void RemoveBox(string stateName, int boxId)
    {
        GetState(stateName).Boxes.Remove(GetBox(stateName, boxId));
        MarkChanged();
    }

    public void RenameBox(string stateName, int boxId, string name)
    {
        GetBox(stateName, boxId).Name = name ?? "";
        MarkChanged();
    }

    // A box still on its default name is renamed to match, so the name never lies about the type
    public void SetBoxType(string stateName, int boxId, BoxType type)
    {
        BoxFile box = GetBox(stateName, boxId);
        if (box.Name == $"{box.Type} {box.Id}") box.Name = $"{type} {box.Id}";
        box.Type = type;
        MarkChanged();
    }

    // Empty bone means the root. With keepPosition, each key's offset is adjusted so the box stays where it was
    // on that key's frame. Blended frames between keys can still shift, since the two bones move differently
    public void SetParent(string stateName, int boxId, string bone, bool keepPosition = true)
    {
        StateFile state = GetState(stateName);
        BoxFile box = GetBox(stateName, boxId);
        bone ??= "";
        if (keepPosition)
        {
            for (int i = 0; i < box.Keys.Count; i++)
            {
                BoxKey key = box.Keys[i];
                key.Offset += BonePosition(state, box.Bone, key.Frame) - BonePosition(state, bone, key.Frame);
                box.Keys[i] = key;
            }
        }
        box.Bone = bone;
        MarkChanged();
    }

    // Bone names baked for the state's animation, for the parent picker
    public IEnumerable<string> GetBones(string stateName)
    {
        BakedAnimation animation = GetAnimation(GetState(stateName));
        return animation != null ? (IEnumerable<string>)animation.Bones.Keys : Array.Empty<string>();
    }

    // ---------- Keys ----------

    // Values the box has on a frame, blended from its keys, relative to its bone
    public void SampleBox(string stateName, int boxId, int frame, out DMVector offset, out DMVector size)
    {
        BoxFile box = GetBox(stateName, boxId);
        if (box.Keys.Count == 0)
        {
            offset = DMVector.zero;
            size = DMVector.zero;
            return;
        }
        BoxKeyMath.Sample(box.Keys, frame, out offset, out size);
    }

    public bool HasKey(string stateName, int boxId, int frame) => BoxKeyMath.IndexOf(GetBox(stateName, boxId).Keys, frame) >= 0;

    // Creates a key on the frame if there is none, so the other frames keep their values
    public void SetPosition(string stateName, int boxId, int frame, DMVector offset)
    {
        EditKey(stateName, boxId, frame, key => { key.Offset = offset; return key; });
    }

    public void SetSize(string stateName, int boxId, int frame, DMVector size)
    {
        EditKey(stateName, boxId, frame, key => { key.Size = size; return key; });
    }

    // How this frame's key blends into the next key
    public void SetInterpolation(string stateName, int boxId, int frame, KeyInterpolation interpolation)
    {
        EditKey(stateName, boxId, frame, key => { key.Interpolation = interpolation; return key; });
    }

    // The last key can not be removed, since a box needs at least one
    public void RemoveKey(string stateName, int boxId, int frame)
    {
        BoxFile box = GetBox(stateName, boxId);
        int index = BoxKeyMath.IndexOf(box.Keys, frame);
        if (index < 0 || box.Keys.Count == 1) return;
        box.Keys.RemoveAt(index);
        MarkChanged();
    }

    private void EditKey(string stateName, int boxId, int frame, Func<BoxKey, BoxKey> edit)
    {
        ValidateFrame(stateName, frame);
        BoxFile box = GetBox(stateName, boxId);
        int index = BoxKeyMath.IndexOf(box.Keys, frame);
        if (index >= 0)
        {
            box.Keys[index] = edit(box.Keys[index]);
        }
        else
        {
            // New keys start from the values the box already had on this frame,
            // and blend the same way as the key they split
            var key = new BoxKey { Frame = frame, Size = new DMVector(1, 1) };
            if (box.Keys.Count > 0)
            {
                BoxKeyMath.Sample(box.Keys, frame, out key.Offset, out key.Size);
                key.Interpolation = box.Keys[BoxKeyMath.IndexAtOrBefore(box.Keys, frame)].Interpolation;
            }
            box.Keys.Add(edit(key));
            BoxKeyMath.Sort(box.Keys);
        }
        MarkChanged();
    }

    // ---------- Active frames ----------

    // One entry per frame of the state
    public bool[] GetActiveFrames(string stateName, int boxId)
    {
        var active = new bool[GetState(stateName).Length];
        foreach (var range in GetBox(stateName, boxId).Active)
        {
            int start = Math.Max(0, range.Start);
            int end = Math.Min(active.Length - 1, range.End);
            for (int frame = start; frame <= end; frame++)
            {
                active[frame] = true;
            }
        }
        return active;
    }

    // Replaces every range, one entry per frame
    public void SetActiveFrames(string stateName, int boxId, bool[] active)
    {
        BoxFile box = GetBox(stateName, boxId);
        box.Active.Clear();
        int start = -1;
        for (int frame = 0; frame <= active.Length; frame++)
        {
            bool on = frame < active.Length && active[frame];
            if (on && start < 0)
            {
                start = frame;
            }
            else if (!on && start >= 0)
            {
                box.Active.Add(new FrameRange(start, frame - 1));
                start = -1;
            }
        }
        MarkChanged();
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
        StateFile state = GetState(stateName);
        int id = state.NextHitGroupId++;
        var group = new HitGroupFile { Id = id, Name = $"Hit {id}" };
        state.HitGroups.Add(group);
        MarkChanged();
        return group;
    }

    // Boxes in the group are left without one
    public void RemoveHitGroup(string stateName, int hitGroupId)
    {
        StateFile state = GetState(stateName);
        state.HitGroups.Remove(GetHitGroup(stateName, hitGroupId));
        foreach (var box in state.Boxes)
        {
            if (box.HitGroup == hitGroupId) box.HitGroup = BoxFile.NoHitGroup;
        }
        MarkChanged();
    }

    // What a hitbox deals
    public void SetBoxHit(string stateName, int boxId, int damage, int hitstun, DMVector knockback)
    {
        BoxFile box = GetBox(stateName, boxId);
        box.Damage = damage;
        box.Hitstun = hitstun;
        box.Knockback = knockback;
        MarkChanged();
    }

    // Pass BoxFile.NoHitGroup to clear it
    public void SetBoxHitGroup(string stateName, int boxId, int hitGroupId)
    {
        if (hitGroupId != BoxFile.NoHitGroup) GetHitGroup(stateName, hitGroupId);
        GetBox(stateName, boxId).HitGroup = hitGroupId;
        MarkChanged();
    }

    // ---------- Helpers ----------

    private void MarkChanged()
    {
        definition = null;
        HasUnsavedChanges = true;
        Changed?.Invoke();
    }

    private void ValidateFrame(string stateName, int frame)
    {
        int length = GetState(stateName).Length;
        if (frame < 0 || frame >= length)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), $"Frame {frame} is outside '{stateName}', which is {length} frames long");
        }
    }

    private BakedAnimation GetAnimation(StateFile state)
    {
        if (Skeleton == null) return null;
        Skeleton.Animations.TryGetValue(state.Animation ?? "", out BakedAnimation animation);
        return animation;
    }

    // Zero for the root, or for a bone with no bake
    private DMVector BonePosition(StateFile state, string bone, int frame)
    {
        if (string.IsNullOrEmpty(bone)) return DMVector.zero;
        BakedAnimation animation = GetAnimation(state);
        if (animation == null || !animation.Bones.TryGetValue(bone, out DMVector[] positions) || positions.Length == 0)
        {
            return DMVector.zero;
        }
        return positions[Math.Min(frame, positions.Length - 1)];
    }
}

using System.Collections.Generic;
using System;
using Godot;

// Turns the authored and baked files into a CharacterDefinition.
// Keyframes are expanded here with DM64 math, so interpolation stays deterministic.
public static class CharacterDefinitionBuilder
{
    // skeleton is null for sprite characters
    public static CharacterDefinition Build(CharacterFile file, BakedSkeletonFile skeleton = null)
    {
        var states = new List<StateDefinition>(file.States.Count);
        foreach (var entry in file.States)
        {
            states.Add(BuildState(entry.Key, entry.Value, skeleton));
        }
        return new CharacterDefinition(states.ToArray());
    }

    private static StateDefinition BuildState(string name, StateFile state, BakedSkeletonFile skeleton)
    {
        int length = Math.Max(1, state.Length);
        BakedAnimation animation = null;
        skeleton?.Animations.TryGetValue(state.Animation ?? "", out animation);

        // Groups only exist to share hit memory, so all that's kept is each one's index
        var hitGroupIndexById = new Dictionary<int, int>();
        for (int i = 0; i < state.HitGroups.Count; i++)
        {
            hitGroupIndexById[state.HitGroups[i].Id] = i;
        }
        // A hitbox without a group is a group of its own, numbered after the authored ones
        int nextOwnGroupIndex = state.HitGroups.Count;

        var boxes = new BoxDefinition[state.Boxes.Count];
        for (int i = 0; i < boxes.Length; i++)
        {
            BoxFile box = state.Boxes[i];
            int hitGroupIndex = -1;
            if (box.HitGroup != BoxFile.NoHitGroup && !hitGroupIndexById.TryGetValue(box.HitGroup, out hitGroupIndex))
            {
                GD.PushWarning($"{name} box {box.Id} uses missing hit group {box.HitGroup}, so it hits on its own");
                hitGroupIndex = -1;
            }
            if (box.Type == BoxType.Hitbox && hitGroupIndex < 0)
            {
                hitGroupIndex = nextOwnGroupIndex++;
            }

            ExpandKeys(box, length, out DMVector[] offsets, out DMVector[] sizes, $"{name} box {box.Id}");
            boxes[i] = new BoxDefinition(
                box.Id,
                box.Type,
                hitGroupIndex,
                box.Type == BoxType.Hitbox ? new HitDefinition(box.Damage, box.Hitstun, box.Knockback) : null,
                offsets,
                sizes,
                ExpandActive(box.Active, length),
                FindBoneTrack(animation, box.Bone, $"{name} box {box.Id}"));
        }

        return new StateDefinition(name, state.Animation, length, state.Loop, boxes);
    }

    private static void ExpandKeys(BoxFile box, int length, out DMVector[] offsets, out DMVector[] sizes, string context)
    {
        offsets = new DMVector[length];
        sizes = new DMVector[length];
        if (box.Keys.Count == 0)
        {
            GD.PushWarning($"{context} has no keys");
            return;
        }

        // Sorted copy, since hand edited files may be out of order
        var keys = new List<BoxKey>(box.Keys);
        BoxKeyMath.Sort(keys);
        for (int frame = 0; frame < length; frame++)
        {
            BoxKeyMath.Sample(keys, frame, out offsets[frame], out sizes[frame]);
        }
    }

    private static bool[] ExpandActive(List<FrameRange> ranges, int length)
    {
        var active = new bool[length];
        foreach (var range in ranges)
        {
            int start = Math.Max(0, range.Start);
            int end = Math.Min(length - 1, range.End);
            for (int frame = start; frame <= end; frame++)
            {
                active[frame] = true;
            }
        }
        return active;
    }

    // Null means the box follows the root
    private static DMVector[] FindBoneTrack(BakedAnimation animation, string bone, string context)
    {
        if (string.IsNullOrEmpty(bone)) return null;
        if (animation != null && animation.Bones.TryGetValue(bone, out DMVector[] positions) && positions.Length > 0)
        {
            return positions;
        }
        GD.PushWarning($"{context} follows bone '{bone}', which has no baked track. Following the root instead");
        return null;
    }
}

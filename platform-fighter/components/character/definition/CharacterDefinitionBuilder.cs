using System.Collections.Generic;
using System;
using Godot;

// Turns the authored and baked files into a CharacterDefinition.
// Keyframes are expanded here with DM64 math, so interpolation stays deterministic.
// Boxes are converted from their authored units (pixels for sprites) to world units here, so play never sees pixels.
public static class CharacterDefinitionBuilder
{
    // boxUnit is world units per authored box unit, see VisualData.BoxUnit. skeleton is null for sprite characters
    public static CharacterDefinition Build(CharacterFile file, DM64 boxUnit, BakedSkeletonFile skeleton = null)
    {
        var states = new List<StateDefinition>(file.States.Count);
        foreach (var entry in file.States)
        {
            states.Add(BuildState(entry.Key, entry.Value, boxUnit, skeleton));
        }
        return new CharacterDefinition(states.ToArray(), BuildAttacks(file, boxUnit, skeleton));
    }

    // Sorted by name with ordinal compare, so the indexes rollback stores are the same on every machine.
    // An attack that can't be told apart from another is left out with an error, so one bad entry doesn't stop the load
    private static AttackDefinition[] BuildAttacks(CharacterFile file, DM64 boxUnit, BakedSkeletonFile skeleton)
    {
        var names = new List<string>(file.Attacks.Keys);
        names.Sort(StringComparer.Ordinal);

        var attacks = new List<AttackDefinition>(names.Count);
        var nameByInput = new Dictionary<AttackInput, string>();
        foreach (string name in names)
        {
            AttackFile attack = file.Attacks[name];
            // A state without data still plays the sheet named after it, so every state name is taken
            if (file.States.ContainsKey(name) || Enum.IsDefined(typeof(CharacterStateId), name))
            {
                GD.PushError($"Attack {name} has the same name as a state, so it's left out");
                continue;
            }
            if (nameByInput.TryGetValue(attack.Input, out string other))
            {
                GD.PushError($"Attacks {other} and {name} both start on {attack.Input}, so {name} is left out");
                continue;
            }
            nameByInput[attack.Input] = name;
            StateDefinition state = BuildState(name, attack, boxUnit, skeleton);
            attacks.Add(new AttackDefinition(attacks.Count, attack.Input, attack.Movement, state));
        }
        return attacks.ToArray();
    }

    private static StateDefinition BuildState(string name, StateFile state, DM64 boxUnit, BakedSkeletonFile skeleton)
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

            ExpandKeys(box, length, boxUnit, out DMVector[] offsets, out DMVector[] sizes, $"{name} box {box.Id}");
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

        return new StateDefinition(name, state.Animation, length, state.Loop, boxes, BuildSprite(name, state.Sprite, length));
    }

    // Null when there's no sequence to play
    private static SpriteTrack BuildSprite(string name, SpriteFile file, int length)
    {
        if (file == null || file.Sequence.Count == 0) return null;
        int columns = Math.Max(1, file.Columns);
        int rows = Math.Max(1, file.Rows);
        int spriteCount = columns * rows;

        // The editor keeps the length at least this long, so only a hand edited file gets here
        if (length < file.Sequence.Count)
        {
            GD.PushWarning($"{name} is {length} ticks long, so some of its {file.Sequence.Count} sprites never show");
        }

        var sequence = new int[file.Sequence.Count];
        bool warned = false;
        for (int i = 0; i < sequence.Length; i++)
        {
            int sprite = file.Sequence[i];
            // The grid may have been made smaller after the sequence was set
            if ((sprite < 0 || sprite >= spriteCount) && !warned)
            {
                GD.PushWarning($"{name} plays sprite {sprite}, but its {columns}x{rows} sheet only has {spriteCount}");
                warned = true;
            }
            sequence[i] = Math.Clamp(sprite, 0, spriteCount - 1);
        }
        return new SpriteTrack(columns, rows, sequence);
    }

    private static void ExpandKeys(BoxFile box, int length, DM64 boxUnit, out DMVector[] offsets, out DMVector[] sizes, string context)
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
            // Scaled after blending, which gives the same result since blending is linear
            offsets[frame] *= boxUnit;
            sizes[frame] *= boxUnit;
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

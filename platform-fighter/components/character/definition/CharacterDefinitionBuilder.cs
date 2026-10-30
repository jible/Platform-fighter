using System.Collections.Generic;
using System;
using Godot;

// Turns the authored file into a CharacterDefinition.
// Boxes are converted from their authored units (pixels for sprites) to world units here, so play never sees pixels.
// Anything a hand edited file gets wrong is fixed here with a warning, so one bad entry doesn't stop the load
public static class CharacterDefinitionBuilder
{
    // boxUnit is world units per authored box unit, see VisualData.BoxUnit
    public static CharacterDefinition Build(CharacterFile file, DM64 boxUnit)
    {
        var states = new List<StateDefinition>(file.States.Count);
        foreach (var entry in file.States)
        {
            states.Add(BuildState(entry.Key, entry.Value, boxUnit));
        }
        return new CharacterDefinition(states.ToArray(), BuildAttacks(file, boxUnit));
    }

    // Sorted by name with ordinal compare, so the indexes rollback stores are the same on every machine.
    // An attack that can't be told apart from another is left out with an error
    private static AttackDefinition[] BuildAttacks(CharacterFile file, DM64 boxUnit)
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
            StateDefinition state = BuildState(name, attack, boxUnit);
            attacks.Add(new AttackDefinition(attacks.Count, attack.Input, attack.Movement, attack.CancelOnLand, state));
        }
        return attacks.ToArray();
    }

    private static StateDefinition BuildState(string name, StateFile state, DM64 boxUnit)
    {
        SpriteFile sprite = state.Sprite ?? new SpriteFile();
        var grid = new SpriteGrid(Math.Max(1, sprite.Columns), Math.Max(1, sprite.Rows));

        // Groups only exist to share hit memory, so all that's kept is each one's index
        var hitGroupIndexById = new Dictionary<int, int>();
        for (int i = 0; i < state.HitGroups.Count; i++)
        {
            hitGroupIndexById[state.HitGroups[i].Id] = i;
        }
        // A hitbox with a missing group hits on its own, numbered after the authored ones
        int hitGroupCount = state.HitGroups.Count;

        // Sprites past the end of the frame list have no boxes. Frames past the grid are kept in the file but unused
        var frames = new BoxDefinition[grid.Count][];
        for (int index = 0; index < frames.Length; index++)
        {
            List<BoxFile> boxes = index < state.Frames.Count ? state.Frames[index].Boxes : null;
            frames[index] = new BoxDefinition[boxes?.Count ?? 0];
            for (int i = 0; i < frames[index].Length; i++)
            {
                BoxFile box = boxes[i];
                int hitGroupIndex = -1;
                if (box.Type == BoxType.Hitbox && !hitGroupIndexById.TryGetValue(box.HitGroup, out hitGroupIndex))
                {
                    GD.PushWarning($"{name} sprite {index} box {box.Id} has no hit group, so it hits on its own");
                    hitGroupIndex = hitGroupCount++;
                }
                frames[index][i] = new BoxDefinition(
                    box.Id,
                    box.Type,
                    hitGroupIndex,
                    box.Type == BoxType.Hitbox ? new HitDefinition(box.Damage, box.Knockback, box.KnockbackScaling) : null,
                    box.Offset * boxUnit,
                    box.Size * boxUnit);
            }
        }

        return new StateDefinition(name, grid, BuildPhases(name, state.Phases, grid.Count), frames, hitGroupCount, state.CancelInAir);
    }

    private static PhaseDefinition[] BuildPhases(string name, List<PhaseFile> files, int spriteCount)
    {
        if (files.Count == 0)
        {
            GD.PushWarning($"{name} has no phases, so it plays its whole sheet once, a tick per sprite");
            return new[] { new PhaseDefinition(0, spriteCount, spriteCount, PhaseDefinition.EndsState) };
        }

        var indexById = new Dictionary<int, int>();
        for (int i = 0; i < files.Count; i++)
        {
            indexById[files[i].Id] = i;
        }

        var phases = new PhaseDefinition[files.Count];
        for (int i = 0; i < files.Count; i++)
        {
            PhaseFile file = files[i];
            int start = Math.Clamp(file.Start, 0, spriteCount - 1);
            int end = Math.Clamp(file.End, start, spriteCount - 1);
            if (start != file.Start || end != file.End)
            {
                GD.PushWarning($"{name} phase {file.Name} plays sprites {file.Start}-{file.End}, but the sheet only has {spriteCount}");
            }
            int count = end - start + 1;
            int length = Math.Max(1, file.Length);
            // The editor keeps the length at least this long, so only a hand edited file gets here
            if (length < count)
            {
                GD.PushWarning($"{name} phase {file.Name} is {length} ticks long, so some of its {count} sprites never show");
            }

            int next = i + 1 < files.Count ? i + 1 : PhaseDefinition.EndsState;
            switch (file.OnEnd)
            {
                case PhaseEnd.Restart:
                    next = i;
                    break;
                case PhaseEnd.First:
                    next = 0;
                    break;
                case PhaseEnd.Phase:
                    if (indexById.TryGetValue(file.Target, out int target)) next = target;
                    else GD.PushWarning($"{name} phase {file.Name} goes to missing phase {file.Target}, so it goes to the next instead");
                    break;
            }
            phases[i] = new PhaseDefinition(start, count, length, next);
        }
        return phases;
    }
}

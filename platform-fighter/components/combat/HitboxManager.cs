using System.Collections.Generic;

// Resolves hits between characters each tick, from the boxes in their CharacterDefinitions.
// Separate from the physics server: boxes are frame data, not physics objects, and only share its overlap math.
// Ticked right after physics, so it sees every character's final position for the tick.
//
// Gathers every active hit and hurt box once, then checks each hitbox against every other owner's hurtboxes.
// Everything runs in player number order, then box order, so every machine resolves the same hits.
// All hits are found before any are applied, so two characters hitting each other on the same tick both land.
// Holds no state between ticks. What each hit group has already hit lives in the attacker's HitGroups, so it rolls back
// A world system, added by the PlayManager, since the world itself knows nothing about characters
public class HitboxManager : IWorldSystem
{
    private struct PendingHit
    {
        public Character Attacker;
        public Character Target;
        public HitDefinition Hit;
    }

    private struct PendingMark
    {
        public HitGroup Group;
        public Character Target;
    }

    // In player number order
    private readonly Character[] characters;
    // Only live for the duration of a tick, kept to avoid allocating every tick.
    // The box lists hold the last tick's boxes until the next one, so they can be drawn between ticks
    private readonly List<Box> hitboxes = new();
    private readonly List<Box> hurtboxes = new();
    private readonly List<PendingHit> hits = new();
    private readonly List<PendingMark> marks = new();

    public IReadOnlyList<Box> Hitboxes => hitboxes;
    public IReadOnlyList<Box> Hurtboxes => hurtboxes;

    public HitboxManager(Character[] characters)
    {
        this.characters = characters;
    }

    public void Tick()
    {
        // A new state starts with nothing hit. TicksInState is only 0 here on the tick a state was entered
        foreach (var character in characters)
        {
            if (character.StateMachine.TicksInState == 0)
            {
                foreach (var group in character.HitGroups)
                {
                    group.Clear();
                }
            }
        }

        hitboxes.Clear();
        hurtboxes.Clear();
        foreach (var character in characters)
        {
            GatherBoxes(character);
        }

        hits.Clear();
        marks.Clear();
        foreach (var hitbox in hitboxes)
        {
            FindHits(hitbox);
        }

        // Marked only after every box is checked, so a group's first overlapping box can't
        // shut out a better box later in the same group, like a sour spot listed before the sweet spot
        foreach (var mark in marks)
        {
            mark.Group.MarkHit(mark.Target);
        }

        foreach (var hit in hits)
        {
            hit.Target.ReceiveHit(hit.Attacker, hit.Hit);
        }
    }

    private void GatherBoxes(Character character)
    {
        StateDefinition state = character.StateMachine.CurrentDefinition;
        if (state == null) return;
        DMVector root = character.SimTransform.GlobalPosition;

        // Only the showing sprite's boxes exist, so a phase cut short takes its hitboxes with it
        foreach (var box in state.BoxesAt(character.StateMachine.CurrentSprite))
        {
            DMVector center = root + box.CenterFacing(character.direction);
            DMVector size = box.Size;

            if (box.Type == BoxType.Hurtbox)
            {
                hurtboxes.Add(Box.Hurtbox(character, center, size));
            }
            else if (box.Type == BoxType.Hitbox)
            {
                hitboxes.Add(Box.Hitbox(character, center, size, character.HitGroups[box.HitGroupIndex], box.Hit));
            }
        }
    }

    private void FindHits(Box hitbox)
    {
        foreach (var hurtbox in hurtboxes)
        {
            Character target = hurtbox.Owner;
            if (target == hitbox.Owner) continue;
            // A group hits a target once. This only sees earlier ticks, since this tick's marks are applied after
            if (hitbox.Group.HasHit(target)) continue;
            if (!hitbox.Overlaps(hurtbox)) continue;

            // Every overlapping group is marked, even if another box's hit wins below, so one move connects once
            marks.Add(new PendingMark { Group = hitbox.Group, Target = target });
            AddHit(new PendingHit { Attacker = hitbox.Owner, Target = target, Hit = hitbox.Hit });
        }
    }

    // An attacker lands at most one hit on a target per tick, from whichever overlapping hitbox deals the most damage,
    // in the same group or not. So a sweet spot beats the sour spot around it.
    // Hitboxes are checked in the state's box order, so on a tie the first one found stays.
    // Prototype rule, to revisit with priorities or clanks
    private void AddHit(PendingHit hit)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].Attacker != hit.Attacker || hits[i].Target != hit.Target) continue;

            if (hit.Hit.Damage > hits[i].Hit.Damage) hits[i] = hit;
            return;
        }
        hits.Add(hit);
    }
}

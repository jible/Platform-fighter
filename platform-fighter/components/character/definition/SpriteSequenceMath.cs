using System;

// Sprite sequence timing shared by the runtime and the editor, so both always agree on which entry shows.
// The entries are spread evenly over however many ticks the animation plays for, so when the ticks don't
// divide evenly some entries show a tick longer. Integer math only, so it stays deterministic
public static class SpriteSequenceMath
{
    // Index into the sequence shown on the tick. count must be above 0, and tick is within 0..duration - 1.
    // A duration shorter than the sequence skips entries, which the editor never authors but a caller might ask for
    public static int EntryAt(int tick, int count, int duration)
    {
        if (duration <= 0) return 0;
        return Math.Clamp(tick * count / duration, 0, count - 1);
    }

    // First tick the entry shows on. The inverse of EntryAt, so EntryStart(count, ...) is the duration
    public static int EntryStart(int entry, int count, int duration) => (entry * duration + count - 1) / count;
}

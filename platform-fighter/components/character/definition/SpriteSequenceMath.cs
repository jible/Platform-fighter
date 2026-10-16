using System;

// Sprite sequence timing shared by the runtime build and the editor, so both always agree on which entry shows.
// Integer math only, so it stays deterministic. Frame rates are clamped to 1..TicksPerSecond,
// so the position never skips an entry and grows by at most one a tick
public static class SpriteSequenceMath
{
    public static int ClampFrameRate(int frameRate) => Math.Clamp(frameRate, 1, TickManager.TicksPerSecond);

    // Entries shown up to and including the tick, minus one, counting every pass of a loop.
    // Without a loop it holds on the last entry
    public static int PositionAt(int tick, int frameRate, int count, bool loop)
    {
        int position = tick * ClampFrameRate(frameRate) / TickManager.TicksPerSecond;
        return loop ? position : Math.Min(position, count - 1);
    }

    // Index into the sequence shown on the tick. count must be above 0
    public static int EntryAt(int tick, int frameRate, int count, bool loop) => PositionAt(tick, frameRate, count, loop) % count;

    // First tick an entry shows on, in the first pass. The inverse of PositionAt
    public static int EntryStart(int entry, int frameRate)
    {
        int rate = ClampFrameRate(frameRate);
        return (entry * TickManager.TicksPerSecond + rate - 1) / rate;
    }

    // Ticks one pass through the sequence takes
    public static int Duration(int count, int frameRate) => EntryStart(count, frameRate);
}

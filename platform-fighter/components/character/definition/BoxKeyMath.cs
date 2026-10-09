using System.Collections.Generic;

// Keyframe sampling shared by the runtime build and the editor, so both always agree on a box's values.
// All math is DM64, so it stays deterministic.
public static class BoxKeyMath
{
    public static void Sort(List<BoxKey> keys) => keys.Sort((a, b) => a.Frame.CompareTo(b.Frame));

    // keys must be sorted and not empty. Frames before the first key hold the first key
    public static void Sample(List<BoxKey> keys, int frame, out DMVector offset, out DMVector size)
    {
        int current = IndexAtOrBefore(keys, frame);
        BoxKey key = keys[current];
        bool blend = key.Interpolation == KeyInterpolation.Linear
            && current + 1 < keys.Count
            && frame > key.Frame;
        if (!blend)
        {
            offset = key.Offset;
            size = key.Size;
            return;
        }

        BoxKey next = keys[current + 1];
        DM64 t = new DM64(frame - key.Frame) / new DM64(next.Frame - key.Frame);
        offset = Lerp(key.Offset, next.Offset, t);
        size = Lerp(key.Size, next.Size, t);
    }

    // The last key at or before frame, or the first key if frame is before all of them
    public static int IndexAtOrBefore(List<BoxKey> keys, int frame)
    {
        int index = 0;
        while (index + 1 < keys.Count && keys[index + 1].Frame <= frame)
        {
            index++;
        }
        return index;
    }

    // -1 when no key sits exactly on frame
    public static int IndexOf(List<BoxKey> keys, int frame)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].Frame == frame) return i;
        }
        return -1;
    }

    private static DMVector Lerp(DMVector a, DMVector b, DM64 t) => a + (b - a) * t;
}

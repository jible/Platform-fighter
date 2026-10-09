using System;
using System.Collections.Generic;

// The generated half of a character's data: bone positions sampled from the model's animations, once per tick.
// Kept in its own file since a re-bake replaces it wholesale, while CharacterFile is edited by hand.
// Sprite characters have no skeleton and can skip this entirely.

[Serializable]
public class BakedSkeletonFile
{
    public int Version = 1;
    // Keyed by animation name, matching StateFile.Animation
    public Dictionary<string, BakedAnimation> Animations = new();
}

[Serializable]
public class BakedAnimation
{
    // Hash of the source clip when baked, so the editor can tell the bake is stale
    public string SourceHash = "";
    // Ticks sampled
    public int Length;
    // Keyed by bone name. One position per tick, relative to the character root, facing right
    public Dictionary<string, DMVector[]> Bones = new();
}

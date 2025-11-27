using Godot;
using System;

[GlobalClass]
public partial class dm64_editor_wrapper : GodotObject
{
    static int SHIFT = 32;
	static long SCALE = 1L << SHIFT;
    public long raw = 0;

    [Export]
    public float EditorValue
    {
        get {return ((float)raw) / SCALE; }
        set {raw = (long)(value * SCALE);}
    }

    public float ToFloat()
	{
		float output = ((float)raw) / ((float)SCALE);
		return output;
	}
}

using Godot;
using System;

[Tool]
[GlobalClass]
public partial class DM_Vector : Godot.Resource
{
    [Export] public DM64 x = new();
    [Export] public DM64 y = new();
    
    // Constructors
    public DM_Vector() { }
    public DM_Vector(DM64 x, DM64 y) { this.x = x; this.y = y; }
    public DM_Vector(int x, int y) { this.x = new DM64(x); this.y = new DM64(y); }
    public DM_Vector(float x, float y) { this.x = new DM64(x); this.y = new DM64(y); }
    public DM_Vector(Vector2 v) { this.x = new DM64(v.X); this.y = new DM64(v.Y); }
    
    // Methods
    public DM_Vector copy() => new DM_Vector(x.copy(), y.copy());
    public Vector2 ToStandardVector() => new Vector2(x.ToFloat(), y.ToFloat());
    public Vector2I ToVector2I() => new Vector2I(x.to_int(), y.to_int());
    
    // Operators
    public static DM_Vector operator +(DM_Vector a, DM_Vector b) => new DM_Vector(a.x + b.x, a.y + b.y);
    public static DM_Vector operator -(DM_Vector a, DM_Vector b) => new DM_Vector(a.x - b.x, a.y - b.y);
    public static DM_Vector operator *(DM_Vector a, DM_Vector b) => new DM_Vector(a.x * b.x, a.y * b.y);
    public static DM_Vector operator /(DM_Vector a, DM_Vector b) => new DM_Vector(a.x / b.x, a.y / b.y);
    public static DM_Vector operator *(DM_Vector a, DM64 b) => new DM_Vector(a.x * b, a.y * b);
    public static DM_Vector operator /(DM_Vector a, DM64 b) => new DM_Vector(a.x / b, a.y / b);
    public static DM_Vector operator *(DM64 a, DM_Vector b) => new DM_Vector(a * b.x, a * b.y);
    public static DM_Vector operator *(DM_Vector a, int b) => new DM_Vector(a.x * b, a.y * b);
    public static DM_Vector operator /(DM_Vector a, int b) => new DM_Vector(a.x / b, a.y / b);
    
    public DM64 GetMagnitude() => (x.Pow(2) + y.Pow(2)).Sqrt();
    public DM_Vector Normalized()
    {
        DM64 mag = GetMagnitude();
        if (mag == 0) return new DM_Vector(0, 0);
        return this / mag;
    }
    
    public override bool Equals(object obj) => obj is DM_Vector other && x == other.x && y == other.y;
    public override int GetHashCode() => HashCode.Combine(x.GetHashCode(), y.GetHashCode());
}
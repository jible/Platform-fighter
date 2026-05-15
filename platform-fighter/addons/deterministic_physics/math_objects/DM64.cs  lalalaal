using Godot;
using System;

[Tool]
[GlobalClass]
public partial class DM64 : Godot.Resource
{
    // The actual math struct
    private DM_Base _value;
    
    // Constructor forwarding
    public DM64() => _value = new DM_Base();
    public DM64(int value) => _value = new DM_Base(value);
    public DM64(long value) => _value = new DM_Base(value);
    public DM64(float value) => _value = new DM_Base(value);
    public DM64(string value) => _value = new DM_Base(value);
    
    // Export for serialization (store the raw value)
    [Export]
    public long RawValue
    {
        get => _value.raw;
        set => _value = DM_Base.FromRaw(value);
    }
    
    // For convenience in editor
    [Export]
    public float EditorValue
    {
        get => _value.ToFloat();
        set => _value = new DM_Base(value);
    }
    
    // Forward all properties and methods
    public long raw { get => _value.raw; set => _value.raw = value; }
    public float ToFloat() => _value.ToFloat();
    public long to_long() => _value.to_long();
    public int to_int() => _value.to_int();
    public DM64 copy() => new DM64 { _value = _value.copy() };
    public DM64 Sign() => new DM64 { _value = _value.Sign() };
    public DM64 Abs() => new DM64 { _value = _value.Abs() };
    public DM64 Round() => new DM64 { _value = _value.Round() };
    public DM64 Ceil() => new DM64 { _value = _value.Ceil() };
    public DM64 Floor() => new DM64 { _value = _value.Floor() };
    public DM64 Sqrt() => new DM64 { _value = _value.Sqrt() };
    public DM64 Pow(DM64 b) => new DM64 { _value = _value.Pow(b._value) };
    public DM64 Pow(int b) => new DM64 { _value = _value.Pow(b) };
    public static DM64 Max(DM64 a, DM64 b) => new DM64 { _value = DM_Base.Max(a._value, b._value) };

    
    // Operators - just wrap the struct operators
    public static DM64 operator +(DM64 a, DM64 b) => new DM64 { _value = a._value + b._value };
    public static DM64 operator -(DM64 a, DM64 b) => new DM64 { _value = a._value - b._value };
    public static DM64 operator *(DM64 a, DM64 b) => new DM64 { _value = a._value * b._value };
    public static DM64 operator /(DM64 a, DM64 b) => new DM64 { _value = a._value / b._value };
    public static DM64 operator %(DM64 a, DM64 b) => new DM64 { _value = a._value % b._value };
    
    // Operations with ints
    public static DM64 operator +(DM64 a, int b) => new DM64 { _value = a._value + b };
    public static DM64 operator -(DM64 a, int b) => new DM64 { _value = a._value - b };
    public static DM64 operator *(DM64 a, int b) => new DM64 { _value = a._value * b };
    public static DM64 operator /(DM64 a, int b) => new DM64 { _value = a._value / b };
    public static DM64 operator +(int a, DM64 b) => new DM64 { _value = a + b._value };
    public static DM64 operator -(int a, DM64 b) => new DM64 { _value = a - b._value };
    public static DM64 operator *(int a, DM64 b) => new DM64 { _value = a * b._value };
    public static DM64 operator /(int a, DM64 b) => new DM64 { _value = a / b._value };
    
    // Comparisons
    public static bool operator >(DM64 a, DM64 b) => a._value > b._value;
    public static bool operator <(DM64 a, DM64 b) => a._value < b._value;
    public static bool operator >=(DM64 a, DM64 b) => a._value >= b._value;
    public static bool operator <=(DM64 a, DM64 b) => a._value <= b._value;
    public static bool operator ==(DM64 a, DM64 b) => a._value == b._value;
    public static bool operator !=(DM64 a, DM64 b) => a._value != b._value;
    
    public static bool operator >(DM64 a, int b) => a._value > b;
    public static bool operator <(DM64 a, int b) => a._value < b;
    public static bool operator >=(DM64 a, int b) => a._value >= b;
    public static bool operator <=(DM64 a, int b) => a._value <= b;
    public static bool operator ==(DM64 a, int b) => a._value == b;
    public static bool operator !=(DM64 a, int b) => a._value != b;
    
    public override bool Equals(object obj) => obj is DM64 other && _value.Equals(other._value);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToFloat().ToString();
}
using Godot;
using System;
using System.Runtime.CompilerServices;

[GlobalClass]
[Tool]
public partial class DM_Vector: Godot.Resource

{
    [Export]
    public float EditorX
    {
        get
        {
            return x.ToFloat();
        }
        set
        {
            x = new DM64(value);
        }
    }
    public DM64 x = new();
    
    [Export]
    public float EditorY
    {
        get
        {
            return y.ToFloat();
        }
        set
        {
            y = new DM64(value);
        }
    }
    public DM64 y = new();


    // Constructors

    public DM_Vector(float v)
    {
        x = new DM64();
        y = new DM64();
    }

    public DM_Vector(String _x, String _y)
    {
        x = new DM64(_x);
        y = new DM64(_y);
    }
    public DM_Vector(DM64 _x, DM64 _y)
    {
        x = _x.copy();
        y = _y.copy();
    }

    public DM_Vector(int _x, int _y)
    {
        x = new DM64(_x);
        y = new DM64(_y);
    }

    public DM_Vector(Vector2 a)
    {
        x = new DM64(a.X);
        y = new DM64(a.Y);
    }

    public DM_Vector(Tuple<string, string> value)
    {
         x = new DM64(value.Item1);
        y = new DM64(value.Item1);
    }

    public DM_Vector()
    {
        x = new DM64(0);
        y = new DM64(0);
    }

    public DM_Vector copy()
    {
        return new DM_Vector(x.copy(), y.copy());
    }

    // Extractors:
    public Vector2 ToStandardVector()
    {
        // NOT SURE IF THIS SHOULD OUTPUT TO LONGS OR FLOATS. 
        // TODO: TEST CASES FOR CONVERTING TO VECTOR AND NEEDING DETERMINSM
        
        return new Vector2(x.ToFloat(), y.ToFloat());
    }

    public Vector2I ToVector2I()
    {
        return new Vector2I(x.to_int(), y.to_int());
    }

    
    // Basic math operator overloads
    public static bool operator ==(DM_Vector a, DM_Vector b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(DM_Vector a, DM_Vector b) => a.x != b.x || a.y != b.y;
    public static DM_Vector operator +(DM_Vector a, DM_Vector b) => new DM_Vector(a.x + b.x, a.y + b.y);
    public static DM_Vector operator -(DM_Vector a, DM_Vector b) => new DM_Vector(a.x - b.x, a.y - b.y);
    public static DM_Vector operator *(DM_Vector a, DM_Vector b) => new DM_Vector(a.x * b.x, a.y * b.y);
    public static DM_Vector operator /(DM_Vector a, DM_Vector b) => new DM_Vector(a.x / b.x, a.y / b.y);

    public static DM_Vector operator *(DM_Vector a, DM64 b) => new DM_Vector(a.x * b, a.y * b);
    public static DM_Vector operator /(DM_Vector a, DM64 b) => new DM_Vector(a.x / b, a.y / b);

    // Helper that just returns 0 for 0 division
    public DM_Vector CheckedDiv( DM_Vector b) {
        DM_Vector output = new DM_Vector();
        output.x =  b.x == 0? new DM64(0) : x/ b.x; 
        output.y =  b.y == 0? new DM64(0) : y/ b.y; 
        return output;
    }

    public static DM_Vector operator *(DM64 a, DM_Vector b) => new DM_Vector(a * b.x, a * b.y);
    public static DM_Vector operator /(DM64 a, DM_Vector b) => new DM_Vector(a / b.x, a / b.y);

    public static DM_Vector operator *(DM_Vector a, int b) => new DM_Vector(a.x * b, a.y * b);
    public static DM_Vector operator /(DM_Vector a, int b)
    => (b != 0) 
    ? new DM_Vector(a.x / b, a.y / b) 
    : new DM_Vector(0,0);

    // Making it hashable
    
    public override bool Equals(object obj)
	{
		if (obj is DM_Vector other)
		{
			return (this.x == other.x) && (this.y == other.y);
		}
		return false;
	}
	
    public override int GetHashCode()
    {
        return TickManager.DeterministicCombineHashes( x.GetHashCode(), y.GetHashCode() );
    }



    public DM64 GetMagnitude()
    {
        return (x.Pow(2) + y.Pow(2)).Sqrt();
    }

    public DM_Vector Normalized()
    {
        DM64 magnitude = GetMagnitude();
        if (magnitude == 0)
        {
            return new DM_Vector(0, 0);
        }
        return this / magnitude;
    }
    

    static public void UnitTest()
    {
        DM64 a = new DM64(1024);

		DM64 b = new DM64(32);

		// GD.Print((a / b).ToFloat());
		// GD.Print( a.Sqrt().ToFloat() );
		DM_Vector c = new DM_Vector(a, b);
		GD.Print("Expected: ", new Vector2(1024, 32), " Received: ", c.ToStandardVector());

		GD.Print("Expected: ", new Vector2(1024, 32).Normalized(), " Received: ", c.ToStandardVector().Normalized());

		
    }
}
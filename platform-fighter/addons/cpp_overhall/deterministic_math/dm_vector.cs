using Godot;
using System;

public partial struct DM_Vector2
{
    public dm64 x;
    public dm64 y;
    // 


    
    // Constructors
    public DM_Vector2()
    {
        x = new dm64();
        y = new dm64();
    }
    public DM_Vector2(dm_vector_editor_wrapper value)
    {
        x = new dm64(value.x);
        y = new dm64(value.y);
    }
    public DM_Vector2(dm64 _x, dm64 _y)
    {
        x = _x.copy();
        y = _y.copy();
    }

    public DM_Vector2(int _x, int _y)
    {
        x = new dm64(_x);
        y = new dm64(_y);
    }

    public DM_Vector2(Vector2 a)
    {
        x = new dm64(a.X);
        y = new dm64(a.Y);
    }

    public DM_Vector2 copy()
    {
        return new DM_Vector2(x.copy(), y.copy());
    }

    // Extractors:
    public Vector2 ToStandardVector()
    {
        // NOT SURE IF THIS SHOULD OUTPUT TO LONGS OR FLOATS. 
        // TODO: TEST CASES FOR CONVERTING TO VECTOR AND NEEDING DETERMINSM
        
        return new Vector2(x.ToFloat(), y.ToFloat());
    }

    
    // Basic math operator overloads
    public static DM_Vector2 operator +(DM_Vector2 a, DM_Vector2 b) => new DM_Vector2(a.x + b.x, a.y + b.y);
    public static DM_Vector2 operator -(DM_Vector2 a, DM_Vector2 b) => new DM_Vector2(a.x - b.x, a.y - b.y);
    public static DM_Vector2 operator *(DM_Vector2 a, DM_Vector2 b) => new DM_Vector2(a.x * b.x, a.y * b.y);
    public static DM_Vector2 operator /(DM_Vector2 a, DM_Vector2 b) => new DM_Vector2(a.x / b.x, a.y / b.y);

    public static DM_Vector2 operator *(DM_Vector2 a, dm64 b) => new DM_Vector2(a.x * b, a.y * b);
    public static DM_Vector2 operator /(DM_Vector2 a, dm64 b) => new DM_Vector2(a.x / b, a.y / b);

    // Helper that just returns 0 for 0 division
    public DM_Vector2 CheckedDiv( DM_Vector2 b) {
        DM_Vector2 output = new DM_Vector2();
        output.x =  b.x == 0? new dm64(0) : x/ b.x; 
        output.y =  b.y == 0? new dm64(0) : y/ b.y; 
        return output;
    }

    public static DM_Vector2 operator *(dm64 a, DM_Vector2 b) => new DM_Vector2(a * b.x, a * b.y);
    public static DM_Vector2 operator /(dm64 a, DM_Vector2 b) => new DM_Vector2(a / b.x, a / b.y);

    public static DM_Vector2 operator *(DM_Vector2 a, int b) => new DM_Vector2(a.x * b, a.y * b);
    public static DM_Vector2 operator /(DM_Vector2 a, int b) => new DM_Vector2(a.x / b, a.y / b);


    public dm64 GetMagnitude()
    {
        return (x.Pow(2) + y.Pow(2)).Sqrt();
    }

    public DM_Vector2 Normalized()
    {
        dm64 magnitude = GetMagnitude();
        if (magnitude == 0)
        {
            return new DM_Vector2(0, 0);
        }
        return this / magnitude;
    }
    

    static public void UnitTest()
    {
        dm64 a = new dm64(1024);

		dm64 b = new dm64(32);

		// GD.Print((a / b).ToFloat());
		// GD.Print( a.Sqrt().ToFloat() );
		DM_Vector2 c = new DM_Vector2(a, b);
		GD.Print("Expected: ", new Vector2(1024, 32), " Received: ", c.ToStandardVector());

		GD.Print("Expected: ", new Vector2(1024, 32).Normalized(), " Received: ", c.ToStandardVector().Normalized());

		
    }
}
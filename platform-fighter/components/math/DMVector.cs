using System;
using Godot;

public struct DMVector
{
    public DM64 x;
    public DM64 y;

    // Constructors
    public static DMVector zero = new DMVector(0, 0);
    public static DMVector Down = new DMVector(0, -1);
    public static DMVector Up = new DMVector(0, 1);
    public static DMVector Left = new DMVector(-1, 0);
    public static DMVector Right = new DMVector(1, 0);
    public DMVector(DM64 _x, DM64 _y)
    {
        x = _x.copy();
        y = _y.copy();
    }

    public DMVector(int _x = 0, int _y = 0)
    {
        x = new DM64(_x);
        y = new DM64(_y);
    }

    public DMVector(Vector2 a)
    {
        x = new DM64(a.X);
        y = new DM64(a.Y);
    }


    public DMVector copy()
    {
        return new DMVector(x.copy(), y.copy());
    }

    // Extractors:
    public Vector2 ToStandardVector()
    {
        return new Vector2(x.ToFloat(), y.ToFloat());
    }

    public Vector2I ToVector2I()
    {
        return new Vector2I(x.to_int(), y.to_int());
    }



    // Basic math operator overloads
    public static bool operator ==(DMVector a, DMVector b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(DMVector a, DMVector b) => a.x != b.x || a.y != b.y;
    public static DMVector operator +(DMVector a, DMVector b) => new DMVector(a.x + b.x, a.y + b.y);
    public static DMVector operator -(DMVector a, DMVector b) => new DMVector(a.x - b.x, a.y - b.y);
    public static DMVector operator *(DMVector a, DMVector b) => new DMVector(a.x * b.x, a.y * b.y);
    public static DMVector operator /(DMVector a, DMVector b) => new DMVector(a.x / b.x, a.y / b.y);

    public static DMVector operator *(DMVector a, DM64 b) => new DMVector(a.x * b, a.y * b);
    public static DMVector operator /(DMVector a, DM64 b) => new DMVector(a.x / b, a.y / b);

    // Helper that just returns 0 for 0 division
    public DMVector CheckedDiv(DMVector b)
    {
        DMVector output = new DMVector();
        output.x = b.x == 0 ? new DM64(0) : x / b.x;
        output.y = b.y == 0 ? new DM64(0) : y / b.y;
        return output;
    }

    public static DMVector operator *(DM64 a, DMVector b) => new DMVector(a * b.x, a * b.y);
    public static DMVector operator /(DM64 a, DMVector b) => new DMVector(a / b.x, a / b.y);

    public static DMVector operator *(DMVector a, int b) => new DMVector(a.x * b, a.y * b);
    public static DMVector operator /(DMVector a, int b)
    => (b != 0)
    ? new DMVector(a.x / b, a.y / b)
    : new DMVector(0, 0);

    // Making it hashable

    public override bool Equals(object obj)
    {
        if (obj is DMVector other)
        {
            return (this.x == other.x) && (this.y == other.y);
        }
        return false;
    }

    public override int GetHashCode()
    {
       return HashCode.Combine(x.GetHashCode(), y.GetHashCode());
    }


    public Vector3 ToVector3(float z = 0)
    {
        return new Vector3(x.ToFloat(), y.ToFloat(), z);
    }

    public DM64 GetMagnitude()
    {
        return (x.Pow(2) + y.Pow(2)).Sqrt();
    }

    public DMVector Normalized()
    {
        DM64 magnitude = GetMagnitude();
        if (magnitude == 0)
        {
            return new DMVector(0, 0);
        }
        return this / magnitude;
    }


    static public void UnitTest()
    {
        DM64 a = new DM64(1024);

        DM64 b = new DM64(32);

        // GD.Print((a / b).ToFloat());
        // GD.Print( a.Sqrt().ToFloat() );
        DMVector c = new DMVector(a, b);
        HelperMethods.PrintMultiple("Expected: ", new Vector2(1024, 32), " Received: ", c.ToStandardVector());
        HelperMethods.PrintMultiple("Expected: ", new Vector2(1024, 32).Normalized(), " Received: ", c.ToStandardVector().Normalized());
    }
}

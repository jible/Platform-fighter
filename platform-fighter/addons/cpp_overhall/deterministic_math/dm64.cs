using Godot;
using System;
using System.Numerics;
public struct DM64
{
	// 1 sign bit 31 bits of in and 32 bits of decimal
	static int SHIFT = 32;
	static long SCALE = 1L << SHIFT;
	public long raw = 0;

	// Constructors
	public DM64()
	{
		raw = 0;
	}

	public DM64(int value)
	{
		raw = (long)value << SHIFT;
	}
	public DM64(long value)
	{
		raw = value << SHIFT;
	}
	public DM64(float value)
	{
		raw = (long)(value * (float)SCALE);
	}

	public DM64 copy()
	{
		DM64 o = new DM64();
		o.raw = raw;
		return o;
	}

	// Extractors
	public long to_long()
	{
		return raw >> SHIFT;
	}
	
	public float ToFloat()
	{
		float output = ((float)raw) / ((float)SCALE);
		return output;
	}

	// Basic Math overwrites
	public static DM64 operator +(DM64 a, DM64 b)
    {
        DM64 o = new DM64();
        o.raw = a.raw + b.raw;
        return o;
    }
	public static DM64 operator -(DM64 a, DM64 b) {
        DM64 o = new DM64();
        o.raw = a.raw - b.raw;
        return o;
    }
	public static DM64 operator *(DM64 a, DM64 b)
	{
		DM64 f = new DM64();
		f.raw = (long)(((BigInteger)a.raw * (BigInteger)b.raw) >> SHIFT);
		return f;
	}
	 
	public static DM64 operator /(DM64 a, DM64 b)
	{
        if (b.raw == 0)
        {
            GD.Print("0 div");
            throw new DivideByZeroException();
        }
		ulong n = (ulong)(a.raw < 0? -a.raw: a.raw);
        ulong d = (ulong)(b.raw < 0? -b.raw: b.raw);
        bool negative = (a.raw < 0) ^ (b.raw < 0);
		long q;
        
		int n_leading_bit = GetLeadingBitNum(n);

		int bit_diff = 63 - n_leading_bit;
        if (bit_diff > 0)
        {
            n = n << bit_diff;
        }
        q = (long)(n / d);
        if (negative)
        {
            q = -q;
        }
        int final_shift = bit_diff - SHIFT;
        DM64 f = new DM64(0);
        if (final_shift > 0){
            f.raw = q >> final_shift;
        }else
        {
            f.raw = q << -final_shift;
        }
        return f;
	}

	// Supporting math with ints
	public static DM64 operator +(DM64 a, int b) => a + new DM64(b);
	public static DM64 operator -(DM64 a, int b) => a - new DM64(b);
	public static DM64 operator *(DM64 a, int b)
    {
		return a * new DM64(b);
    }
	public static DM64 operator /(DM64 a, int b) => a / new DM64(b);

	public static DM64 operator +(int a, DM64 b) => b + a;
	public static DM64 operator -(int a, DM64 b) => b - a;
	public static DM64 operator *(int a, DM64 b) => b * a;
	public static DM64 operator /(int a, DM64 b) => new DM64(a) / b;

	// Comparison operators
	public static bool operator > (DM64 a, int b) => a.raw > ((long)b << SHIFT);
	public static bool operator < (DM64 a, int b) => a.raw < ((long)b << SHIFT);
	public static bool operator >= (DM64 a, int b) => a.raw >= ((long)b << SHIFT);
	public static bool operator <=(DM64 a, int b) => a.raw <= ((long)b << SHIFT);
	public static bool operator ==(DM64 a, int b) => a.raw == ((long)b << SHIFT);
	public static bool operator !=(DM64 a, int b) => a.raw != ((long)b << SHIFT);


	
	public static bool operator > (DM64 a, DM64 b) => a.raw > b.raw;
	public static bool operator < (DM64 a, DM64 b) => a.raw < b.raw;
	public static bool operator >= (DM64 a, DM64 b) => a.raw >= b.raw;
	public static bool operator <=(DM64 a, DM64 b) => a.raw <= b.raw;
	public static bool operator ==(DM64 a, DM64 b) => a.raw == b.raw;
	public static bool operator !=(DM64 a, DM64 b) => a.raw != b.raw;

	public override bool Equals(object obj)
	{
		if (obj is DM64 other)
		{
			return this.raw == other.raw;
		}
		return false;
	}
	
    public override int GetHashCode()
    {
        return raw.GetHashCode();
    }

	


// Powers
	public DM64 Pow(DM64 b)
	{
		DM64 c = new DM64(1);
		long ib = b.to_long();
		for (int i = 0; i < ib; i++)
		{
			c = c * this;
		}
		return c;
	}
	public DM64 Pow(int b)
	{
		DM64 c = new DM64(1);
		for (int i = 0; i < b; i++)
		{
			c = c * this;
		}
		return c;
	}

// Squre root
	public DM64 Sqrt()
	{
		if (raw <= 0)
		{
			return new DM64(0);
		}

		int precision = 5;
        // Guess something with about half the leading bits of the radicand
        int guessBits = GetLeadingBitNum((ulong)to_long()) / 2;

        DM64 output = new DM64(1 << guessBits);
		// Use the Raphson Newton method
		for (int i = 0; i < precision; i++)
        {
            output = (output + (this / output)) / 2;
		}
		return output;
	}

// Helper for div and square root
	static private int GetLeadingBitNum(ulong a)
	{
		if (a == 0)
		{
			return 0;
		}
		int bit = 0;	
		while ((a >> (bit + 1)) != 0)
		{
			bit += 1;
		}
		return bit;
	}

	public static void UnitTest()
    {
        GD.Print("Expected ", 4 + 5, " output ", (new DM64(4) + new DM64(5)).ToFloat());
        GD.Print("Expected ", 4 + 5, " output ", (new DM64(4) + 5).ToFloat());
        GD.Print("Expected ", 4 + 5, " output ", (new DM64(4) + new DM64(5)).ToFloat());
        GD.Print("Expected ", 4 * 5, " output ", (new DM64(4) * 5). ToFloat());
        GD.Print("Expected ", 2 * 50, " output ", (new DM64(2) * 50). ToFloat());

    } 
}

using Godot;
using System;
public partial class Fix64: Godot.RefCounted
{
	// 1 sign bit 31 bits of in and 32 bits of decimal
	static int SHIFT = 32;
	static long SCALE = 1L << SHIFT;
	// static long int_bits = 0b0111_1111_1111_1111_0000_0000_0000_0000L;
	static long decimal_bits =0x00000000FFFFFFFF;

	public long raw = 0;

	// Constructors
	public Fix64()
	{
		raw = 0;
	}

	public Fix64(int value)
	{
		raw = (long)value << SHIFT;
	}
	public Fix64(long value)
	{
		raw = value << SHIFT;
	}
	public Fix64(float value)
	{
		raw = (long)(value * (float)SCALE);
	}

	public Fix64 copy()
	{
		return new Fix64(raw);
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
	public static Fix64 operator +(Fix64 a, Fix64 b) => new Fix64(a.raw + b.raw);
	public static Fix64 operator -(Fix64 a, Fix64 b) => new Fix64(a.raw - b.raw);
	public static Fix64 operator *(Fix64 a, Fix64 b)
	{
		Fix64 f = new Fix64();
		f.raw = (a.raw * b.raw) >> SHIFT;
		return f;
	}
	 
	public static Fix64 operator /(Fix64 a, Fix64 b)
	{
		

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
        Fix64 f = new Fix64(0);
        if (final_shift > 0){
            f.raw = q >> final_shift;
        }else
        {
            f.raw = q << -final_shift;
        }
        return f;
	}

	// Supporting math with ints
	public static Fix64 operator +(Fix64 a, int b) => new Fix64(a.raw + (b << SHIFT));
	public static Fix64 operator -(Fix64 a, int b) => new Fix64(a.raw - (b << SHIFT));
	public static Fix64 operator *(Fix64 a, int b) => a * new Fix64(b);
	public static Fix64 operator /(Fix64 a, int b) => a / new Fix64(b);

	public static Fix64 operator +(int a, Fix64 b) => new Fix64((a << SHIFT) + b.raw);
	public static Fix64 operator -(int a, Fix64 b) => new Fix64((a << SHIFT) - b.raw);
	public static Fix64 operator *(int a, Fix64 b) => new Fix64(a) * b;
	public static Fix64 operator /(int a, Fix64 b) => new Fix64(a) / b;

// Powers
	public static Fix64 Pow(Fix64 a, Fix64 b)
	{
		Fix64 c = new Fix64(1);
		long ib = b.to_long();
		for (int i = 0; i < ib; i++)
		{
			c = c * a;
		}
		return c;
	}
	public static Fix64 Pow(Fix64 a, int b)
	{
		Fix64 c = new Fix64(1);
		for (int i = 0; i < b; i++)
		{
			c = c * a;
		}
		return c;
	}


// Squre root
	public Fix64 Sqrt()
	{
		GD.Print(raw);
		if (raw <= 0)
		{
			return new Fix64(0);
		}

		int precision = 5;
		// Guess something with about half the leading bits of the radicand
		int guessBits = GetLeadingBitNum((ulong)raw) / 2;
		Fix64 output = new Fix64(0);
		output.raw = 1 <<(guessBits + (SHIFT /2 ) -1 );
		// Use the Raphson Newton method
		for (int i = 0; i < precision; i++)
		{
			GD.Print(output.raw, " ", output + (this / output));
			output = (output + (this/output))/ 2;
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
}

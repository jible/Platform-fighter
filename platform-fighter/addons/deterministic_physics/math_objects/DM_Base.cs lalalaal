using System;
using System.Numerics;
using Godot;

public struct DM_Base{
    // 1 sign bit 31 bits of in and 32 bits of decimal
	static int SHIFT = 32;
	static long SCALE = 1L << SHIFT;
	public long raw = 0;
	public ulong sign_bit = 0x8000000000000000;
	public ulong whole_bits = 0x7FFFFFFF00000000;
	public ulong decimal_bits = 0x0000000FFFFFFFF;

	// Constructors
	public DM_Base()
	{
		raw = 0;
	}

	public DM_Base(int value)
	{
		raw = (long)value << SHIFT;
	}
	

	public DM_Base (string value)
    {
        SetRawFromString(value);
		return;
    }
	public static DM_Base FromRaw(long r)
    {
        DM_Base o = new();
		o.raw = r;
		return o;
    }

	public void SetRawFromString(String value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }
		value = value.Trim();

		bool is_negative = false;
		String unsigned = value;


		if (value [0] == '-')
        {
            is_negative = true;
			unsigned = value[1..];
			if (unsigned.Length == 0)
            {
                raw = 0;
				return;
            }
        }

		String[] parts = unsigned.Split('.');



		uint a ;
		if (parts[0].Length == 0)
		{
			a = 0;

		} else
		{
			a = (uint)parts[0].ToInt();
		}
		uint b;
		uint zeroes = 0;
		
		if (parts.Length > 1)
        {
			for (int i = 0; i < parts[1].Length; i++)
			{
				if (parts[1][i] != '0') break;
				zeroes += 1;
			}


			b = (uint)parts[1].ToInt();
        } else
        {
            b = 0;
        }

		SetRawFromWholeAndDecimal(a, zeroes, b, is_negative);
    }


	public void SetRawFromWholeAndDecimal( uint a, uint zeroes, uint b, bool is_negative = false)
    {
		int decimalDigits = 0;
		uint walker = b;

		
        raw = (long)a << SHIFT;

		if (b == 0) return;
		while (walker > 0)
        {
            walker /= 10;
			decimalDigits ++;
        }
		long divisor = 1;
		for (int i = 1; i < decimalDigits + zeroes; i++)
		{
			divisor *= 10;
			
		}
		raw += ((long) b << SHIFT) / divisor;
		if (is_negative){ raw *= -1;}
    }

	public DM_Base(long value)
	{
		raw = value << SHIFT;
	}
	public DM_Base(float value)
	{
		raw = (long)(value * (float)SCALE);
	}

	public DM_Base copy()
	{
		DM_Base o = new DM_Base();
		o.raw = raw;
		return o;
	}

	// Extractors
	public long to_long()
	{
		return raw / SCALE;
	}
	public int to_int()
    {
        return (int)to_long();
    }
	
	public float ToFloat()
	{
		float output = ((float)raw) / ((float)SCALE);
		return output;
	}

	// Basic Math overwrites
	public static DM_Base operator +(DM_Base a, DM_Base b)
    {
        DM_Base o = new DM_Base();
        o.raw = a.raw + b.raw;
        return o;
    }
	public static DM_Base operator -(DM_Base a, DM_Base b) {
        DM_Base o = new DM_Base();
        o.raw = a.raw - b.raw;
        return o;
    }
	public static DM_Base operator *(DM_Base a, DM_Base b)
	{
		DM_Base f = new DM_Base();
		f.raw = (long)(((BigInteger)a.raw * (BigInteger)b.raw) >> SHIFT);
		return f;
	}
	 
	public static DM_Base operator /(DM_Base a, DM_Base b)
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
        DM_Base f = new DM_Base(0);
        if (final_shift > 0){
            f.raw = q >> final_shift;
        }else
        {
            f.raw = q << -final_shift;
        }
        return f;
	}
	public static DM_Base operator %(DM_Base a, DM_Base b)
    {
        if (b== 0){throw new DivideByZeroException();}

		long r = a.raw % b.raw;
		DM_Base o = new();
		o.raw = r;
		return o;

    }

	// Supporting math with ints
	public static DM_Base operator +(DM_Base a, int b) => a + new DM_Base(b);
	public static DM_Base operator -(DM_Base a, int b) => a - new DM_Base(b);
	public static DM_Base operator *(DM_Base a, int b)
    {
		return a * new DM_Base(b);
    }
	public static DM_Base operator /(DM_Base a, int b) => a / new DM_Base(b);


	public static DM_Base operator +(int a, DM_Base b) => b + a;
	public static DM_Base operator -(int a, DM_Base b) => b - a;
	public static DM_Base operator *(int a, DM_Base b) => b * a;
	public static DM_Base operator /(int a, DM_Base b) => new DM_Base(a) / b;


	// Comparison operators
	public static bool operator > (DM_Base a, int b) => a.raw > ((long)b << SHIFT);
	public static bool operator < (DM_Base a, int b) => a.raw < ((long)b << SHIFT);
	public static bool operator >= (DM_Base a, int b) => a.raw >= ((long)b << SHIFT);
	public static bool operator <=(DM_Base a, int b) => a.raw <= ((long)b << SHIFT);
	public static bool operator ==(DM_Base a, int b) => a.raw == ((long)b << SHIFT);
	public static bool operator !=(DM_Base a, int b) => a.raw != ((long)b << SHIFT);


	
	public static bool operator > (DM_Base a, DM_Base b) => a.raw > b.raw;
	public static bool operator < (DM_Base a, DM_Base b) => a.raw < b.raw;
	public static bool operator >= (DM_Base a, DM_Base b) => a.raw >= b.raw;
	public static bool operator <=(DM_Base a, DM_Base b) => a.raw <= b.raw;
	public static bool operator ==(DM_Base a, DM_Base b) => a.raw == b.raw;
	public static bool operator !=(DM_Base a, DM_Base b) => a.raw != b.raw;


    public override bool Equals(object obj)
	{
		if (obj is DM_Base other)
		{
			return this.raw == other.raw;
		}
		return false;
	}
	
    public override int GetHashCode()
    {
        return (int)raw;
    }

	public DM_Base Sign()
    {
        if (raw > 0) return new DM_Base (1);
		if (raw < 0) return new DM_Base (-1);
		return new DM_Base(0);
    }

	public DM_Base Abs()
    {
		DM_Base o = new ();
		o.raw = raw < 0? - raw : raw;
		return o;
    }

	public DM_Base Round()
    {
        DM_Base o = new();
		if (raw >= 0)
        	o.raw = ((raw + (SCALE>>1)) >> SHIFT) << SHIFT;
		else
        	o.raw = (( (raw - (SCALE>>1)) >> SHIFT) << SHIFT) + SCALE;
		return o;
    }

	public DM_Base Ceil()
    {
        DM_Base o = new();
		long truncated = (raw >> SHIFT) << SHIFT;
		if (raw <= 0)
        {
            return DM_Base.FromRaw(truncated);
        }
        else
        {
            if ( raw == truncated) return DM_Base.FromRaw(truncated);
			return DM_Base.FromRaw(truncated + SCALE);
        }
    }

	public DM_Base Floor()
	{
        DM_Base o = new();
		long truncated = (raw >> SHIFT) << SHIFT;
		if (raw >= 0)
        {
            return DM_Base.FromRaw(truncated);
        }
        else
        {
            if ( raw == truncated) return DM_Base.FromRaw(truncated);
			return DM_Base.FromRaw(truncated - SCALE);
        }
    }

	public static DM_Base  Max(DM_Base a, DM_Base b)
    {
        return a > b ? a.copy() : b.copy();
    }

// Powers
	public DM_Base Pow(DM_Base b)
	{
		DM_Base c = new DM_Base(1);
		long ib = b.to_long();
		for (int i = 0; i < ib; i++)
		{
			c = c * this;
		}
		return c;
	}
	public DM_Base Pow(int b)
	{
		DM_Base c = new DM_Base(1);
		for (int i = 0; i < b; i++)
		{
			c = c * this;
		}
		return c;
	}

// Squre root
	public DM_Base Sqrt()
	{
		if (raw <= 0)
		{
			return new DM_Base(0);
		}

		int precision = 5;
        // Guess something with about half the leading bits of the radicand
        int guessBits = GetLeadingBitNum((ulong)to_long()) / 2;

        DM_Base output = new DM_Base(1 << guessBits);
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
        // GD.Print("Expected ", 4 + 5, " output ", (new DM_Base(4) + new DM_Base(5)).ToFloat());
        // GD.Print("Expected ", 4 + 5, " output ", (new DM_Base(4) + 5).ToFloat());
        // GD.Print("Expected ", 4 + 5, " output ", (new DM_Base(4) + new DM_Base(5)).ToFloat());
        // GD.Print("Expected ", 4 * 5, " output ", (new DM_Base(4) * 5). ToFloat());
        // GD.Print("Expected ", 2 * 50, " output ", (new DM_Base(2) * 50). ToFloat());


        GD.Print("Expected ", 2 % 50, " output ", (new DM_Base(2) % new DM_Base(50)). ToFloat());
        GD.Print("Expected ", 7 % 3, " output ", (new DM_Base(7) % new DM_Base(3)). ToFloat());
        GD.Print("Expected ", 50 % 12, " output ", (new DM_Base(50) % new DM_Base(12)). ToFloat());
        GD.Print("Expected ", 100 % 9, " output ", (new DM_Base(100) % new DM_Base(9)). ToFloat());
        GD.Print("Expected ", 500 % 9, " output ", (new DM_Base(100) % new DM_Base(9)). ToFloat());


		// int [][] tests = [
        //     [100, 10],
		// 	[9,2]
        // ];

		// foreach (int[] test in tests)
		// {
		// 	int a = test[0];
		// 	int b = test[1];
		// 	GD.Print("Add ", a, " ", b, " Expected ", a + b, " output ", (new DM_Base(a) + new DM_Base(b)).ToFloat());
		// 	GD.Print("Add ", a, " ", b, " Expected ", a + b, " output ", (a + new DM_Base(b)).ToFloat());
		// 	GD.Print("Add ", a, " ", b, " Expected ", a + b, " output ", (new DM_Base(a) + new DM_Base(b)).ToFloat());

		// }
    } 
}
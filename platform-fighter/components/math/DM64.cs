using System;
using Godot;

public struct DM64
{

    // 1 sign bit 31 bits of in and 32 bits of decimal
    const int SHIFT = 32;
    const long SCALE = 1L << SHIFT;
    const ulong sign_bit = 0x8000000000000000;
    const ulong whole_bits = 0x7FFFFFFF00000000;
    const ulong decimal_bits = 0x0000000FFFFFFFF;
    
    private long raw;

     
    public float value
    {
        get { return ToFloat(); }
        set { SetFromFloat(value); }
    }




    // Constructors
    public DM64(int value = 0)
    {
        raw = (long)value << SHIFT;
    }

    public static float RawToFloat(long r)
    {
        return ((float)r) / ((float)SCALE);
    }
    public static long FloatToRaw(float f)
    {
        return (long)(f * (float)SCALE);
    }

    public long RawValue => raw;

    public static DM64 FromRaw(long r)
    {
        DM64 o = new();
        o.raw = r;
        return o;
    }


    public void SetRawFromWholeAndDecimal(uint a, uint zeroes, uint b, bool is_negative = false)
    {
        int decimalDigits = 0;
        uint walker = b;


        raw = (long)a << SHIFT;

        if (b == 0) return;
        while (walker > 0)
        {
            walker /= 10;
            decimalDigits++;
        }
        long divisor = 1;
        for (int i = 1; i < decimalDigits + zeroes; i++)
        {
            divisor *= 10;

        }
        raw += ((long)b << SHIFT) / divisor;
        if (is_negative) { raw *= -1; }
    }

    public void SetFromFloat(float f)
    {
        raw = (long)(f * (float)SCALE);
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
    public static DM64 operator +(DM64 a, DM64 b)
    {
        DM64 o = new DM64();
        o.raw = a.raw + b.raw;
        return o;
    }
    public static DM64 operator -(DM64 a, DM64 b)
    {
        DM64 o = new DM64();
        o.raw = a.raw - b.raw;
        return o;
    }
    public static DM64 operator -(DM64 a) => FromRaw(-a.raw);
    public static DM64 operator *(DM64 a, DM64 b)
    {
        DM64 f = new DM64();
        f.raw = MulShift(a.raw, b.raw);
        return f;
    }

    // (a * b) >> SHIFT via a 128-bit product, without allocating. Floors like an arithmetic shift and throws on overflow.
    private static long MulShift(long a, long b)
    {
        unchecked
        {
            ulong ua = (ulong)a, ub = (ulong)b;
            ulong aLo = ua & 0xFFFFFFFFUL, aHi = ua >> 32;
            ulong bLo = ub & 0xFFFFFFFFUL, bHi = ub >> 32;

            ulong p0 = aLo * bLo;
            ulong p1 = aLo * bHi;
            ulong p2 = aHi * bLo;
            ulong p3 = aHi * bHi;

            ulong mid = (p0 >> 32) + (p1 & 0xFFFFFFFFUL) + (p2 & 0xFFFFFFFFUL);
            ulong lo = (p0 & 0xFFFFFFFFUL) | (mid << 32);

            // High word of the unsigned product, corrected to the signed product's high word
            long hi = (long)(p3 + (p1 >> 32) + (p2 >> 32) + (mid >> 32));
            if (a < 0) hi -= b;
            if (b < 0) hi -= a;

            long topBits = hi >> (SHIFT - 1);
            if (topBits != 0 && topBits != -1) throw new OverflowException();

            return (hi << (64 - SHIFT)) | (long)(lo >> SHIFT);
        }
    }


    public static DM64 operator /(DM64 a, DM64 b)
    {
        if (b.raw == 0)
        {
            GD.PushError("Division by 0");
            throw new DivideByZeroException();
        }
        ulong n = (ulong)(a.raw < 0 ? -a.raw : a.raw);
        ulong d = (ulong)(b.raw < 0 ? -b.raw : b.raw);
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
        if (final_shift > 0)
        {
            f.raw = q >> final_shift;
        }
        else
        {
            f.raw = q << -final_shift;
        }
        return f;
    }
    public static DM64 operator %(DM64 a, DM64 b)
    {
        if (b == 0) { throw new DivideByZeroException(); }

        long r = a.raw % b.raw;
        DM64 o = new();
        o.raw = r;
        return o;

    }

    // Supporting math with ints
    public static DM64 operator +(DM64 a, int b) => a + new DM64(b);
    public static DM64 operator -(DM64 a, int b) => a - new DM64(b);
    public static DM64 operator *(DM64 a, int b)
    {
        return a * new DM64(b);
    }
    public static DM64 operator /(DM64 a, int b) => a / new DM64(b);


    public static DM64 operator +(int a, DM64 b) => new DM64(a) + b;
    public static DM64 operator -(int a, DM64 b) => new DM64(a) - b;
    public static DM64 operator *(int a, DM64 b) => new DM64(a) * b;
    public static DM64 operator /(int a, DM64 b) => new DM64(a) / b;


    // Comparison operators
    public static bool operator >(DM64 a, int b) => a.raw > ((long)b << SHIFT);
    public static bool operator <(DM64 a, int b) => a.raw < ((long)b << SHIFT);
    public static bool operator >=(DM64 a, int b) => a.raw >= ((long)b << SHIFT);
    public static bool operator <=(DM64 a, int b) => a.raw <= ((long)b << SHIFT);
    public static bool operator ==(DM64 a, int b) => a.raw == ((long)b << SHIFT);
    public static bool operator !=(DM64 a, int b) => a.raw != ((long)b << SHIFT);



    public static bool operator >(DM64 a, DM64 b) => a.raw > b.raw;
    public static bool operator <(DM64 a, DM64 b) => a.raw < b.raw;
    public static bool operator >=(DM64 a, DM64 b) => a.raw >= b.raw;
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
        return (int)raw;
    }

    public DM64 Sign()
    {
        if (raw > 0) return new DM64(1);
        if (raw < 0) return new DM64(-1);
        return new DM64(0);
    }

    public DM64 Abs()
    {
        DM64 o = new();
        o.raw = raw < 0 ? -raw : raw;
        return o;
    }

    public DM64 Round()
    {
        DM64 o = new();
        if (raw >= 0)
            o.raw = ((raw + (SCALE >> 1)) >> SHIFT) << SHIFT;
        else
            o.raw = (((raw - (SCALE >> 1)) >> SHIFT) << SHIFT) + SCALE;
        return o;
    }

    public DM64 Ceil()
    {
        long floored = ((raw >> SHIFT) << SHIFT);
        return DM64.FromRaw( raw == floored? raw : floored + SCALE);
    }

    public DM64 Floor()
    {
        return DM64.FromRaw((raw >> SHIFT) << SHIFT);
    }

    public DM64 Clamped(DM64 min, DM64 max)
    {
        if (min > max)
        {
            GD.PushError("Min cannot be greater than max in clamped");
        }
        if (this < min) return min;
        if (this > max) return max;
        return this;
    }

    public static DM64 Max(DM64 a, DM64 b)
    {
        return a > b ? a.copy() : b.copy();
    }
    public static DM64 Min(DM64 a, DM64 b)
    {
        return a < b ? a.copy() : b.copy();
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
        // GD.Print("Expected ", 4 + 5, " output ", (new DM64(4) + new DM64(5)).ToFloat());
        // GD.Print("Expected ", 4 + 5, " output ", (new DM64(4) + 5).ToFloat());
        // GD.Print("Expected ", 4 + 5, " output ", (new DM64(4) + new DM64(5)).ToFloat());
        // GD.Print("Expected ", 4 * 5, " output ", (new DM64(4) * 5). ToFloat());
        // GD.Print("Expected ", 2 * 50, " output ", (new DM64(2) * 50). ToFloat());


        HelperMethods.PrintMultiple("Expected ", 2 % 50, " output ", (new DM64(2) % new DM64(50)).ToFloat());
        HelperMethods.PrintMultiple("Expected ", 7 % 3, " output ", (new DM64(7) % new DM64(3)).ToFloat());
        HelperMethods.PrintMultiple("Expected ", 50 % 12, " output ", (new DM64(50) % new DM64(12)).ToFloat());
        HelperMethods.PrintMultiple("Expected ", 100 % 9, " output ", (new DM64(100) % new DM64(9)).ToFloat());
        HelperMethods.PrintMultiple("Expected ", 500 % 9, " output ", (new DM64(100) % new DM64(9)).ToFloat());

    }

}


public static class HelperMethods
{
    // Console.Write doesn't reach Godot's output panel, GD.Print does
    public static void PrintMultiple(params object[] items)
    {
        GD.Print(items);
    }
}
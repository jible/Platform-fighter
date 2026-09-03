using Godot;
using Godot.NativeInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public struct StickState
{
    /* 
    Super Proud of this!
    This Stores the data for a stick into just a single byte! 
    And it makes it super easy to do conversion back to Vectors  
    */

    public enum Axis
    {
        X,
        Y
    }

    public static int MAX_STICK_AXIS_VALUE = 7;
    const int Buckets= 7;
    byte XMagnitudeMask = 0x70; 
    byte XSignMask = 0x80;
    byte YMagnitudeMask = 0x07;
    byte YSignMask = 0x08;
    // How Data is stored:
    // [X-Sign, 2nd bit, 1st bit, 0th bit, Y-Sign, 2nd bit, 1st bit, 0th bit]
    byte Data = 0;

    public StickState()
    {
    }

    public void SetFromVector(Godot.Vector2 V)
    {
        int XSign = V.X < 0 ? 1: 0;
        int YSign = V.Y < 0 ? 1: 0;
        int XBits = (int)Math.Round(Math.Abs(V.X) * Buckets);
        int YBits = (int)Math.Round(Math.Abs(V.Y) * Buckets);
        Data = (byte)(
            XSign << 7 |
            XBits << 4 | 
            YSign << 3|
            YBits 
            );
    }

    public void SetAxis(Axis axis, float Value)
    {
        Value = Math.Clamp(Value, -1f , 1f);
        if (axis == Axis.X)
        {
            Data = (byte)(  (Data & YMagnitudeMask) | (Data & YSignMask) | ((byte)Math.Round(Value * Buckets) << 4));
        } else
        {
            Data = (byte)( (Data & XMagnitudeMask) | (Data & XSignMask) |((byte)Math.Round(Value* Buckets)));
        }
    }

    public DM_Vector2 ToVector()
    {
         
        int UX = (Data & XMagnitudeMask) >> 4;
        int XSign = (Data & XSignMask) == 0 ? 1 : -1;
        int UY = Data & YMagnitudeMask;
        int YSign = (Data & YSignMask) == 0 ? 1 : -1;
        DM64 X = new DM64(UX) * XSign;
        DM64 Y = new DM64(UY) * YSign;
        return new DM_Vector2( X,  Y);
    }

    // Returns input vector, who's magnitude is squished into the range [0,1] (Not normalized)
    public DM_Vector2 ToRangedVector()
    {
         
        DM_Vector2 LargeVector =  ToVector();
        return LargeVector/7;
    }

    public byte GetEncoded()
    {
        return Data;
    }

    public void Decoded(byte Coded)
    {
        Data = Coded;
    }



    public override bool Equals(object obj)
    {
        //
        // See the full list of guidelines at
        //   http://go.microsoft.com/fwlink/?LinkID=85237
        // and also the guidance for operator== at
        //   http://go.microsoft.com/fwlink/?LinkId=85238
        //
        
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        
        // TODO: write your implementation of Equals() here
        return Data == ((StickState)obj).Data;
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        // TODO: write your implementation of GetHashCode() here
        return Data;
    }

    public static bool operator ==(StickState left, StickState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StickState left, StickState right)
    {
        return !(left == right);
    }
}

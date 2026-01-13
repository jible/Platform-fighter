using Godot;
using Godot.NativeInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class StickState
{
    /* 
    Super Proud of this!
    This Stores the data for a stick into just a single bit! 
    And it makes it super easy to do conversion back to Vectors  
    */

    public enum Axis
    {
        X,
        Y
    }

    public static int MAX_STICK_AXIS_VALUE = 7;
    const int Buckets= 7;
    byte XMask = 0x70; 
    byte XSignMask = 0x80;
    byte YMask = 0x07;
    byte YSignMask = 0x08;
    // How Data is stored:
    // [X-Sign, 2nd bit, 1st bit, 0th bit, Y-Sign, 2nd bit, 1st bit, 0th bit]
    byte Data = 0;

    public void SetFromVector(Godot.Vector2 V)
    {
        int XSign = V.X < 0 ? 1: 0;
        int YSign = V.Y < 0 ? 1: 0;
        int XBits = (int)Math.Round(Math.Abs(V.X) * Buckets);
        int YBits = (int)Math.Round(Math.Abs(V.Y) * Buckets);
        GD.Print(XBits, " ",YBits);
        Data = (byte)(
            XSign << 7 |
            XBits << 4 | 
            YSign << 3|
            YBits 
            );
    }

    public void SetAxis(Axis axis, float Value)
    {
        if (axis == Axis.X)
        {
            Data = (byte)((Data & YMask) |((byte)Math.Round(Value* Buckets) << 4));
        } else
        {
            Data = (byte)((Data & XMask) |((byte)Math.Round(Value* Buckets)));
        }
    }

    public DM_Vector2 ToVector()
    {
         
        int UX = (Data & XMask) >> 4;
        int XSign = (Data & XSignMask) == 0 ? 1 : -1;
        int UY = Data & YMask;
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

    public StickState Copy()
    {
        StickState copy = new();
        copy.Data = Data;
        return copy;
    }
}
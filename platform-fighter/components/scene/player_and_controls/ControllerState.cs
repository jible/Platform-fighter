using Godot;
using Godot.NativeInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public struct ControllerState
{
    
    // Represents what bit a given input corresponds to
    // IMPORTANT NOTE: 
    // The order of this enum matters. If a number falls after "SERIALIZEABLE_END", it is not serialized or stored to the state
    // This is made for handling direction inputs, which will modify the stick at the end of the frame.
    public enum ButtonTypes
    {
        SPECIAL,
        LIGHT,
        GRAB,
        JUMP,
        SERIALIZABLE_END, // Does not collect these button states or serialize them
        LEFT_UP,
        LEFT_DOWN,
        LEFT_LEFT,
        LEFT_RIGHT,
        RIGHT_UP,
        RIGHT_DOWN,
        RIGHT_LEFT,
        RIGHT_RIGHT,
        COUNT,
    }

    public enum StickTypes
    {
        LEFT,
        RIGHT,
        COUNT,
    }

    public ButtonTypes[] ToBeSerialized = [
        ButtonTypes.SPECIAL,
        ButtonTypes.LIGHT,
        ButtonTypes.GRAB,
        ButtonTypes.JUMP,
    ];
    const int ButtonCount = (int)ButtonTypes.SERIALIZABLE_END;
    const int ButtonBytes = (ButtonCount + 7) / 8;
    public const int EncodedSize = (int)(ButtonBytes + StickTypes.COUNT);
    uint ButtonStates = 0;


    public StickState LeftStick = new();
    public StickState RightStick = new();

    public ControllerState()
    {
    }

    public void SetButton(ButtonTypes Button, bool Value)
    {
        if (Value)
        {
            uint Mask = (uint)(1 << (int)Button);
            ButtonStates |= Mask;

        }
        else
        {
            uint Mask = ~(1u<< (int)Button);
            ButtonStates &= Mask;
        }
    }

    public bool GetButton (ButtonTypes Button)
    {
        uint Mask = (uint)(1 << (int)Button);
        return (ButtonStates & Mask) != 0;
    }
    


    public byte[] GetEncoded()
    {
        var Output = new byte[ButtonBytes + 2];
        int ByteIndex = 0;
        for ( ;  ByteIndex < ButtonBytes; ByteIndex++)
        {
            int shift = ByteIndex * 8;
            Output[ByteIndex] = (byte)(ButtonStates  >> shift); 

        }
        Output[ByteIndex] = LeftStick.GetEncoded();
        ByteIndex += 1;
        Output[ByteIndex] = RightStick.GetEncoded();

        
        return Output;
    }

    public static ControllerState FromEncoded(byte[] Encoded)
    {
        ControllerState output = new();
        int EncodedSize = (int)(ButtonBytes + StickTypes.COUNT);
        if (Encoded.Length != EncodedSize)
        {
            GD.PushError("Received Input Data of incorrect size");
        }

        int Walker = 0;
        for (; Walker < ButtonBytes;  Walker ++)
        {
            int shift = 8 * Walker;
            output.ButtonStates |= (uint)Encoded [Walker] << shift;
        }
        var LeftStick = new StickState();
        LeftStick.Decoded(Encoded[Walker]);
        output.LeftStick = LeftStick;

        Walker++;
        var RightStick = new StickState();
        RightStick.Decoded(Encoded[Walker]);
        output.RightStick = RightStick;
        return output;
    }

    public override bool Equals(object obj)
    {
        if (obj is not ControllerState Other)return false;

        if (ButtonStates != Other.ButtonStates)return false;

        if (LeftStick != Other.LeftStick || RightStick != Other.RightStick) return false;
        
        return true;
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        int output = 0;
        output = HashCode.Combine(LeftStick.GetHashCode(), output );
        output = HashCode.Combine(RightStick.GetHashCode(), output );

        output = HashCode.Combine(ButtonStates, output );
        
        return output;
    }

    public static bool operator ==(ControllerState left, ControllerState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ControllerState left, ControllerState right)
    {
        return !left.Equals(right);
    }

    // Coppies properties from other state into this state
    public void CopyFromState( ControllerState OtherState)
    {
        ButtonStates = OtherState.ButtonStates;
        LeftStick = OtherState.LeftStick;
        RightStick = OtherState.RightStick;
    }
}


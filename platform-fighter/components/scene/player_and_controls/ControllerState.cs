using Godot;
using Godot.NativeInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ControllerState
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
    byte[] ButtonStates = new byte[ButtonBytes];


    public List<StickState> StickStates = [
        new StickState(),
        new StickState(),
    ];


    public void SetButton(ButtonTypes Button, bool Value)
    {
        int ByteIndex = (int)Button / 8;
        int BitIndex =  (int)Button % 8;

        if (Value)
        {
            byte Mask = (byte)(1 << BitIndex);
            ButtonStates[ByteIndex] |= Mask;

        }
        else
        {
            byte Mask = (byte)((~0 ) ^ 1 << BitIndex);
            ButtonStates[ByteIndex] &= Mask;
        }
    }

    public bool GetButton (ButtonTypes Button)
    {
        int ByteIndex = (int)Button / 8;
        int BitIndex =  (int)Button % 8;

        byte NeededByte = ButtonStates[ByteIndex];
        byte Mask = (byte)(1 << BitIndex);
        return (NeededByte & Mask) != 0;
    }
    
    public ControllerState GetCopy()
    {
        var Copy = new ControllerState();
        ButtonStates.CopyTo(Copy.ButtonStates, 0);
        Copy.StickStates[0] = StickStates[0].Copy();
        Copy.StickStates[1] = StickStates[1].Copy();

        return Copy;
    }


    public byte[] GetEncoded()
    {
        var Output = new byte[ButtonBytes + StickStates.Count];
        ButtonStates.CopyTo(Output, 0);
        int OutputWalker = ButtonBytes;
        foreach (var Stick in StickStates)
        {
            Output[OutputWalker] = Stick.GetEncoded();
            OutputWalker += 1;
        }
        
        return Output;
    }

    public static ControllerState FromEncoded(byte[] Encoded)
    {
        ControllerState output = new();
        int EncodedSize = ButtonBytes + output.StickStates.Count;
        if (Encoded.Count() != EncodedSize)
        {
            GD.PushError("Received Input Data of incorrect size");
        }

        int Walker = 0;
        for (; Walker < ButtonBytes;  Walker ++)
        {
            output.ButtonStates[Walker] = Encoded [Walker];
        }
        for (int i = 0; i < output.StickStates.Count(); i++, Walker++)
        {
            var stick = new StickState();
            stick.Decoded(Encoded[Walker]);
            output.StickStates[i] = stick;
        }

        
        return output;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        ControllerState Other = (ControllerState)obj;
        if (Other.ButtonStates !=ButtonStates)
        {
            return false;
        }

        for (int i = 0; i < StickStates.Count(); i++)
        {
            if (StickStates[i] != Other.StickStates[i]) return false;
        }
        
        return true;
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        int output = 0;
        foreach (var stick in StickStates)
        {
            output = HashCode.Combine(stick.GetHashCode(), output );
        }
        foreach (var item in ButtonStates)
        {
            output = HashCode.Combine(item.GetHashCode(), output );
        }
        return HashCode.Combine(ButtonStates.GetHashCode(), output );
    }


}


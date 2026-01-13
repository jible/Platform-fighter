using Godot;
using Godot.NativeInterop;
using System;
using System.Collections;
using System.Collections.Generic;

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

    public ButtonTypes[] ToBeSerialized = [
        ButtonTypes.SPECIAL,
        ButtonTypes.LIGHT,
        ButtonTypes.GRAB,
        ButtonTypes.JUMP,
    ];
    const int ButtonCount = (int)ButtonTypes.SERIALIZABLE_END;
    const int ButtonBytes = (ButtonCount + 7) / 8;
    sbyte[] ButtonStates = new sbyte[ButtonBytes];


    public List<StickState> StickStates = [
        new StickState(),
        new StickState(),
    ];

    // public ControllerState()
    // {
        
    // }

    public void SetButton(ButtonTypes Button, bool Value)
    {
        int ByteIndex = (int)Button / 8;
        int BitIndex =  (int)Button % 8;

        if (Value)
        {
            sbyte Mask = (sbyte)(1 << BitIndex);
            ButtonStates[ByteIndex] |= Mask;

        }
        else
        {
            sbyte Mask = (sbyte)((~0 ) ^ 1 << BitIndex);
            ButtonStates[ByteIndex] &= Mask;
        }
    }

    public bool GetButton (ButtonTypes Button)
    {
        int ByteIndex = (int)Button / 8;
        int BitIndex =  (int)Button % 8;

        sbyte NeededByte = ButtonStates[ByteIndex];
        sbyte Mask = (sbyte)(1 << BitIndex);
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


    public StreamPeerBuffer GetEncoded()
    {
        var Buffer = new StreamPeerBuffer();
        foreach (var B in ButtonStates)
        {
            Buffer.Put8(B);
        }

        foreach (var Stick in StickStates)
        {
            Buffer.Put8((sbyte)Stick.GetEncoded());
        }
        
        return Buffer;
    }




}


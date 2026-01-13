using Godot;
using System;

public partial class PlayerProfile : Resource
{
    
    public enum ControllerTypes
    {
        KEYBOARD,
        CONTROLLER,
    }


    public int PlayerNumber = 0;
    public int TeamNumber = 0;
    public PlayerTag playerTag = new();
    // public int LocalPeerNumber = 0;

    public String SelectedCharacterPath = "uid://due8lbighr0jf"; 

    public int InputDeviceNumber = 0;



    public ControllerTypes ControllerType = ControllerTypes.CONTROLLER;
    // public CharacterProfile characterProfile;

    // public PlayerTag playerTag = PlayerTag.default;

    public Node Instance;

    public void Configure(int _PlayerNumber, int _InputDeviceNumber, ControllerTypes _ControllerType)
    {
        PlayerNumber = _PlayerNumber;
        InputDeviceNumber = _InputDeviceNumber;
        ControllerType = _ControllerType;
    }
}

using Godot;
using System;

public partial class PlayerProfile : Resource
{
    
    public enum ControllerTypes
    {
        KEYBOARD,
        CONTROLLER,
        REMOTE_PLAYER
    }


    public int PlayerNumber = 0;
    public int TeamNumber = 0;
    public PlayerTag playerTag = new();
    // public int LocalPeerNumber = 0;

    public String SelectedCharacterPath = "uid://due8lbighr0jf"; 

    public int InputDeviceNumber = -1;
    public int RemotePeerID = -1;



    public ControllerTypes ControllerType = ControllerTypes.CONTROLLER;
    // public CharacterProfile characterProfile;

    // public PlayerTag playerTag = PlayerTag.default;

    public Node Instance;

    public void Configure(int _PlayerNumber, int _InputDeviceNumber, ControllerTypes _ControllerType, int _RemotePeerId = -1)
    {
        PlayerNumber = _PlayerNumber;
        InputDeviceNumber = _InputDeviceNumber;
        ControllerType = _ControllerType;
        RemotePeerID = _RemotePeerId;
    }
}

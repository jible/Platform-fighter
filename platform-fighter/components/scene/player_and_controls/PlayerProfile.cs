using Godot;
using System;

public partial class PlayerProfile : Resource
{
    
    public enum ControllerTypes
    {
        KEYBOARD,
        CONTROLLER,
        REMOTE_PLAYER,
        UNACCEPTED_INPUT
    }


    public int PlayerNumber = 0;
    public int TeamNumber = 0;
    // The tag this player's binds come from. Local to this machine, so it means nothing for remote players
    public int TagId { get; private set; }
    // public int LocalPeerNumber = 0;

    public CharacterId Character = CharacterId.Platty;

    public int InputDeviceNumber = -1;
    public int RemotePeerID = -1;



    public ControllerTypes ControllerType = ControllerTypes.CONTROLLER;
    // public CharacterProfile characterProfile;

    public Node Instance;

    public void Configure(int _PlayerNumber, int _InputDeviceNumber, ControllerTypes _ControllerType, int _RemotePeerId = -1)
    {
        PlayerNumber = _PlayerNumber;
        InputDeviceNumber = _InputDeviceNumber;
        ControllerType = _ControllerType;
        RemotePeerID = _RemotePeerId;
        UseDefaultTag();
    }

    // When the player picks a tag
    public void SetTag(int tagId)
    {
        TagId = tagId;
        TagManager.MarkUsed(tagId);
    }

    // Until the player picks a tag, or when theirs is deleted. Not a pick, so it doesn't count as using the tag
    public void UseDefaultTag()
    {
        TagId = TagManager.DefaultFor(ControllerType == ControllerTypes.KEYBOARD);
    }
}

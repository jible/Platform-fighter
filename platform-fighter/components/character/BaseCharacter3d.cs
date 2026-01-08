using Godot;
using System;

public partial class BaseCharacter3d : Node3D
{
    public int TeamNumber;
    public int PlayerNumber;
    public PlayerTag playerTag;

    public PlayManager playManager;
    public void ConfigurePlayer(int _teamNumber, int _playerNumber, PlayerTag _playerTag)
    {
        TeamNumber = _teamNumber;
        PlayerNumber = _playerNumber;
        playerTag = _playerTag;

        CharacterHolder characterHolder = (CharacterHolder)GetParent();
        playManager = characterHolder.playManager;
        // Many nodes may have a configure method that still needs to be called
        // TODO: Implement that soon!
    }
}

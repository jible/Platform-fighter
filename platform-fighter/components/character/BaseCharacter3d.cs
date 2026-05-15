using Godot;
using System;

[Tool]
public partial class BaseCharacter3d : Node3D
{
    [Export] InputHandler inputHandler;
    [Export] DP_PlayerBody playerBody;
    public int PlayerNumber;
    public PlayerProfile playerProfile;

    public PlayManager playManager;
    public void ConfigurePlayer(int _playerNumber)
    {
        PlayerNumber = _playerNumber;
        playerProfile = PlayerManager.GlobalInstance.AllPlayers[PlayerNumber];

        CharacterHolder characterHolder = (CharacterHolder)GetParent();
        playManager = characterHolder.playManager;


        inputHandler.Configure();
        // Many nodes may have a configure method that still needs to be called
        // TODO: Implement that soon!
    }
}

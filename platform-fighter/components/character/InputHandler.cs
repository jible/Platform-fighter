using Godot;
using System;

[GlobalClass]
[Tool]
public partial class InputHandler : Node
{
    /* 
    This Node is basically a middle man between the player nodes and the input manager. 
    Rather than all player nodes needing to establish a reference to the  
    input manager, just this one does. All of the other nodes can just get export
    references to this node and have it call the poll for inputs function.
    */
    [Export] BaseCharacter3d baseCharacter;
    InputManager inputManager;
    PlayManager playManager;
    int PlayerNumber;

    public void Configure()
    {
        PlayerNumber = baseCharacter.PlayerNumber;
        playManager = baseCharacter.playManager;
        inputManager = playManager.inputManager;
    }

    public bool PollForInput(ControllerState.ButtonTypes Button)
    {
       return inputManager.PollForInput(Button, PlayerNumber);
    }

    public DM_Vector PollForStickState(int StickNumber)
    {
       return inputManager.PollForStickState(StickNumber,PlayerNumber);
    }
}

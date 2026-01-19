using Godot;
using System;

public class HostMenuState: CharSelectMenuState
    {
        public HostMenuState(CharacterSelect _charSelectScene) : base(_charSelectScene)
        {
        }

        public override void HandleInput(InputEvent inputEvent)
        {
            if (!inputEvent.IsPressed()) return;
            if (inputEvent.IsActionPressed("debug_ready") )
            {
                PlayerManager.GlobalInstance.AttemptAddPlayer(inputEvent);
            }  else if (inputEvent.IsActionPressed("start"))
            {
            }
        }

         public override void OnPlayerAdded(int PlayerNumber)
        {
            PlayerProfile NewPlayer = PlayerManager.GlobalInstance.AllPlayers[PlayerNumber];
            NetworkManager.GlobalInstance.NotifyPlayerAdded(PlayerNumber, NewPlayer.RemotePeerID);
        }
    }

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
            int PlayerNumber = PlayerManager.GlobalInstance.GetPlayerNumberFromInput(inputEvent);

            if (inputEvent.IsActionPressed("debug_ready") && PlayerNumber == -1)
            {
                PlayerManager.GlobalInstance.AttemptAddPlayer(inputEvent);
            }  else if (inputEvent.IsActionPressed("start"))
            {
                NetworkManager.GlobalInstance.SendMessage(NetworkManager.NetworkMessageType.EnterMatch, null);
                charSelectScene.GoToPlayScene();
            }
        }

    }

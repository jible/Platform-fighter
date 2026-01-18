using Godot;
using System;

public class OfflineMenuState: CharSelectMenuState
    {
        public OfflineMenuState(CharacterSelect _charSelectScene) : base(_charSelectScene)
        {
        }

        public override void HandleInput(InputEvent inputEvent)
        {
            if (!inputEvent.IsPressed()) return;
            if (inputEvent.IsActionPressed("debug_ready") )
            {
                PlayerManager.GlobalInstance.AttemptAddPlayer(inputEvent);
            } else if (inputEvent.IsActionPressed("play_default_special"))
            {
                PlayerManager.GlobalInstance.AttemptRemovePlayer(inputEvent);
            } else if (inputEvent.IsActionPressed("start"))
            {
                charSelectScene.GoToPlayScene();
            }
        }
    }

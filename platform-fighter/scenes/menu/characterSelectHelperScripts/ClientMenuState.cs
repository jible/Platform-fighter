using Godot;
using System;
using System.Collections.Generic;

public class ClientMenuState: CharSelectMenuState
    {
        public ClientMenuState(CharacterSelect _charSelectScene) : base(_charSelectScene)
        {
        }

        // List of controllers that are waiting to be added as a player

        public override void HandleInput(InputEvent inputEvent)
        {
            if (!inputEvent.IsPressed()) return;
            // Will be negative 1 if no player is bound to it
            int PlayerNumber = PlayerManager.GlobalInstance.GetPlayerNumberFromInput(inputEvent);
            if (inputEvent.IsActionPressed("debug_ready") && PlayerNumber == -1 && !CheckIfQueued(inputEvent))
            {
                GD.Print("REady pressed)");
                PlayerManager.GlobalInstance.QueueController(inputEvent);
                NetworkManager.GlobalInstance.lobbyManager.RequestAddPlayer();
            }
        }

        public bool CheckIfQueued(InputEvent inputEvent)
        {
            foreach (var q in PlayerManager.GlobalInstance.QueuedControllers)
            {
                if (PlayerManager.IsSameDevice(q, inputEvent)) return true;
            }
            return false;
        }

        public override void Ready()
        {
            base.Ready();
        }

    }
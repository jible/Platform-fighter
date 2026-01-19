using Godot;
using System;

   public class CharSelectMenuState
    {
        public CharacterSelect charSelectScene;
        public CharSelectMenuState(CharacterSelect _charSelectScene)
        {
            charSelectScene = _charSelectScene;
        }
        public virtual void  Ready(){}
        public virtual void HandleInput(InputEvent inputEvent ){}

        public virtual void OnPlayerAdded(int PlayerNum){}
    }

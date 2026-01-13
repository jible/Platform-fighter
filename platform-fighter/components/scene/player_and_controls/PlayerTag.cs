using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class PlayerTag : Resource
{
    string PlayPrefix = "play";
    string DefaultTagName = "default";
    public Dictionary<ControllerState.ButtonTypes, List<object>> ActionMap = [];
    public Dictionary<Object, ControllerState.ButtonTypes> ReverseMap = [];


    public static Dictionary<ControllerState.ButtonTypes, object[]> DefaultInputActionMap = new Dictionary<ControllerState.ButtonTypes, object[]>{
        {ControllerState.ButtonTypes.LIGHT,  [Key.E, JoyButton.A]},
        {ControllerState.ButtonTypes.SPECIAL ,  [Key.Shift, JoyButton.B]},
        {ControllerState.ButtonTypes.JUMP ,  [Key.Space, JoyButton.X]},
        {ControllerState.ButtonTypes.GRAB ,  [Key.Q, JoyButton.RightShoulder]},
        {ControllerState.ButtonTypes.LEFT_UP,  [Key.W]},
        {ControllerState.ButtonTypes.LEFT_DOWN,  [Key.S]},
        {ControllerState.ButtonTypes.LEFT_LEFT,  [Key.A]},
        {ControllerState.ButtonTypes.LEFT_RIGHT,  [Key.D]},
        {ControllerState.ButtonTypes.RIGHT_UP,  [Key.Up]},
        {ControllerState.ButtonTypes.RIGHT_DOWN,  [Key.Down]},
        {ControllerState.ButtonTypes.RIGHT_LEFT,  [Key.Left]},
        {ControllerState.ButtonTypes.RIGHT_RIGHT,  [Key.Right]},
    };


    public PlayerTag()
    {
        if (ActionMap.Count == 0)
        {
            foreach (ControllerState.ButtonTypes Action in DefaultInputActionMap.Keys)
            {
                ActionMap[Action] = DefaultInputActionMap[Action].ToList();
            }
            UpdateReverseMap();
        }
    }

    public void AddInputEvent(ControllerState.ButtonTypes Action, ControllerState.ButtonTypes Button )
    {
        ActionMap[Action].Add(Button);
    }

    public void RemoveInputEvent(ControllerState.ButtonTypes Action, ControllerState.ButtonTypes Button )
    {
        ActionMap[Action].Remove(Button);
    }

    public void UpdateReverseMap()
    {
        ReverseMap.Clear();
        foreach (var Action in ActionMap.Keys)
        {

            foreach (var Event in ActionMap[Action])
            {
                ReverseMap[Event] = Action;
            }
        }

    }
}

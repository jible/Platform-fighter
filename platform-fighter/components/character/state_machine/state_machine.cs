using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class CharacterStateMachine<StateEnum>:Node where StateEnum : Enum
{
    public Dictionary<StateEnum, State<StateEnum>> StateToObject;
    public State<StateEnum>? CurrentState = null;
    // Some State make sense 
    // If you need a collection of 
    public enum StateBucketKeys {
        Attack
    }
    
    public Dictionary<StateBucketKeys, StateEnum> StateBuckets; 
    public void ChangeState(StateEnum StateArg)
    {
        State<StateEnum> NewStateObject = StateToObject[StateArg];
        if (StateToObject[StateArg] == CurrentState)
        {
            // Don't change to a state you're already in
            return;
        }


        if (CurrentState != null)
        {
            CurrentState.LeaveState();
        }

        CurrentState = NewStateObject;
        CurrentState.EnterState();
    }


    public void OnAnimationFinished()
    {
        CurrentState.OnAnimationFinished();
    }

    public void Tick()
    {
        CurrentState.Tick();
    }
}

public abstract class State<StateEnum> where StateEnum: Enum
{
    public Animation MyAnimation;
    public abstract bool Condition();

    public abstract void EnterState();
    public abstract void LeaveState();
    public abstract void OnAnimationFinished();
    public abstract void Tick();
}

public partial class JohnnyStateMachine : CharacterStateMachine<JohnnyStateMachine.States>
{
    public enum States {};
    // public override Dictionary< StateBucketKeys, JohnnyStateMachine.States> StateBuckets = 
    // {
    //     {}
    // };


}
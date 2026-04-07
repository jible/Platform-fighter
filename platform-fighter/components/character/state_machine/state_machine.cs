using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class CharacterStateMachine<StateEnum>:Node where StateEnum : Enum
{
    public Dictionary<StateEnum, State<StateEnum>> StateToObject;
    public State<StateEnum> CurrentState = null;

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


// Sample state machine 
public partial class JohnnyStateMachine : CharacterStateMachine<JohnnyStateMachine.States>
{
    public enum States {};
    // public override Dictionary< StateBucketKeys, JohnnyStateMachine.States> StateBuckets = 
    // {
    //     {}
    // };


}


/*
Imagined workflow:


Making a new character
you manually add a new state machine? (This is a little annoying since it doesn't inherit the references)



Ideally: You can add a new character and their default input flow already 
works for the state machine and you don't need to set references. You just 
have to attatch animation and set properties like movement attributes.

You won't need to change anything about the statemachine, just the states, which have exportable properties for everything.
- Acceleration
- Max Velocity
- Animation

- Apply color?
- Particles?


How is this achieveable?

Base character state machine already comes with all of the states.
Somehow, you can add new methods to each state?

- What methods would you need?
    - instancing projectiles? Can this be done in the animation?
    - recoil damage? <- This can be done in the animation with generalized method
    - Set (super/heavy) armor? <- this can be done in the animation




If states are nodes, you can put export properties on them! This makes nodes a good candidate for states.
you would export animation references( or just use naming conventions)?

How do states respond to inputs? 
Signal based? 


Got it:

States are nodes with exportable properties: including their method resoucres
They default to a reference with no methods, however you can make a new one 
'and replace that resource with a different version that has the mthods you need!

*/
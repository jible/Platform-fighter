using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Godot;

[GlobalClass]
public partial class CharacterStateMachine: Node, ICanBeTicked/*, ISerializable*/
{
    [Export]
    public AnimationPlayer animationPlayer;
    [Export] public dp_player_body PlayerBody;
    public Dictionary<States, State> StateTitleToState;
    public State CurrentState = null;
    [Export]
    public States StartingState;
    public enum States
    {
        IDLE,
        WALK,
        RUN,
        CROUCH,
        JUMP,
        ARIEL_JUMP,
        FALLING,
        HURT,
        
        ATTACK1,
        ATTACK2,
        
        
        COUNT
    };

    public enum StateTags {
        AllStates,
        Attack,
        Actionable,
    }
    // This method populates the StateTitleToState dictionary with all of the children of the node. 
    public override void _Ready()
    {
        Config();
    }
        
    public void Config()
    {
        StateTitleToState = new();
        CollectStateRecursion(this);
        ChangeState(StartingState);
        animationPlayer.AnimationFinished += (e) => {OnAnimationFinished();};
    }


    private void CollectStateRecursion(Node Parent)
    {
        if (Parent is State CastedParent)
        {
            // Give it a reference to this state machine
            CastedParent.stateMachine = this;
            State populatedState;
            StateTitleToState.TryGetValue(CastedParent.StateTitle, out populatedState);
            if ( populatedState == null)
            {
                StateTitleToState[CastedParent.StateTitle] = CastedParent;
            }
        }

        foreach (var child in Parent.GetChildren())
        {
            CollectStateRecursion(child);
        }
    }

    // Collects all states who's conditions need to be checked while in the given state
    private SortedSet<State> CollectAvailableStates(State Target)
    {
        var output = new SortedSet<State>();
 
        
        for (int StateTitle = 0; StateTitle < (int)States.COUNT; StateTitle++)
        {
            State state;
            StateTitleToState.TryGetValue((States)StateTitle, out state);
            if (state == null ) continue;
            if (state == Target) continue;
            
            if (Target.AccessibleStates.Contains(state.StateTitle))
            {
                output.Add(state);
                continue;
            }

            foreach (var i in state.Tags)
            {
                if (Target.AccessibleTags.Contains(i))
                {
                    output.Add(state);
                    continue;
                }
            }
            
        }
        
        
        return output;
    }
    
    SortedSet<State> AvailableStates = new();
    public void ChangeState(States StateArg)
    {
        State NewStateObject = StateTitleToState[StateArg];
        if (StateTitleToState[StateArg] == CurrentState)
        {
            // Don't change to a state you're already in
            return;
        }

        if (CurrentState != null)
        {        
            CurrentState.LeaveState();
        }

        CurrentState = NewStateObject;
        AvailableStates = CollectAvailableStates(CurrentState);
        var NextAnimation = CurrentState.AnimationName;
        if (NextAnimation != null && animationPlayer != null)
        {
            animationPlayer.Play(NextAnimation);
        }
        CurrentState.EnterState();

    }


    public void OnAnimationFinished()
    {
        CurrentState.OnAnimationFinished();
    }

    public void Tick()
    {
        CurrentState.Tick();
        foreach (var state in AvailableStates)
        {
            GD.Print(state);
            if (state.GeneralCondition())
            {
                ChangeState(state.StateTitle);
                return;
            }
        }


    }

    public ISaveHandler MakeSaveHandler()
    {
        throw new NotImplementedException();
    }

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
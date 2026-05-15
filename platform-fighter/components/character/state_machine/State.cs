using Godot;
using System;
using System.Collections.Generic;

public abstract partial class State: Node
{
    [Export] public CharacterStateMachine.States StateTitle;
    /// <summary>
    /// This is the list of states that this state can enter when that state's general conditions occur
    /// </summary>
    [Export] public Godot.Collections.Array<CharacterStateMachine.States> AccessibleStates = new();
    /// <summary>
    /// In this state, every frame check the conditions for states that have these tags.
    ///  if its condition is fulfilled, change to that state.
    /// </summary>
    [Export] public Godot.Collections.Array<CharacterStateMachine.StateTags> AccessibleTags = new();
    [Export] public Godot.Collections.Array<CharacterStateMachine.StateTags> Tags = new();
    public Animation MyAnimation;
    // TODO: Establish these references:
    public CharacterStateMachine stateMachine; 
    public InputHandler inputHandler;
    
    public abstract bool GeneralCondition();
    public abstract void EnterState();
    public abstract void LeaveState();
    public abstract void OnAnimationFinished();
    public abstract void Tick();
}

using System;

// Every character state. Values must stay contiguous starting at 0, since they index the state table
public enum CharacterStateId : byte
{
    Idle,
    Walk,
    Jump,
    Fall,
    Turn
}

// Everything the state machine needs to roll back. Kept unmanaged so it fits in a SerializableProperty
public struct CharacterStateData
{
    public CharacterStateId Id;
    public uint TicksInState;
}

// Runs a character's states. Plain C#, made and owned by the character.
// The states are handed in by the character, one per CharacterStateId. They must not hold data:
// per character data belongs on the Character, so the only rollback state is the current id and its tick count
public class CharacterStateMachine
{
    public readonly Character Character;
    // Indexed by CharacterStateId
    private readonly CharacterState[] states;
    private readonly SerializableProperty<CharacterStateData> data = new();
    // Only lives for the duration of a tick, so it doesn't need to be rolled back
    private bool changedThisTick;
    public Action<CharacterStateId> StateChanged;
    public CharacterStateId CurrentStateId => data.Value.Id;
    public uint TicksInState => data.Value.TicksInState;
    public CharacterState CurrentState => states[(int)data.Value.Id];

    public CharacterStateMachine(Character character, CharacterState[] stateList, CharacterStateId startingState)
    {
        Character = character;
        states = BuildStateTable(stateList);
        data.Value = new CharacterStateData { Id = startingState, TicksInState = 0 };
        CurrentState.EnterState(this);
    }

    // Every CharacterStateId must have exactly one state, so a missing one fails when the character is made, not mid match
    private static CharacterState[] BuildStateTable(CharacterState[] stateList)
    {
        var table = new CharacterState[Enum.GetValues<CharacterStateId>().Length];
        foreach (var state in stateList)
        {
            if (table[(int)state.Id] != null)
            {
                throw new InvalidOperationException($"Two states registered for {state.Id}");
            }
            table[(int)state.Id] = state;
        }
        for (int i = 0; i < table.Length; i++)
        {
            if (table[i] == null)
            {
                throw new InvalidOperationException($"No state registered for {(CharacterStateId)i}");
            }
        }
        return table;
    }

    public bool IsInState(CharacterStateId id) { return data.Value.Id == id; }

    public void ChangeState(CharacterStateId id)
    {
        if (IsInState(id)) return;

        CurrentState.ExitState(this);
        data.Value = new CharacterStateData { Id = id, TicksInState = 0 };
        changedThisTick = true;
        CurrentState.EnterState(this);
        StateChanged?.Invoke(id);
    }

    public void Tick()
    {
        changedThisTick = false;
        CurrentState.Tick(this);

        // A state entered this tick sees TicksInState == 0 on its first tick
        if (!changedThisTick)
        {
            data.Value.TicksInState++;

            // Timing comes from the state's data, never the art, so it is the same on every machine
            StateDefinition state = Character.Definition.GetState(CurrentStateId);
            bool loops = state != null && state.Loop;
            int length = state != null ? state.Length : Character.Data.DefaultStateLength;
            // Fires once, on the tick the animation runs out. Looping states never finish
            if (!loops && data.Value.TicksInState == length)
            {
                CurrentState.OnAnimFinished(this);
            }
        }
    }
}

public abstract class CharacterState
{
    public abstract CharacterStateId Id { get; }
    public virtual void EnterState(CharacterStateMachine machine) { }
    public virtual void ExitState(CharacterStateMachine machine) { }
    public virtual void Tick(CharacterStateMachine machine) { }
    public virtual void OnAnimFinished(CharacterStateMachine machine) { }
    public abstract bool ApplyGravity {get;}
    /*
    Paste this into a class to get the defaults:

    public override CharacterStateId Id => CharacterStateId.;
    public override bool ApplyGravity => ;
    public override void EnterState(CharacterStateMachine machine) { }
    public override void ExitState(CharacterStateMachine machine) { }
    public override void Tick(CharacterStateMachine machine) { }
     */
}

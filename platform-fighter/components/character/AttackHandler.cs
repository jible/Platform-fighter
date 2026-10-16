// Turns attack inputs into attacks. Reads the button and stick, looks the input up in the character's attacks,
// and starts the Attack state with the match. The map from input to attack is on the shared CharacterDefinition,
// so the only rollback state here is which attack is playing.
// Plain C#, owned by the character. The state machine calls TryStartAttack while its state is cancellable
public class AttackHandler
{
    // Stick magnitude below this fraction of full tilt reads as neutral. Separate from the controller's own deadzone
    public DM64 NeutralZone = new DM64(1) / 5;

    private readonly Character character;
    private readonly InputHandler input;
    // Index into the definition's attacks, or NoAttack before the first one starts.
    // Left on the last attack after it ends, since only the Attack state reads it
    private readonly SerializableProperty<int> currentAttack = new();

    private const int NoAttack = -1;

    public AttackHandler(Character character, InputHandler input)
    {
        this.character = character;
        this.input = input;
        currentAttack.Value = NoAttack;
    }

    // The attack the Attack state is playing. Null before any attack has started
    public AttackDefinition Current => currentAttack.Value == NoAttack ? null : character.Definition.Attacks[currentAttack.Value];

    // Starts the attack for this tick's input, if there is one. An input with no attack does nothing
    public bool TryStartAttack()
    {
        if (!TryReadInput(out AttackInput attackInput)) return false;
        AttackDefinition attack = character.Definition.FindAttack(attackInput);
        if (attack == null) return false;

        currentAttack.Value = attack.Index;
        character.StateMachine.ChangeState(CharacterStateId.Attack);
        return true;
    }

    // False when no attack button was pressed this tick
    public bool TryReadInput(out AttackInput attackInput)
    {
        attackInput = default;
        if (!TryReadButton(out AttackButton button)) return false;
        AttackContext context = character.Movement.IsGrounded ? AttackContext.Ground : AttackContext.Air;
        attackInput = new AttackInput(button, ReadDirection(), context);
        return true;
    }

    // Only a press this tick counts. B wins when both are pressed together
    public bool TryReadButton(out AttackButton button)
    {
        if (input.JustPressed(ControllerState.ButtonTypes.SPECIAL))
        {
            button = AttackButton.B;
            return true;
        }
        if (input.JustPressed(ControllerState.ButtonTypes.LIGHT))
        {
            button = AttackButton.A;
            return true;
        }
        button = default;
        return false;
    }

    // Forward and back are relative to facing. Outside the neutral zone the larger axis wins,
    // and horizontal wins an exact diagonal. Squared magnitudes, so there's no square root
    public StickDirection ReadDirection()
    {
        DMVector stick = input.LeftStick();
        DM64 neutral = NeutralZone * StickState.MAX_STICK_AXIS_VALUE;
        if (stick.x * stick.x + stick.y * stick.y < neutral * neutral) return StickDirection.Neutral;

        if (stick.x.Abs() >= stick.y.Abs())
        {
            bool towardFacing = (stick.x > 0) == (character.direction == Character.Direction.Right);
            return towardFacing ? StickDirection.Forward : StickDirection.Back;
        }
        return stick.y > 0 ? StickDirection.Up : StickDirection.Down;
    }
}

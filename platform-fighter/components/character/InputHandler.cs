// One player's controller, as the simulation sees it. Everything is read from the inputs saved for the
// current tick, so a rollback resimulation sees exactly what the first run saw.
// Presses are buffered: a press within the player's tag's InputBuffer ticks still reads as just pressed,
// until whatever acted on it consumes it. Plain C#, owned by the character
public class InputHandler
{
    // Past this, the buffer would read inputs the rollback buffers have already written over
    public const int MaxInputBuffer = 20;

    public readonly int PlayerNumber;
    // Ticks back a press still counts, 0 for only this tick. Read once from the player's tag, so a tag edited
    // mid match doesn't change it
    public readonly int InputBuffer;
    private readonly InputManager inputManager;
    // Per button, the tick of the last press that was acted on, so a buffered press is only used once
    private readonly SerializableProperty<int>[] consumedPressTicks = new SerializableProperty<int>[(int)ControllerState.ButtonTypes.SERIALIZABLE_END];

    private const int NoPress = int.MinValue;

    public InputHandler(int playerNumber, InputManager inputManager)
    {
        PlayerNumber = playerNumber;
        this.inputManager = inputManager;
        InputTag tag = TagManager.Get(PlayerManager.GlobalInstance.AllPlayers[playerNumber]?.TagId ?? -1);
        InputBuffer = System.Math.Clamp(tag?.InputBuffer ?? 0, 0, MaxInputBuffer);
        for (int i = 0; i < consumedPressTicks.Length; i++)
        {
            consumedPressTicks[i] = new SerializableProperty<int> { Value = NoPress };
        }
    }

    public bool IsDown(ControllerState.ButtonTypes button) => inputManager.PollForInput(button, PlayerNumber);
    // Pressed within the buffer, and not consumed yet. Doesn't consume it, so call Consume when acting on it
    public bool JustPressed(ControllerState.ButtonTypes button) => FindPress(button) != NoPress;
    // Up this tick, down the tick before. Not buffered
    public bool JustReleased(ControllerState.ButtonTypes button) => !IsDown(button) && inputManager.PollForInput(button, PlayerNumber, 1);

    // Uses up the press JustPressed found, so it can't start anything else
    public void Consume(ControllerState.ButtonTypes button)
    {
        int press = FindPress(button);
        if (press != NoPress) consumedPressTicks[(int)button].Value = press;
    }

    // JustPressed, consuming the press when there is one
    public bool TryConsume(ControllerState.ButtonTypes button)
    {
        if (!JustPressed(button)) return false;
        Consume(button);
        return true;
    }

    // Each axis is in whole steps from -StickState.MAX_STICK_AXIS_VALUE to MAX_STICK_AXIS_VALUE
    public DMVector LeftStick() => inputManager.PollForStickState((int)ControllerState.StickTypes.LEFT, PlayerNumber);
    public DMVector RightStick() => inputManager.PollForStickState((int)ControllerState.StickTypes.RIGHT, PlayerNumber);

    // The tick of the oldest unconsumed press in the buffer, or NoPress. Oldest first, so two quick presses
    // buffer two actions, like a jump then an air jump
    private int FindPress(ControllerState.ButtonTypes button)
    {
        int currentTick = inputManager.tickManager.CurrentTick;
        int consumed = consumedPressTicks[(int)button].Value;
        for (int ticksAgo = InputBuffer; ticksAgo >= 0; ticksAgo--)
        {
            int tick = currentTick - ticksAgo;
            if (tick <= consumed) continue;
            if (inputManager.PollForInput(button, PlayerNumber, ticksAgo) && !inputManager.PollForInput(button, PlayerNumber, ticksAgo + 1)) return tick;
        }
        return NoPress;
    }
}

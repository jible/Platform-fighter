// One player's controller, as the simulation sees it. Everything is read from the inputs saved for the
// current tick, so a rollback resimulation sees exactly what the first run saw.
// Plain C#, owned by the character
public class InputHandler
{
    public readonly int PlayerNumber;
    private readonly InputManager inputManager;

    public InputHandler(int playerNumber, InputManager inputManager)
    {
        PlayerNumber = playerNumber;
        this.inputManager = inputManager;
    }

    public bool IsDown(ControllerState.ButtonTypes button) => inputManager.PollForInput(button, PlayerNumber);
    // Down this tick, up the tick before
    public bool JustPressed(ControllerState.ButtonTypes button) => IsDown(button) && !inputManager.PollForInput(button, PlayerNumber, 1);
    // Up this tick, down the tick before
    public bool JustReleased(ControllerState.ButtonTypes button) => !IsDown(button) && inputManager.PollForInput(button, PlayerNumber, 1);

    // Each axis is in whole steps from -StickState.MAX_STICK_AXIS_VALUE to MAX_STICK_AXIS_VALUE
    public DMVector LeftStick() => inputManager.PollForStickState((int)ControllerState.StickTypes.LEFT, PlayerNumber);
    public DMVector RightStick() => inputManager.PollForStickState((int)ControllerState.StickTypes.RIGHT, PlayerNumber);
}

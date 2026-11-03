using Godot;
using System;
using System.Linq;

// Sets up the PlayerManager for a local match. Controllers join by giving any input, into the first free slot.
// A keyboard can't join that way, since it can hold more than one player, so those are added with the mouse from a slot's dropdown
public partial class LocalCharacterSelect : Control
{
    [Export] PackedScene SlotScene;
    [Export] Container Slots;
    [Export] Button StartButton;

    string PlayScenePath = "uid://t16aulelxjm1";

    private PlayerSlot[] playerSlots = new PlayerSlot[PlayerManager.MaxPlayerCount];

    public override void _Ready()
    {
        RemovePlayersWithMissingControllers();

        for (int playerNumber = 0; playerNumber < PlayerManager.MaxPlayerCount; playerNumber++)
        {
            PlayerSlot slot = SlotScene.Instantiate<PlayerSlot>();
            Slots.AddChild(slot);
            slot.Setup(playerNumber);
            // Deferred, so the dropdown that was just picked isn't cleared while it's still handling the pick
            slot.ControllerChanged += () => Callable.From(RefreshSlots).CallDeferred();
            playerSlots[playerNumber] = slot;
        }
        RefreshSlots();

        StartButton.Pressed += () => GetTree().ChangeSceneToFile(PlayScenePath);
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnJoyConnectionChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsControllerInput(@event)) return;
        if (PlayerManager.GlobalInstance.GetPlayerNumberFromInput(@event) != -1) return;

        PlayerManager.GlobalInstance.AttemptAddPlayer(@event);
        RefreshSlots();
    }

    // Button presses, and stick moves past the default controller tag's deadzone, since the player has no tag of their own yet
    private static bool IsControllerInput(InputEvent @event)
    {
        return @event switch
        {
            InputEventJoypadButton button => button.Pressed,
            InputEventJoypadMotion motion => Mathf.Abs(motion.AxisValue) > TagManager.Get(TagManager.DefaultFor(false)).Deadzone,
            _ => false,
        };
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        RemovePlayersWithMissingControllers();
        RefreshSlots();
    }

    private static void RemovePlayersWithMissingControllers()
    {
        var connected = Input.GetConnectedJoypads();
        foreach (var player in PlayerManager.GlobalInstance.AllPlayers)
        {
            if (player == null || player.ControllerType != PlayerProfile.ControllerTypes.CONTROLLER) continue;
            if (!connected.Contains(player.InputDeviceNumber)) PlayerManager.GlobalInstance.RemovePlayer(player.PlayerNumber);
        }
    }

    private void RefreshSlots()
    {
        foreach (var slot in playerSlots)
        {
            slot.Refresh();
        }
        StartButton.Visible = PlayerManager.GlobalInstance.AllPlayers.Any(player => player != null);
    }
}

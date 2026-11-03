using Godot;
using System;

// One player number's box on the local character select. Reads and writes that player in the PlayerManager
public partial class PlayerSlot : PanelContainer
{
    [Export] Label PlayerLabel;
    [Export] OptionButton ControllerDropdown;
    [Export] OptionButton TagDropdown;
    [Export] OptionButton CharacterDropdown;

    // Item ids in the controller dropdown. A controller's id is FirstDeviceItem plus its device number.
    // Not negative, since an OptionButton reads a negative id as asking for one to be assigned
    const int NoControllerItem = 0;
    const int KeyboardItem = 1;
    const int FirstDeviceItem = 2;

    // Raised after the dropdown changes who's in this slot, since other slots list which controllers are free
    public event Action ControllerChanged;

    private int playerNumber;

    public void Setup(int _playerNumber)
    {
        playerNumber = _playerNumber;
        PlayerLabel.Text = $"Player {playerNumber + 1}";

        foreach (var tag in TagManager.ByMostRecent())
        {
            TagDropdown.AddItem(tag.Name, tag.Id);
        }
        foreach (var character in Enum.GetValues<CharacterId>())
        {
            CharacterDropdown.AddItem(character.ToString(), (int)character);
        }

        ControllerDropdown.ItemSelected += index => OnControllerSelected(ControllerDropdown.GetItemId((int)index));
        TagDropdown.ItemSelected += index => Player().SetTag(TagDropdown.GetItemId((int)index));
        CharacterDropdown.ItemSelected += index => Player().Character = (CharacterId)CharacterDropdown.GetItemId((int)index);
    }

    public void Refresh()
    {
        PlayerProfile player = Player();

        ControllerDropdown.Clear();
        ControllerDropdown.AddItem("None", NoControllerItem);
        ControllerDropdown.AddItem("Keyboard", KeyboardItem);
        foreach (int device in Input.GetConnectedJoypads())
        {
            ControllerDropdown.AddItem($"{device}: {Input.GetJoyName(device)}", FirstDeviceItem + device);
            // A controller only drives one player. Keyboards can be shared, since tags can bind different keys
            ControllerDropdown.SetItemDisabled(ControllerDropdown.ItemCount - 1, IsUsedByOtherPlayer(device));
        }
        ControllerDropdown.Select(ControllerDropdown.GetItemIndex(ItemFor(player)));

        TagDropdown.Visible = player != null;
        CharacterDropdown.Visible = player != null;
        if (player == null) return;

        TagDropdown.Select(TagDropdown.GetItemIndex(player.TagId));
        CharacterDropdown.Select(CharacterDropdown.GetItemIndex((int)player.Character));
    }

    private PlayerProfile Player() => PlayerManager.GlobalInstance.AllPlayers[playerNumber];

    private void OnControllerSelected(int item)
    {
        // Picking the current item again shouldn't reset the player's tag
        if (item == ItemFor(Player())) return;

        if (item == NoControllerItem)
        {
            PlayerManager.GlobalInstance.RemovePlayer(playerNumber);
        }
        else if (item == KeyboardItem)
        {
            // Any device number but -1 reads as local. Keyboards ignore it, since every keyboard is one device
            PlayerManager.GlobalInstance.SetLocalPlayer(playerNumber, 0, PlayerProfile.ControllerTypes.KEYBOARD);
        }
        else
        {
            PlayerManager.GlobalInstance.SetLocalPlayer(playerNumber, item - FirstDeviceItem, PlayerProfile.ControllerTypes.CONTROLLER);
        }
        ControllerChanged?.Invoke();
    }

    private static int ItemFor(PlayerProfile player)
    {
        if (player == null) return NoControllerItem;
        if (player.ControllerType == PlayerProfile.ControllerTypes.KEYBOARD) return KeyboardItem;
        return FirstDeviceItem + player.InputDeviceNumber;
    }

    private bool IsUsedByOtherPlayer(int device)
    {
        foreach (var other in PlayerManager.GlobalInstance.AllPlayers)
        {
            if (other == null || other.PlayerNumber == playerNumber) continue;
            if (other.ControllerType == PlayerProfile.ControllerTypes.CONTROLLER && other.InputDeviceNumber == device) return true;
        }
        return false;
    }
}

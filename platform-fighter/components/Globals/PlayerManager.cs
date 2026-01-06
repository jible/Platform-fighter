using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node
{
    public List<PlayerProfile> AllPlayers = [];
    public int MaxPlayerCount = 4;
    public static  PlayerManager GlobalInstance;
    [Signal] public delegate void PlayerAddedEventHandler(int PlayerNumber);
    [Signal] public delegate void PlayerRemovedEventHandler(int PlayerNumber);

    public override void _Ready()
    {
        GlobalInstance = this;
        for (int i = 0; i < MaxPlayerCount; i++)
        {
            AllPlayers.Add(null);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("debug_slow_game"))
        {
            foreach (var item in AllPlayers)
            {
                GD.Print(item);

            }

        }
    }


    public void AttemptAddPlayer(InputEvent input)
    {
        if (GetPlayerNumberFromInput(input ) != -1) return;
        int PlayerNumber = -1;
        for (int i = 0 ; i < MaxPlayerCount; i++)
        {
            if ( AllPlayers[i] == null)
            {
                PlayerNumber = i;
                break;
            }
        }
        // No valid player slots
        if (PlayerNumber == -1) return;

        PlayerProfile playerProfile = new();
        playerProfile.Configure(PlayerNumber, input.Device, GetControllerType(input));

        AllPlayers[PlayerNumber] = playerProfile;
        EmitSignal("PlayerAdded", PlayerNumber);
    }

     public void AttemptRemovePlayer(InputEvent input)
    {
        int PlayerNumber = GetPlayerNumberFromInput(input);
        GD.Print(PlayerNumber);
        if (PlayerNumber == -1) return;
        AllPlayers[PlayerNumber] = null;
        EmitSignal("PlayerRemoved", PlayerNumber);
    }


    // Outputs -1 if input not bound to player number;
    public int GetPlayerNumberFromInput(InputEvent input)
    { 
        for (int i = 0; i < MaxPlayerCount; i++)
        {
            PlayerProfile player = AllPlayers[i];
            if (
                player != null && 
                input.Device == player.InputDeviceNumber && 
                GetControllerType(input) == player.ControllerType
            )return i;
        }
        
        return -1;
    }

    public PlayerProfile.ControllerTypes GetControllerType(InputEvent input)
    {
        if (input is InputEventJoypadButton || input is InputEventJoypadMotion)
        {
            return PlayerProfile.ControllerTypes.CONTROLLER;
        } else if (input is InputEventKey)
        {
            return PlayerProfile.ControllerTypes.KEYBOARD;
        } else
        {
            throw new ArgumentException("Unexpected Input Type Received");
        }
    }
}


/*

func attempt_add_player(new_player_device: int, controller_type: PlayerProfile.ControllerType, new_player_peer_id: int):
	if get_player_num_from_input(new_player_device, controller_type) != -1: return
	
	var new_player_profile = PlayerProfile.new()
	var new_player_num
	for i in range(max_player_count):
		if all_players[i] == null:
			new_player_num = i
			break
	new_player_profile.configure(new_player_num, new_player_peer_id, new_player_device, controller_type)
	all_players[new_player_num] = new_player_profile
	print("adding player")
	
	added_player.emit(new_player_num)

func attempt_remove_player(target_device_num, controller_type: PlayerProfile.ControllerType , _peer_id):
	var player_num = get_player_num_from_input(target_device_num, controller_type)
	if player_num == -1: return
	all_players[player_num] = null
	removed_player.emit(player_num)

# Outputs -1 if no such player exists
func get_player_num_from_input(target_device_num: int, controller_type : PlayerProfile.ControllerType) -> int:
	for i in range(all_players.size()):
		var player = all_players[i]
		if player and target_device_num == player.input_device_number and player.controller_type == controller_type:
			return i
	return -1
    
    */
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public partial class GlobalResources : Node
{
    
    // func get_current_match_frame():
	// return Engine.get_physics_frames()

    enum ConnectionMode {
        LOCAL,
        ONLINE
    }

    

    enum PhysicsLayers {
        ENVIRONMENT,
        PLATFORM,
        PLAYER_1_COLLISION,
        PLAYER_2_COLLISION,
        PLAYER_3_COLLISION,
        PLAYER_4_COLLISION,
        PLAYER_1_HITBOX,
        PLAYER_2_HITBOX,
        PLAYER_3_HITBOX,
        PLAYER_4_HITBOX,
        PLAYER_1_HURTBOX,
        PLAYER_2_HURTBOX,
        PLAYER_3_HURTBOX,
        PLAYER_4_HURTBOX,
    }

    

}
// Bits for PhysicsObject.Layer and Mask
public static class CollisionLayers
{
    public const int Stage = 1 << 0;
    public const int Characters = 1 << 1;
    // One way, see PhysicsObject.oneWay
    public const int Platforms = 1 << 2;
}

public static class DebugPrintColors
{
    public static Color ErrorColor = new Color("#f54266");
    public static Color SuccessColor = new Color("#42f56f");
    public static Color ExtraColor = new Color("#6c2cd3");


}
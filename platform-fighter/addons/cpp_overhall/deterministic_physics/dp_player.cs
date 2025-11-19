using Godot;
using System;
using System.ComponentModel;

public partial class dp_player : dp_object
{
    DM_Vector2 Velocity = new();
    // Shadow these properties so they aren't exported to the editor
    public new bool is_static;
    public new bool is_trigger;
    public override void _Ready()
    {
        base.is_static = false;
        base.is_trigger = false;
        base._Ready();
    }
    public override void PhysicsStep()
    {
        Shape.Position += Velocity;
        base.PhysicsStep();
    }
}

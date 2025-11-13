using Godot;
using System;

[Tool]
[GlobalClass]
public partial class dp_shape_renderer : Node2D
{
    [Export] public dp_physics_server PhysicsServer;
    [Export] public bool RenderShapesInEditor = true;
    [Export] public bool RenderShapesInPlay = true;

    public override void _PhysicsProcess(double delta)
    {
        if ((Engine.IsEditorHint() && RenderShapesInEditor) || (!Engine.IsEditorHint() && RenderShapesInPlay))
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        // base._Draw();
        if (PhysicsServer == null) { return; }
        foreach (dp_object PhysicsObject in PhysicsServer.AllShapes)
        {
            dp_shape Shape = PhysicsObject.Shape;
            if (PhysicsObject == null) { continue; }
            if (Shape is dp_circle) { draw_collision_circle((dp_circle)Shape); }
            if (Shape is dp_rectangle) { draw_collision_rectangle((dp_rectangle)Shape); }

        }
    }

    public void draw_collision_circle(dp_circle circle)
    {
        DrawCircle(circle.Position.ToStandardVector(), circle.radius.ToFloat(), circle.PhysicsObject.color);
    }
    public void draw_collision_rectangle(dp_rectangle rectangle)
    {
        return;
    }

}


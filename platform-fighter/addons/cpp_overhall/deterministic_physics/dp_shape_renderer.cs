using Godot;
using System;

[Tool]
[GlobalClass]
public partial class dp_shape_renderer : Node2D
{
    [Export] public dp_physics_server PhysicsServer;
    [Export] public bool RenderShapesInEditor;
    [Export] public bool RenderShapesInPlay;

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
        foreach (dp_object PhysicsObject in PhysicsServer.AllEntities)
        {
            dp_shape Shape = PhysicsObject.Shape;
            if (PhysicsObject == null || Shape == null) { continue; }
            if (Shape is dp_circle circle) { draw_collision_circle(PhysicsObject, circle); }
            if (Shape is dp_rectangle rectangle) { draw_collision_rectangle(PhysicsObject, rectangle); }
        }
    }

    public void draw_collision_circle(dp_object PhysicsObject, dp_circle circle)
    {
        if (Engine.IsEditorHint())
        {
            DrawCircle(PhysicsObject.EditorGlobalPosition, circle.EditorRadius, circle.PhysicsObject.color);
            return;
        }
        DrawCircle(PhysicsObject.Position.ToStandardVector(), circle.Radius.ToFloat(), circle.PhysicsObject.color);
    }
    public void draw_collision_rectangle(dp_object PhysicsObject, dp_rectangle rectangle)
    {
        Rect2 DrawShape;
        if (Engine.IsEditorHint())
        {
            DrawShape = new Rect2(PhysicsObject.EditorGlobalPosition- (rectangle.EditorSize/2), rectangle.EditorSize);
        } else
        {
            DrawShape = new Rect2((PhysicsObject.GlobalPosition - (rectangle.Size/2)).ToStandardVector(), rectangle.Size.ToStandardVector());
        }
        DrawRect(DrawShape, rectangle.PhysicsObject.color);
    }

}


    // public void draw_collision_rectangle(dp_rectangle rectangle)
    // {
    //     GD.Print(rectangle.Position.ToStandardVector(), rectangle.size.ToStandardVector());
    //     Rect2 DrawShape = new Rect2((rectangle.Position- rectangle.size).ToStandardVector(), ( rectangle.size* 2).ToStandardVector());
    //     DrawRect(DrawShape, rectangle.PhysicsObject.color);
    // }